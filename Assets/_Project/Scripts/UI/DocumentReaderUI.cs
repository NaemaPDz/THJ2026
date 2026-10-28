using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates a lightweight reading panel over the gameplay UI.</summary>
public sealed class DocumentReaderUI : MonoBehaviour
{
	private GameObject panel;
	private TMP_Text titleText;
	private TMP_Text bodyText;
	private TMP_FontAsset documentFont;
	private ScrollRect bodyScrollRect;
	private RectTransform bodyViewport;

	public bool IsOpen => panel != null && panel.activeSelf;

	private void Awake()
	{
		foreach (TMP_FontAsset fontAsset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
		{
			if (fontAsset != null && fontAsset.name == "VT323-Regular SDF")
			{
				documentFont = fontAsset;
				break;
			}
		}

		CreatePanel();
	}

	private void OnDisable()
	{
		Close();
	}

	public void Open(DocData document)
	{
		if (document == null)
			return;

		if (panel == null)
			CreatePanel();

		titleText.text = document.itemName;
		bodyText.text = document.documentText;
		bodyText.ForceMeshUpdate();
		RectTransform bodyRect = bodyText.rectTransform;
		bodyRect.sizeDelta = new Vector2(
			0f,
			Mathf.Max(bodyViewport.rect.height, bodyText.preferredHeight + 24f)
		);
		bodyScrollRect.verticalNormalizedPosition = 1f;
		panel.SetActive(true);
	}

	public void Close()
	{
		if (panel != null)
			panel.SetActive(false);
	}

	private void CreatePanel()
	{
		var canvasObject = new GameObject("DocumentReaderCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
		Canvas canvas = canvasObject.GetComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		canvas.sortingOrder = 100;

		CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
		scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
		scaler.referenceResolution = new Vector2(1920f, 1080f);
		scaler.matchWidthOrHeight = 0.5f;

		panel = CreateRect("DocumentReaderPanel", canvasObject.transform);
		RectTransform panelRect = panel.GetComponent<RectTransform>();
		panelRect.anchorMin = new Vector2(0.15f, 0.12f);
		panelRect.anchorMax = new Vector2(0.85f, 0.88f);
		panelRect.offsetMin = Vector2.zero;
		panelRect.offsetMax = Vector2.zero;
		Image background = panel.AddComponent<Image>();
		background.color = new Color(0.055f, 0.045f, 0.035f, 0.96f);

		GameObject titleObject = CreateRect("Title", panel.transform);
		RectTransform titleRect = titleObject.GetComponent<RectTransform>();
		titleRect.anchorMin = new Vector2(0.06f, 0.82f);
		titleRect.anchorMax = new Vector2(0.94f, 0.96f);
		titleRect.offsetMin = Vector2.zero;
		titleRect.offsetMax = Vector2.zero;
		titleText = titleObject.AddComponent<TextMeshProUGUI>();
		ConfigureText(titleText, 42f, FontStyles.Bold, TextAlignmentOptions.Center);

		GameObject viewportObject = CreateRect("BodyViewport", panel.transform);
		bodyViewport = viewportObject.GetComponent<RectTransform>();
		bodyViewport.anchorMin = new Vector2(0.07f, 0.12f);
		bodyViewport.anchorMax = new Vector2(0.93f, 0.79f);
		bodyViewport.offsetMin = Vector2.zero;
		bodyViewport.offsetMax = Vector2.zero;
		viewportObject.AddComponent<RectMask2D>();
		bodyScrollRect = viewportObject.AddComponent<ScrollRect>();
		bodyScrollRect.viewport = bodyViewport;
		bodyScrollRect.vertical = true;
		bodyScrollRect.horizontal = false;
		bodyScrollRect.scrollSensitivity = 5f;

		GameObject bodyObject = CreateRect("Body", viewportObject.transform);
		RectTransform bodyRect = bodyObject.GetComponent<RectTransform>();
		bodyRect.anchorMin = new Vector2(0f, 1f);
		bodyRect.anchorMax = new Vector2(1f, 1f);
		bodyRect.pivot = new Vector2(0.5f, 1f);
		bodyRect.sizeDelta = new Vector2(0f, bodyViewport.rect.height);
		bodyText = bodyObject.AddComponent<TextMeshProUGUI>();
		ConfigureText(bodyText, 30f, FontStyles.Normal, TextAlignmentOptions.TopLeft);
		bodyText.enableWordWrapping = true;
		bodyScrollRect.content = bodyRect;

		GameObject hintObject = CreateRect("CloseHint", panel.transform);
		RectTransform hintRect = hintObject.GetComponent<RectTransform>();
		hintRect.anchorMin = new Vector2(0.05f, 0.025f);
		hintRect.anchorMax = new Vector2(0.95f, 0.09f);
		hintRect.offsetMin = Vector2.zero;
		hintRect.offsetMax = Vector2.zero;
		TMP_Text hintText = hintObject.AddComponent<TextMeshProUGUI>();
		ConfigureText(hintText, 20f, FontStyles.Italic, TextAlignmentOptions.Center);
		hintText.text = "Use / Esc - Close";

		panel.SetActive(false);
	}

	private static GameObject CreateRect(string objectName, Transform parent)
	{
		var child = new GameObject(objectName, typeof(RectTransform));
		child.transform.SetParent(parent, false);
		return child;
	}

	private void ConfigureText(TMP_Text text, float size, FontStyles style, TextAlignmentOptions alignment)
	{
		text.font = documentFont != null ? documentFont : TMP_Settings.defaultFontAsset;
		text.fontSize = size;
		text.fontStyle = style;
		text.alignment = alignment;
		text.color = new Color(0.91f, 0.87f, 0.77f, 1f);
		text.margin = new Vector4(8f, 8f, 8f, 8f);
	}
}
