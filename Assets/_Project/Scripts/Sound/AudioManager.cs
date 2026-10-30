using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public enum SoundId
{
	WorldAlert,
	UiStartGame,
	UiHeartbeat,
	UiButtonSelect,
	UiButtonConfirm,
	UiButtonBack,
	PlayerTalk,
	PlayerStress,
	PlayerShootTaser,
	PlayerShootGun,
	PlayerLockerOpen,
	PlayerLockerClose,
	PlayerItemGet,
	PlayerInteract,
	PlayerHurt2,
	PlayerHurt1,
	PlayerFootstep4,
	PlayerFootstep3,
	PlayerFootstep2,
	PlayerFootstep1,
	PlayerDeath,
	PlayerComputerStartLoopEnd,
	PlayerComputerStart,
	PlayerComputerKeyboard,
	MonsterHurt4,
	MonsterHurt3,
	MonsterHurt2,
	MonsterHurt1,
	MonsterAttack4,
	MonsterAttack3,
	MonsterAttack2,
	MonsterAttack1,
	MonsterAlert3,
	MonsterAlert2,
	MonsterAlert1,
	MonsterStaggered,
	PlayerDoorFail,
	PlayerDoorSuccess,
	PlayerDropItem,
	PlayerPipeAttack
}

public enum MusicId
{
	MainMenuAmbient,
	TapeLayerStart,
	TapeLayerLoopEnd
}

/// <summary>Central playback point for all authored game audio.</summary>
public sealed class AudioManager : MonoBehaviour
{
	private const int DefaultSfxVoiceCount = 16;
	private const float BgmCrossfadeDuration = 1.5f;
	private const float ComputerCrossfadeDuration = 0.15f;
	private const string SfxGroupName = "SFX";
	private const string BgmGroupName = "BGM";

	public static AudioManager Instance { get; private set; }

	[SerializeField] private AudioMixer mixer;
	[SerializeField] private AudioClip[] sfxClips = new AudioClip[Enum.GetValues(typeof(SoundId)).Length];
	[SerializeField] private AudioClip[] musicClips = new AudioClip[Enum.GetValues(typeof(MusicId)).Length];
	[SerializeField, Min(1)] private int sfxVoiceCount = DefaultSfxVoiceCount;
	[SerializeField, Range(0f, 1f)] private float spatialSfxVolume = 1f;

	private AudioSource[] sfxSources;
	private AudioSource[] musicSources;
	private AudioSource[] computerSequenceSources;
	private int nextSfxSource;
	private int activeMusicSourceIndex = -1;
	private int activeComputerSourceIndex = -1;
	private int currentMusicIndex = -1;
	private float baseSfxVolumeDb;
	private float sfxVolumeOffsetDb;
	private Coroutine musicRoutine;
	private Coroutine computerSequenceRoutine;
	private bool stageMusicSequenceActive;
	private bool computerSequenceActive;
	private bool computerEndInProgress;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
		CreateSources();
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDestroy()
	{
		SceneManager.sceneLoaded -= HandleSceneLoaded;
		if (Instance == this)
			Instance = null;
	}

	public void PlaySfx2D(SoundId sound, float volume = 1f)
	{
		PlaySfx(sound, transform.position, false, volume);
	}

	public void PlaySfxAt(SoundId sound, Vector3 position, float volume = 1f)
	{
		PlaySfx(sound, position, false, volume);
	}

	public void PlayRandomSfx2D(params SoundId[] sounds)
	{
		if (sounds == null || sounds.Length == 0)
			return;

		PlaySfx2D(sounds[UnityEngine.Random.Range(0, sounds.Length)]);
	}

	public void PlayRandomSfxAt(Vector3 position, params SoundId[] sounds)
	{
		if (sounds == null || sounds.Length == 0)
			return;

		PlaySfxAt(sounds[UnityEngine.Random.Range(0, sounds.Length)], position);
	}

	public void PlayMusic(MusicId music, bool loop = true)
	{
		int index = (int)music;
		if (musicSources == null || !HasClip(musicClips, index, "music"))
			return;

		if (currentMusicIndex == index && activeMusicSourceIndex >= 0 &&
			musicSources[activeMusicSourceIndex].isPlaying &&
			musicSources[activeMusicSourceIndex].loop == loop)
			return;

		stageMusicSequenceActive = false;
		StopMusicRoutine();
		musicRoutine = StartCoroutine(CrossfadeToMusic(musicClips[index], loop, BgmCrossfadeDuration, index));
	}

