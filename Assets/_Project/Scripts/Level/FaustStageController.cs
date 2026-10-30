using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class FaustStageController : MonoBehaviour
{
	private sealed class ChoiceCard
	{
		public GameObject Root;
		public Button Button;
		public TMP_Text Name;
		public TMP_Text GoodEffect;
		public TMP_Text BadEffect;
		public Image Icon;
	}

	private const string CatalogResourceName = "FaustCatalog";
	private FaustCatalog catalog;
	private Transform panelRoot;
	private Transform faustIconRoot;
	private Transform documentReaderRoot;
	private readonly Dictionary<FaustId, GameObject> sacrificeIcons = new();
	private readonly List<ChoiceCard> cards = new();
	private readonly List<FaustDefinition> currentOffers = new();
	private Button skipButton;
	private int currentStage;

	private void Awake()
	{
		FaustRunState.BeginStageSelection();
		catalog = Resources.Load<FaustCatalog>(CatalogResourceName);
		currentStage = GetStageNumber(SceneManager.GetActiveScene().name);
		panelRoot = transform.Find("FausePanel");
		BindFaustIcons();
		documentReaderRoot = transform.Find("DocumentReader");
		if (panelRoot == null)
			Debug.LogError("[Faust] FausePanel must be a child of the stage Canvas.", this);
		BindPanel();
	}

	private void Start()
	{
		if (catalog == null)
		{
			Debug.LogError("[Faust] Resources/FaustCatalog.asset is missing.", this);
			FinishSelection();
			return;
		}

		currentOffers.AddRange(FaustRunState.GetOffers(catalog, currentStage));
		if (currentOffers.Count > 0 && panelRoot != null)
			panelRoot.gameObject.SetActive(true);

		ApplyEffects(FaustRunState.BuildEffectProfile(catalog), true);
		RefreshFaustIcons();
		if (currentOffers.Count == 0)
		{
			FinishSelection();
			return;
		}

		for (int i = 0; i < cards.Count; i++)
		{
			ChoiceCard card = cards[i];
			if (card == null || card.Root == null || card.Button == null ||
				card.Name == null || card.GoodEffect == null || card.BadEffect == null)
			{
				Debug.LogError("[Faust] Panel choice card is incomplete.", this);
				FinishSelection();
				return;
			}

			bool hasOffer = i < currentOffers.Count;
			card.Root.SetActive(hasOffer);
			if (!hasOffer)
				continue;

			FaustDefinition definition = currentOffers[i];
			card.Name.text = definition.displayName;
			card.GoodEffect.text = definition.goodEffect;
			card.BadEffect.text = definition.badEffect;
			if (definition.icon != null && card.Icon != null)
				card.Icon.sprite = definition.icon;
			int choiceIndex = i;
			card.Button.onClick.AddListener(() => Choose(choiceIndex));
		}

		if (skipButton == null)
		{
			Debug.LogError("[Faust] Skip button is missing.", this);
			FinishSelection();
			return;
		}

		skipButton.onClick.AddListener(Skip);
	}

	private void BindPanel()
	{
		if (panelRoot == null)
			return;

		cards.Add(BindCard("Choice (1)"));
		cards.Add(BindCard("Choice (2)"));

		Transform skipTransform = panelRoot.Find("Button");
		if (skipTransform != null)
		{
			skipButton = skipTransform.GetComponent<Button>();
		}

		if (skipButton == null)
			Debug.LogError("[Faust] Skip button is missing from the Faust Panel.", this);
	}

	private void BindFaustIcons()
	{
		faustIconRoot = transform.Find("FaustIcon");
		if (faustIconRoot == null)
		{
			Debug.LogError("[Faust] FaustIcon must be a child of the stage Canvas.", this);
			return;
		}

		BindFaustIcon(FaustId.Touch, "Icon_Touch");
		BindFaustIcon(FaustId.Eyes, "Icon_Eye");
		BindFaustIcon(FaustId.Heart, "Icon_Heart");
		BindFaustIcon(FaustId.Ears, "Icon_Ear");
		BindFaustIcon(FaustId.Voice, "Icon_Voice");
	}

	private void BindFaustIcon(FaustId id, string childName)
	{
		Transform icon = faustIconRoot.Find(childName);
		if (icon == null)
		{
			Debug.LogError($"[Faust] Icon '{childName}' is missing from FaustIcon.", this);
			return;
		}

		sacrificeIcons[id] = icon.gameObject;
	}

	private void RefreshFaustIcons()
	{
		foreach (KeyValuePair<FaustId, GameObject> icon in sacrificeIcons)
			icon.Value.SetActive(!FaustRunState.IsSacrificed(icon.Key));
	}

	private ChoiceCard BindCard(string childName)
	{
		Transform cardTransform = panelRoot.Find(childName);
		if (cardTransform == null)
		{
			Debug.LogError($"[Faust] Choice card '{childName}' is missing.", this);
			return null;
		}

		Transform iconTransform = cardTransform.Find("Icon");
		return new ChoiceCard
		{
			Root = cardTransform.gameObject,
			Button = cardTransform.GetComponent<Button>(),
			Name = FindText(cardTransform, "FaustTitle"),
			GoodEffect = FindText(cardTransform, "GoodText"),
			BadEffect = FindText(cardTransform, "BadText"),
			Icon = iconTransform != null
				? iconTransform.GetComponentInChildren<Image>(true)
				: null
		};
	}

	private static TMP_Text FindText(Transform parent, string childName)
	{
		Transform child = parent.Find(childName);
		return child != null ? child.GetComponent<TMP_Text>() : null;
	}

	private void Choose(int offerIndex)
	{
		if (offerIndex < 0 || offerIndex >= currentOffers.Count)
			return;

		if (FaustRunState.Select(currentStage, currentOffers[offerIndex], catalog))
			FinishSelection();
	}

	private void Skip()
	{
		if (FaustRunState.Skip(currentStage, catalog))
			FinishSelection();
	}

	private void FinishSelection()
	{
		FaustEffectProfile profile = FaustRunState.BuildEffectProfile(catalog);
		ApplyEffects(profile, false);
		RefreshFaustIcons();
		if (panelRoot != null)
			panelRoot.gameObject.SetActive(false);
		FaustRunState.ReleaseGameplay();
	}

	private void ApplyEffects(FaustEffectProfile profile, bool keepSelectionPanelVisible)
	{
		SacrificeEffectReceiver receiver = FindFirstObjectByType<SacrificeEffectReceiver>();
		if (receiver != null)
			receiver.Apply(profile);

		HeartRateSystem heartRate = FindFirstObjectByType<HeartRateSystem>();
		if (heartRate != null && profile.SprintMaxBpmOverride > 0f)
			heartRate.maxBPM = profile.SprintMaxBpmOverride;

		FakeVisionEffect vision = Camera.main != null
			? Camera.main.GetComponent<FakeVisionEffect>()
			: FindFirstObjectByType<FakeVisionEffect>();
		if (vision != null)
		{
			vision.SetVisionAngleOffset(profile.VisionAngleOffset);
			vision.SetEyesSacrifice(
				profile.EyesNearVisionRadius,
				profile.EyesSacrificeStrength,
				profile.EyesConeLengthMultiplier,
				profile.EyesConeVisibilityMultiplier
			);
		}
		AudioManager.Instance?.SetSfxVolumeOffsetDb(profile.SfxVolumeOffsetDb);

		if (profile.HideHud)
		{
			Canvas hudCanvas = GetComponentInParent<Canvas>();
			if (hudCanvas != null)
			{
				foreach (Transform child in hudCanvas.transform)
				{
					bool keepChild = child == faustIconRoot || child == documentReaderRoot ||
						(keepSelectionPanelVisible && child == panelRoot);
					child.gameObject.SetActive(keepChild);
				}
			}
		}
		else if (profile.HideHeartbeatHud)
		{
			HeartRateUI heartRateUi = FindFirstObjectByType<HeartRateUI>();
			if (heartRateUi != null)
			{
				heartRateUi.enabled = false;
				heartRateUi.gameObject.SetActive(false);
			}
		}
	}

	private static int GetStageNumber(string sceneName)
	{
		if (sceneName == "Stage1") return 1;
		if (sceneName == "Stage2") return 2;
		if (sceneName == "Stage3") return 3;
		return 0;
	}
}
