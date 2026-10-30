using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class MainMenuController : MonoBehaviour
{
	private const string Stage1SceneName = "Stage1";
	private const string MasterPreferenceKey = "MainMenu.MasterVolume";
	private const string SfxPreferenceKey = "MainMenu.SfxVolume";
	private const string BgmPreferenceKey = "MainMenu.BgmVolume";
	private const string MasterMixerParameter = "MasterVolume";
	private const string SfxMixerParameter = "SFXVolume";
	private const string BgmMixerParameter = "BGMVolume";
	private const int MinimumVolume = 1;
	private const int MaximumVolume = 10;

	[SerializeField] private GameObject mainPanel;
	[SerializeField] private GameObject optionPanel;
	[SerializeField] private GameObject loadingPanel;
	[SerializeField] private Button startButton;
	[SerializeField] private Button optionsButton;
	[SerializeField] private Button quitButton;
	[SerializeField] private Button optionsBackButton;
	[SerializeField] private Button masterMinusButton;
	[SerializeField] private Button masterPlusButton;
	[SerializeField] private TMP_Text masterValueText;
	[SerializeField] private Button sfxMinusButton;
	[SerializeField] private Button sfxPlusButton;
	[SerializeField] private TMP_Text sfxValueText;
	[SerializeField] private Button bgmMinusButton;
	[SerializeField] private Button bgmPlusButton;
	[SerializeField] private TMP_Text bgmValueText;
	[SerializeField] private AudioMixer mainMixer;

	private int masterVolume = MaximumVolume;
	private int sfxVolume = MaximumVolume;
	private int bgmVolume = MaximumVolume;
	private GameObject lastSelectedObject;

	private void Awake()
	{
		masterVolume = PlayerPrefs.GetInt(MasterPreferenceKey, MaximumVolume);
		sfxVolume = PlayerPrefs.GetInt(SfxPreferenceKey, MaximumVolume);
		bgmVolume = PlayerPrefs.GetInt(BgmPreferenceKey, MaximumVolume);

		masterVolume = Mathf.Clamp(masterVolume, MinimumVolume, MaximumVolume);
		sfxVolume = Mathf.Clamp(sfxVolume, MinimumVolume, MaximumVolume);
		bgmVolume = Mathf.Clamp(bgmVolume, MinimumVolume, MaximumVolume);

		RefreshVolumeLabels();
		ShowMainMenu();
	}

	private void OnEnable()
	{
		AddListener(startButton, StartGame);
		AddListener(optionsButton, ShowOptions);
		AddListener(quitButton, QuitGame);
		AddListener(optionsBackButton, ReturnToMainMenu);
		AddListener(masterMinusButton, DecreaseMasterVolume);
		AddListener(masterPlusButton, IncreaseMasterVolume);
		AddListener(sfxMinusButton, DecreaseSfxVolume);
		AddListener(sfxPlusButton, IncreaseSfxVolume);
		AddListener(bgmMinusButton, DecreaseBgmVolume);
		AddListener(bgmPlusButton, IncreaseBgmVolume);
	}

	private void Start()
	{
		PlayUiSelectForCurrentSelection();
		ApplyMixerVolume(MasterMixerParameter, masterVolume);
		ApplyMixerVolume(SfxMixerParameter, sfxVolume);
		ApplyMixerVolume(BgmMixerParameter, bgmVolume);
	}

	private void Update()
	{
		GameObject selectedObject = EventSystem.current != null
			? EventSystem.current.currentSelectedGameObject
			: null;

		if (selectedObject == lastSelectedObject)
			return;

		lastSelectedObject = selectedObject;
		PlayUiSelectForCurrentSelection();
	}

	private void OnDisable()
	{
		RemoveListener(startButton, StartGame);
		RemoveListener(optionsButton, ShowOptions);
		RemoveListener(quitButton, QuitGame);
		RemoveListener(optionsBackButton, ReturnToMainMenu);
		RemoveListener(masterMinusButton, DecreaseMasterVolume);
		RemoveListener(masterPlusButton, IncreaseMasterVolume);
		RemoveListener(sfxMinusButton, DecreaseSfxVolume);
		RemoveListener(sfxPlusButton, IncreaseSfxVolume);
		RemoveListener(bgmMinusButton, DecreaseBgmVolume);
		RemoveListener(bgmPlusButton, IncreaseBgmVolume);
	}

	private void ShowMainMenu()
	{
		SetPanelActive(mainPanel, true);
		SetPanelActive(optionPanel, false);
		SetPanelActive(loadingPanel, false);
	}

	private void ShowOptions()
	{
		AudioManager.Instance?.PlaySfx2D(SoundId.UiButtonConfirm);
		SetPanelActive(mainPanel, false);
		SetPanelActive(optionPanel, true);
		SetPanelActive(loadingPanel, false);
	}

	private void ReturnToMainMenu()
	{
		AudioManager.Instance?.PlaySfx2D(SoundId.UiButtonBack);
		ShowMainMenu();
	}

	private void StartGame()
	{
		if (!Application.CanStreamedLevelBeLoaded(Stage1SceneName))
		{
			Debug.LogError($"[MainMenu] ไม่พบ Scene '{Stage1SceneName}' ใน Build Settings", this);
			return;
		}

		StartCoroutine(StartGameCoroutine());
	}

	private IEnumerator StartGameCoroutine()
	{
		AudioManager.Instance?.PlaySfx2D(SoundId.UiStartGame);
		loadingPanel.SetActive(true);
		mainPanel.SetActive(false);
		optionPanel.SetActive(false);

		yield return new WaitForSeconds(2f);

		FaustRunState.BeginNewRun();
		SceneManager.LoadScene(Stage1SceneName);
	}

	private void PlayUiSelectForCurrentSelection()
	{
		if (lastSelectedObject != null && lastSelectedObject.GetComponent<Button>() != null)
			AudioManager.Instance?.PlaySfx2D(SoundId.UiButtonSelect);
	}

	private void QuitGame()
	{
		AudioManager.Instance?.PlaySfx2D(SoundId.UiButtonConfirm);
#if UNITY_EDITOR
		Debug.Log("[MainMenu] Quit requested. Application.Quit() is ignored in the Unity Editor.", this);
#endif
		Application.Quit();
	}

	private void DecreaseMasterVolume() => ChangeVolume(ref masterVolume, -1, MasterPreferenceKey, MasterMixerParameter, masterValueText);
	private void IncreaseMasterVolume() => ChangeVolume(ref masterVolume, 1, MasterPreferenceKey, MasterMixerParameter, masterValueText);
	private void DecreaseSfxVolume() => ChangeVolume(ref sfxVolume, -1, SfxPreferenceKey, SfxMixerParameter, sfxValueText);
	private void IncreaseSfxVolume() => ChangeVolume(ref sfxVolume, 1, SfxPreferenceKey, SfxMixerParameter, sfxValueText);
	private void DecreaseBgmVolume() => ChangeVolume(ref bgmVolume, -1, BgmPreferenceKey, BgmMixerParameter, bgmValueText);
	private void IncreaseBgmVolume() => ChangeVolume(ref bgmVolume, 1, BgmPreferenceKey, BgmMixerParameter, bgmValueText);

	private void ChangeVolume(ref int currentValue, int adjustment, string preferenceKey, string mixerParameter, TMP_Text valueText)
	{
		AudioManager.Instance?.PlaySfx2D(
			adjustment > 0 ? SoundId.UiButtonConfirm : SoundId.UiButtonBack
		);

		currentValue = Mathf.Clamp(currentValue + adjustment, MinimumVolume, MaximumVolume);
		PlayerPrefs.SetInt(preferenceKey, currentValue);
		PlayerPrefs.Save();
		SetVolumeLabel(valueText, currentValue);
		ApplyMixerVolume(mixerParameter, currentValue);
	}

	private void RefreshVolumeLabels()
	{
		SetVolumeLabel(masterValueText, masterVolume);
		SetVolumeLabel(sfxValueText, sfxVolume);
		SetVolumeLabel(bgmValueText, bgmVolume);
	}

	private void ApplyMixerVolume(string parameterName, int level)
	{
		if (mainMixer == null)
		{
			Debug.LogError("[MainMenu] ยังไม่ได้กำหนด Main AudioMixer ใน Inspector", this);
			return;
		}

		float normalizedLevel = (level - MinimumVolume) / (float)(MaximumVolume - MinimumVolume);
		float decibels = Mathf.Lerp(-40f, 0f, normalizedLevel);
		if (parameterName == SfxMixerParameter && AudioManager.Instance != null)
		{
			AudioManager.Instance.SetBaseSfxVolumeDb(decibels);
			return;
		}

		if (!mainMixer.SetFloat(parameterName, decibels))
			Debug.LogError($"[MainMenu] ไม่พบ Exposed AudioMixer parameter '{parameterName}'", this);
	}

	private static void SetVolumeLabel(TMP_Text label, int value)
	{
		if (label != null)
			label.text = value.ToString();
	}

	private void SetPanelActive(GameObject panel, bool isActive)
	{
		if (panel == null)
		{
			Debug.LogError("[MainMenu] มี Panel ที่ยังไม่ได้กำหนดใน Inspector", this);
			return;
		}

		panel.SetActive(isActive);
	}

	private void AddListener(Button button, UnityEngine.Events.UnityAction listener)
	{
		if (button == null)
		{
			Debug.LogError("[MainMenu] มี Button ที่ยังไม่ได้กำหนดใน Inspector", this);
			return;
		}

		button.onClick.AddListener(listener);
	}

	private static void RemoveListener(Button button, UnityEngine.Events.UnityAction listener)
	{
		if (button != null)
			button.onClick.RemoveListener(listener);
	}
}