	public void StopMusic()
	{
		stageMusicSequenceActive = false;
		StopMusicRoutine();
		StopAllMusicSources();
		activeMusicSourceIndex = -1;
		currentMusicIndex = -1;
	}

	public void PlayEndingComputerEnd()
	{
		if (!computerSequenceActive || !HasClip(sfxClips, (int)SoundId.PlayerComputerStartLoopEnd, "SFX"))
			return;

		if (computerSequenceRoutine != null)
			StopCoroutine(computerSequenceRoutine);

		int fromIndex = GetLoudestComputerSourceIndex();
		int toIndex = fromIndex < 0 ? 0 : 1 - fromIndex;
		AudioSource target = computerSequenceSources[toIndex];
		target.Stop();
		target.clip = sfxClips[(int)SoundId.PlayerComputerStartLoopEnd];
		target.loop = false;
		target.volume = 0f;
		target.Play();
		computerEndInProgress = true;
		computerSequenceRoutine = StartCoroutine(CrossfadeComputerToEnd(fromIndex, toIndex));
	}

	private void StartStageMusicSequence()
	{
		if (stageMusicSequenceActive)
			return;

		if (!HasClip(musicClips, (int)MusicId.TapeLayerStart, "music") ||
			!HasClip(musicClips, (int)MusicId.TapeLayerLoopEnd, "music"))
			return;

		stageMusicSequenceActive = true;
		StopMusicRoutine();
		musicRoutine = StartCoroutine(PlayStageMusicSequence());
	}

	private IEnumerator PlayStageMusicSequence()
	{
		AudioClip intro = musicClips[(int)MusicId.TapeLayerStart];
		AudioClip loop = musicClips[(int)MusicId.TapeLayerLoopEnd];

		yield return CrossfadeToMusic(intro, false, BgmCrossfadeDuration, (int)MusicId.TapeLayerStart);
		yield return new WaitForSecondsRealtime(Mathf.Max(0f, intro.length - (2f * BgmCrossfadeDuration)));
		if (!stageMusicSequenceActive)
			yield break;

		yield return CrossfadeToMusic(loop, false, BgmCrossfadeDuration, (int)MusicId.TapeLayerLoopEnd);
		while (stageMusicSequenceActive)
		{
			yield return new WaitForSecondsRealtime(Mathf.Max(0f, loop.length - (2f * BgmCrossfadeDuration)));
			if (!stageMusicSequenceActive)
				yield break;

			yield return CrossfadeToMusic(loop, false, BgmCrossfadeDuration, (int)MusicId.TapeLayerLoopEnd);
		}
	}

	private void StartEndingComputerSequence()
	{
		if (computerSequenceActive && !computerEndInProgress)
			return;

		StopComputerSequence();
		if (!HasClip(sfxClips, (int)SoundId.PlayerComputerStart, "SFX") ||
			!HasClip(sfxClips, (int)SoundId.PlayerComputerKeyboard, "SFX") ||
			!HasClip(sfxClips, (int)SoundId.PlayerComputerStartLoopEnd, "SFX"))
			return;

		computerSequenceActive = true;
		computerSequenceRoutine = StartCoroutine(PlayEndingComputerSequence());
	}

	private IEnumerator PlayEndingComputerSequence()
	{
		AudioClip startClip = sfxClips[(int)SoundId.PlayerComputerStart];
		AudioClip keyboardClip = sfxClips[(int)SoundId.PlayerComputerKeyboard];
		AudioSource introSource = computerSequenceSources[0];
		introSource.Stop();
		introSource.clip = startClip;
		introSource.loop = false;
		introSource.volume = 1f;
		introSource.Play();
		activeComputerSourceIndex = 0;

		yield return new WaitForSecondsRealtime(Mathf.Max(0f, startClip.length - ComputerCrossfadeDuration));
		if (!computerSequenceActive || computerEndInProgress)
			yield break;

		int nextIndex = 1 - activeComputerSourceIndex;
		PlayComputerClip(nextIndex, keyboardClip, false, 0f);
		yield return CrossfadeComputerSources(activeComputerSourceIndex, nextIndex, ComputerCrossfadeDuration);
		activeComputerSourceIndex = nextIndex;

		while (computerSequenceActive && !computerEndInProgress)
		{
			yield return new WaitForSecondsRealtime(Mathf.Max(0f, keyboardClip.length - (2f * ComputerCrossfadeDuration)));
			if (!computerSequenceActive || computerEndInProgress)
				yield break;

			nextIndex = 1 - activeComputerSourceIndex;
			PlayComputerClip(nextIndex, keyboardClip, false, 0f);
			yield return CrossfadeComputerSources(activeComputerSourceIndex, nextIndex, ComputerCrossfadeDuration);
			activeComputerSourceIndex = nextIndex;
		}
	}

