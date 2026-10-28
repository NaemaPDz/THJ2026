using UnityEngine;

/// <summary>
/// Central place for playing authored audio clips. Add this component to one
/// object in the first scene (for example, the Main Menu); it persists across scenes.
/// </summary>
public sealed class AudioManager : MonoBehaviour
{
	public static AudioManager Instance { get; private set; }

	[SerializeField] private AudioSource musicSource;
	[SerializeField] private AudioSource sfxSource;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
		musicSource = GetOrCreateSource(musicSource, "Music", true);
		sfxSource = GetOrCreateSource(sfxSource, "SFX", false);
	}

	private void OnDestroy()
	{
		if (Instance == this)
			Instance = null;
	}

	/// <summary>Plays a 2D one-shot sound. Repeated calls can overlap.</summary>
	public void PlaySfx(AudioClip clip, float volume = 1f)
	{
		if (clip == null || sfxSource == null)
			return;

		sfxSource.PlayOneShot(clip, Mathf.Clamp01(volume));
	}

	/// <summary>Plays a one-shot sound from a position in the world.</summary>
	public void PlaySfxAt(AudioClip clip, Vector3 position, float volume = 1f)
	{
		if (clip == null)
			return;

		AudioSource.PlayClipAtPoint(clip, position, Mathf.Clamp01(volume));
	}

	/// <summary>Starts looping background music, or changes the current track.</summary>
	public void PlayMusic(AudioClip clip, bool loop = true)
	{
		if (clip == null || musicSource == null)
			return;

		if (musicSource.clip == clip && musicSource.isPlaying)
			return;

		musicSource.clip = clip;
		musicSource.loop = loop;
		musicSource.Play();
	}

	public void StopMusic()
	{
		if (musicSource != null)
			musicSource.Stop();
	}

	private AudioSource GetOrCreateSource(AudioSource source, string sourceName, bool isMusic)
	{
		if (source == null)
		{
			GameObject sourceObject = new GameObject(sourceName);
			sourceObject.transform.SetParent(transform, false);
			source = sourceObject.AddComponent<AudioSource>();
		}

		source.playOnAwake = false;
		source.spatialBlend = 0f;
		source.loop = isMusic;
		return source;
	}
}
