using System;
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
	MonsterAlert1
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
	private const string SfxGroupName = "SFX";
	private const string BgmGroupName = "BGM";

	public static AudioManager Instance { get; private set; }

	[SerializeField] private AudioMixer mixer;
	[SerializeField] private AudioClip[] sfxClips = new AudioClip[Enum.GetValues(typeof(SoundId)).Length];
	[SerializeField] private AudioClip[] musicClips = new AudioClip[Enum.GetValues(typeof(MusicId)).Length];
	[SerializeField, Min(1)] private int sfxVoiceCount = DefaultSfxVoiceCount;
	[SerializeField, Range(0f, 1f)] private float spatialSfxVolume = 1f;

	private AudioSource[] sfxSources;
	private AudioSource musicSource;
	private int nextSfxSource;
	private int currentMusicIndex = -1;
	private float baseSfxVolumeDb;
	private float sfxVolumeOffsetDb;

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

	private void Start()
	{
		PlayMusic(MusicId.MainMenuAmbient);
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
		PlaySfx(sound, position, true, volume);
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
		if (musicSource == null || !HasClip(musicClips, index, "music"))
			return;

		if (currentMusicIndex == index && musicSource.isPlaying)
			return;

		currentMusicIndex = index;
		musicSource.clip = musicClips[index];
		musicSource.loop = loop;
		musicSource.Play();
	}

	public void StopMusic()
	{
		if (musicSource != null)
			musicSource.Stop();
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

		musicSource = CreateSource("Music", bgmGroup);
		musicSource.spatialBlend = 0f;
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
			PlayMusic(MusicId.MainMenuAmbient);
		else if (scene.name == "Stage1")
			PlayMusic(MusicId.TapeLayerStart);
		else if (scene.name == "Stage2" || scene.name == "Stage3")
			PlayMusic(MusicId.TapeLayerLoopEnd);
	}
}