	private IEnumerator CrossfadeComputerToEnd(int fromIndex, int toIndex)
	{
		if (fromIndex >= 0)
			yield return CrossfadeComputerSources(fromIndex, toIndex, ComputerCrossfadeDuration);
		else
			computerSequenceSources[toIndex].volume = 1f;

		activeComputerSourceIndex = toIndex;
		AudioClip endClip = computerSequenceSources[toIndex].clip;
		yield return new WaitForSecondsRealtime(endClip.length);
		computerSequenceSources[toIndex].Stop();
		computerSequenceSources[toIndex].volume = 1f;
		activeComputerSourceIndex = -1;
		computerSequenceActive = false;
		computerEndInProgress = false;
		computerSequenceRoutine = null;
	}

	private IEnumerator CrossfadeComputerSources(int fromIndex, int toIndex, float duration)
	{
		AudioSource from = computerSequenceSources[fromIndex];
		AudioSource to = computerSequenceSources[toIndex];
		float fromVolume = from.volume;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			from.volume = Mathf.Lerp(fromVolume, 0f, progress);
			to.volume = progress;
			yield return null;
		}

		from.volume = 0f;
		from.Stop();
		to.volume = 1f;
	}

	private void PlayComputerClip(int sourceIndex, AudioClip clip, bool loop, float volume)
	{
		AudioSource source = computerSequenceSources[sourceIndex];
		source.Stop();
		source.clip = clip;
		source.loop = loop;
		source.volume = volume;
		source.Play();
	}

	private int GetLoudestComputerSourceIndex()
	{
		int loudestIndex = -1;
		float loudestVolume = 0f;
		for (int i = 0; i < computerSequenceSources.Length; i++)
		{
			AudioSource source = computerSequenceSources[i];
			if (source.isPlaying && source.volume > loudestVolume)
			{
				loudestIndex = i;
				loudestVolume = source.volume;
			}
		}
		return loudestIndex;
	}

	private void StopComputerSequence()
	{
		if (computerSequenceRoutine != null)
		{
			StopCoroutine(computerSequenceRoutine);
			computerSequenceRoutine = null;
		}

		if (computerSequenceSources != null)
		{
			foreach (AudioSource source in computerSequenceSources)
			{
				source.Stop();
				source.volume = 1f;
			}
		}

		activeComputerSourceIndex = -1;
		computerSequenceActive = false;
		computerEndInProgress = false;
	}

	private IEnumerator CrossfadeToMusic(AudioClip clip, bool loop, float duration, int musicIndex)
	{
		int fromIndex = activeMusicSourceIndex;
		int toIndex = fromIndex < 0 ? 0 : 1 - fromIndex;
		AudioSource target = musicSources[toIndex];
		target.Stop();
		target.clip = clip;
		target.loop = loop;
		target.volume = 0f;
		target.Play();
		activeMusicSourceIndex = toIndex;
		currentMusicIndex = musicIndex;

		if (fromIndex >= 0)
			yield return CrossfadeMusicSources(fromIndex, toIndex, duration);
		else
			yield return FadeMusicSourceIn(target, duration);
	}

	private IEnumerator FadeMusicToSilence(float duration)
	{
		float[] startVolumes = new float[musicSources.Length];
		for (int i = 0; i < musicSources.Length; i++)
			startVolumes[i] = musicSources[i].volume;

		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			for (int i = 0; i < musicSources.Length; i++)
				musicSources[i].volume = Mathf.Lerp(startVolumes[i], 0f, progress);
			yield return null;
		}

		StopAllMusicSources();
		activeMusicSourceIndex = -1;
		currentMusicIndex = -1;
		musicRoutine = null;
	}

	private IEnumerator CrossfadeMusicSources(int fromIndex, int toIndex, float duration)
	{
		AudioSource from = musicSources[fromIndex];
		AudioSource to = musicSources[toIndex];
		float fromVolume = from.volume;
		float elapsed = 0f;

		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			float progress = Mathf.Clamp01(elapsed / duration);
			from.volume = Mathf.Lerp(fromVolume, 0f, progress);
			to.volume = progress;
			yield return null;
		}

		from.volume = 0f;
		from.Stop();
		to.volume = 1f;
	}

	private IEnumerator FadeMusicSourceIn(AudioSource source, float duration)
	{
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.unscaledDeltaTime;
			source.volume = Mathf.Clamp01(elapsed / duration);
			yield return null;
		}
		source.volume = 1f;
	}

	private void StopMusicRoutine()
	{
		if (musicRoutine != null)
		{
			StopCoroutine(musicRoutine);
			musicRoutine = null;
		}
	}

	private void StopAllMusicSources()
	{
		if (musicSources == null)
			return;
		foreach (AudioSource source in musicSources)
		{
			source.Stop();
			source.volume = 1f;
		}
	}

	public void SetBaseSfxVolumeDb(float decibels)
	{
		baseSfxVolumeDb = decibels;
		ApplySfxVolume();
	}

	public void SetSfxVolumeOffsetDb(float decibels)
	{
		sfxVolumeOffsetDb = decibels;
		ApplySfxVolume();
	}

	private void ApplySfxVolume()
	{
		if (mixer != null && !mixer.SetFloat("SFXVolume", baseSfxVolumeDb + sfxVolumeOffsetDb))
			Debug.LogError("[AudioManager] Exposed AudioMixer parameter 'SFXVolume' was not found.", this);
	}

	private void PlaySfx(SoundId sound, Vector3 position, bool spatial, float volume)
	{
		int index = (int)sound;
		if (!HasClip(sfxClips, index, "SFX") || sfxSources == null || sfxSources.Length == 0)
			return;

		AudioSource source = GetNextSfxSource();
		source.transform.position = position;
		source.spatialBlend = spatial ? 1f : 0f;
		source.volume = Mathf.Clamp01(volume) * (spatial ? spatialSfxVolume : 1f);
		source.clip = sfxClips[index];
		source.Play();
	}

	private AudioSource GetNextSfxSource()
	{
		AudioSource source = sfxSources[nextSfxSource];
		nextSfxSource = (nextSfxSource + 1) % sfxSources.Length;
		return source;
	}

	private void CreateSources()
	{
		AudioMixerGroup sfxGroup = FindMixerGroup(SfxGroupName);
		AudioMixerGroup bgmGroup = FindMixerGroup(BgmGroupName);
		sfxSources = new AudioSource[Mathf.Max(1, sfxVoiceCount)];

		for (int i = 0; i < sfxSources.Length; i++)
		{
			sfxSources[i] = CreateSource($"SFX Voice {i + 1}", sfxGroup);
			sfxSources[i].spatialBlend = 0f;
			// Each one-shot uses a free voice; this assignment is reset before playback.
			sfxSources[i].playOnAwake = false;
		}

		musicSources = new AudioSource[2];
		computerSequenceSources = new AudioSource[2];
		for (int i = 0; i < 2; i++)
		{
			musicSources[i] = CreateSource($"Music Voice {i + 1}", bgmGroup);
			computerSequenceSources[i] = CreateSource($"Computer Sequence Voice {i + 1}", sfxGroup);
		}
	}

	private AudioSource CreateSource(string sourceName, AudioMixerGroup outputGroup)
	{
		GameObject sourceObject = new GameObject(sourceName);
		sourceObject.transform.SetParent(transform, false);
		AudioSource source = sourceObject.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.outputAudioMixerGroup = outputGroup;
		return source;
	}

	private AudioMixerGroup FindMixerGroup(string groupName)
	{
		if (mixer == null)
		{
			Debug.LogError("[AudioManager] Main AudioMixer is not assigned.", this);
			return null;
		}

		AudioMixerGroup[] groups = mixer.FindMatchingGroups(groupName);
		if (groups.Length == 0)
		{
			Debug.LogError($"[AudioManager] Mixer group '{groupName}' was not found.", this);
			return null;
		}

		return groups[0];
	}

	private bool HasClip(AudioClip[] clips, int index, string category)
	{
		if (clips != null && index >= 0 && index < clips.Length && clips[index] != null)
			return true;

		Debug.LogWarning($"[AudioManager] Missing {category} clip at catalog index {index}.", this);
		return false;
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.name == "MainMenu")
		{
			stageMusicSequenceActive = false;
			StopMusicRoutine();
			PlayMusic(MusicId.MainMenuAmbient);
			if (!computerEndInProgress)
				StopComputerSequence();
		}
		else if (scene.name == "Stage1" || scene.name == "Stage2" || scene.name == "Stage3")
		{
			StopComputerSequence();
			StartStageMusicSequence();
		}
		else if (scene.name == "GoodEnding" || scene.name == "BadEnding")
		{
			stageMusicSequenceActive = false;
			StopMusicRoutine();
			musicRoutine = StartCoroutine(FadeMusicToSilence(BgmCrossfadeDuration));
			StartEndingComputerSequence();
		}
	}
}
