using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class EndingCutsceneController : MonoBehaviour
{
	private const string GoodEndingSceneName = "GoodEnding";
	private const string BadEndingSceneName = "BadEnding";
	private const string MainMenuSceneName = "MainMenu";
	private const float GoodEndingHoldDuration = 2f;
	private const float StateStartTimeout = 1f;

	private Button confirmButton;
	private TMP_InputField nameInput;
	private GameObject nameEnter;
	private GameObject cutsceneRoot;
	private Animator cutsceneAnimator;
	private TMP_Text playerNameText;
	private TMP_Text dateText;
	private Button okButton;
	private string sceneName;
	private bool sequenceStarted;
	private bool returningToMenu;

	private bool IsGoodEnding => sceneName == GoodEndingSceneName;
	private string EndingStateName => IsGoodEnding ? GoodEndingSceneName : BadEndingSceneName;

	private void Awake()
	{
		sceneName = SceneManager.GetActiveScene().name;
		if (sceneName != GoodEndingSceneName && sceneName != BadEndingSceneName)
		{
			Debug.LogError($"[Ending] Unsupported ending scene '{sceneName}'.", this);
			enabled = false;
			return;
		}

		Transform nameEnterTransform = FindInActiveScene("NameEnter");
		Transform cutsceneTransform = FindInActiveScene("CutScene");
		Transform confirmTransform = nameEnterTransform != null
			? nameEnterTransform.Find("ConfirmButton")
			: null;
		Transform inputTransform = nameEnterTransform != null
			? nameEnterTransform.Find("InputField (TMP)")
			: null;

		if (nameEnterTransform == null || cutsceneTransform == null ||
			confirmTransform == null || inputTransform == null)
		{
			Debug.LogError("[Ending] NameEnter, ConfirmButton, InputField (TMP), and CutScene are required.", this);
			enabled = false;
			return;
		}

		nameEnter = nameEnterTransform.gameObject;
		cutsceneRoot = cutsceneTransform.gameObject;
		confirmButton = confirmTransform.GetComponent<Button>();
		nameInput = inputTransform.GetComponent<TMP_InputField>();
		cutsceneAnimator = cutsceneTransform.GetComponent<Animator>();

		if (confirmButton == null || nameInput == null || cutsceneAnimator == null)
		{
			Debug.LogError("[Ending] ConfirmButton, InputField (TMP), or CutScene Animator is missing its component.", this);
			enabled = false;
			return;
		}

		if (IsGoodEnding)
		{
			Transform nameTextTransform = cutsceneTransform.Find("NameText");
			Transform dateTransform = cutsceneTransform.Find("Date");
			playerNameText = nameTextTransform != null ? nameTextTransform.GetComponent<TMP_Text>() : null;
			dateText = dateTransform != null ? dateTransform.GetComponent<TMP_Text>() : null;
			if (playerNameText == null || dateText == null)
			{
				Debug.LogError("[Ending] GoodEnding requires NameText and Date TMP text objects under CutScene.", this);
				enabled = false;
				return;
			}
		}
		else
		{
			Transform okTransform = cutsceneTransform.Find("OkButton");
			okButton = okTransform != null ? okTransform.GetComponent<Button>() : null;
			if (okButton == null)
			{
				Debug.LogError("[Ending] BadEnding requires an OkButton under CutScene.", this);
				enabled = false;
				return;
			}

			okButton.onClick.AddListener(ReturnToMainMenu);
			okButton.gameObject.SetActive(false);
		}

		confirmButton.onClick.AddListener(BeginEnding);
		nameEnter.SetActive(true);
		cutsceneRoot.SetActive(false);
	}

	private void OnDestroy()
	{
		if (confirmButton != null)
			confirmButton.onClick.RemoveListener(BeginEnding);
		if (okButton != null)
			okButton.onClick.RemoveListener(ReturnToMainMenu);
	}

	private void BeginEnding()
	{
		if (sequenceStarted)
			return;

		sequenceStarted = true;
		confirmButton.interactable = false;

		if (IsGoodEnding)
		{
			string enteredName = nameInput.text.Trim();
			playerNameText.text = string.IsNullOrWhiteSpace(enteredName) ? "Player" : enteredName;
			dateText.text = DateTime.Now.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
		}

		nameEnter.SetActive(false);
		cutsceneRoot.SetActive(true);
		cutsceneAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
		cutsceneAnimator.Play(Animator.StringToHash($"Base Layer.{EndingStateName}"), 0, 0f);
		StartCoroutine(WaitForCutsceneThenFinish());
	}

	private IEnumerator WaitForCutsceneThenFinish()
	{
		float elapsed = 0f;
		AnimatorStateInfo state = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
		while (!state.IsName($"Base Layer.{EndingStateName}") && elapsed < StateStartTimeout)
		{
			elapsed += Time.unscaledDeltaTime;
			yield return null;
			state = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
		}

		if (state.IsName($"Base Layer.{EndingStateName}"))
		{
			float animationTimeout = state.length + 1f;
			elapsed = 0f;
			while (cutsceneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f &&
				elapsed < animationTimeout)
			{
				elapsed += Time.unscaledDeltaTime;
				yield return null;
			}
		}
		else
		{
			Debug.LogWarning($"[Ending] Animator did not enter state '{EndingStateName}'.", this);
		}

		if (IsGoodEnding)
		{
			yield return new WaitForSecondsRealtime(GoodEndingHoldDuration);
			ReturnToMainMenu();
		}
		else if (okButton != null)
		{
			okButton.gameObject.SetActive(true);
		}
	}

	private void ReturnToMainMenu()
	{
		if (returningToMenu)
			return;

		if (!Application.CanStreamedLevelBeLoaded(MainMenuSceneName))
		{
			Debug.LogError($"[Ending] Scene '{MainMenuSceneName}' is missing from Build Settings.", this);
			return;
		}

		returningToMenu = true;
		SceneManager.LoadScene(MainMenuSceneName);
	}

	private static Transform FindInActiveScene(string objectName)
	{
		UnityEngine.SceneManagement.Scene activeScene = SceneManager.GetActiveScene();
		Transform[] sceneTransforms = FindObjectsByType<Transform>(
			FindObjectsInactive.Include,
			FindObjectsSortMode.None
		);

		foreach (Transform candidate in sceneTransforms)
		{
			if (candidate.gameObject.scene == activeScene && candidate.name == objectName)
				return candidate;
		}

		return null;
	}
}
