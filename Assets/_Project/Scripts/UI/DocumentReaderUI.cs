using TMPro;
using UnityEngine;

/// <summary>Controls the document panel authored in the shared Canvas prefab.</summary>
public sealed class DocumentReaderUI : MonoBehaviour
{
	private const string PanelName = "DocumentReader";
	private const string TextName = "Text";

	private GameObject panel;
	private TMP_Text documentText;
	private bool pausedGameplay;
	private float previousTimeScale;

	public bool IsOpen => panel != null && panel.activeSelf;

	private void Awake()
	{
		Transform panelTransform = transform.Find(PanelName);
		if (panelTransform == null)
		{
			Debug.LogError($"[DocumentReader] Canvas is missing '{PanelName}'.", this);
			return;
		}

		panel = panelTransform.gameObject;
		Transform textTransform = panelTransform.Find(TextName);
		documentText = textTransform != null
			? textTransform.GetComponent<TMP_Text>()
			: null;

		if (documentText == null)
			Debug.LogError($"[DocumentReader] '{PanelName}/{TextName}' needs a TMP_Text component.", this);

		panel.SetActive(false);
	}

	private void OnDisable()
	{
		Close();
	}

	public void Open(DocData document)
	{
		if (document == null)
			return;

		Open(document.documentText);
	}

	public void Open(string body)
	{
		if (!FaustRunState.GameplayActive || documentText == null)
			return;

		if (IsOpen)
		{
			documentText.text = body;
			return;
		}

		previousTimeScale = FaustRunState.PauseGameplayForDocument();
		pausedGameplay = true;
		documentText.text = body;
		panel.SetActive(true);
	}

	public void Close()
	{
		if (panel != null)
			panel.SetActive(false);

		if (!pausedGameplay)
			return;

		pausedGameplay = false;
		FaustRunState.ResumeGameplayAfterDocument(previousTimeScale);
	}
}
