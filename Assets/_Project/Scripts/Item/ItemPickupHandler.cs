using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickupHandler : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private InventorySystem inventory;

	[Header("Pickup Settings")]
	[SerializeField] private float pickupDistance = 0.8f;
	[SerializeField] private Transform pickupOrigin;
	[SerializeField, Min(0f)] private float pickupHearingRadius = 2f;
	private SacrificeEffectReceiver sacrificeEffects;
	private DocumentReaderUI documentReader;

	private void Awake()
	{
		if (inventory == null)
			inventory = GetComponent<InventorySystem>();

		if (inventory == null)
		{
			Debug.LogError(
				"[ItemPickupHandler] ไม่พบ InventorySystem",
				this
			);
		}

		sacrificeEffects = GetComponent<SacrificeEffectReceiver>();
	}

	private void Start()
	{
		documentReader = FindFirstObjectByType<DocumentReaderUI>();
	}

	private void Update()
	{
		if (!FaustRunState.GameplayActive || (documentReader != null && documentReader.IsOpen))
			return;

		if (InputSystem.actions["Interact"].WasPressedThisFrame())
		{
			TryPickupItem();
		}
	}

	private void TryPickupItem()
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(
			pickupOrigin.position,
			pickupDistance
		);

		foreach (Collider2D hit in hits)
		{
			GameFinisher finisher = hit.GetComponent<GameFinisher>();
			if (finisher == null)
				continue;

			finisher.FinishRun();
			return;
		}

		foreach (Collider2D hit in hits)
		{
			WorldItem worldItem = hit.GetComponent<WorldItem>();

			if (worldItem == null)
				continue;

			if (worldItem.IsDocument)
			{
				float soundMultiplier = sacrificeEffects != null
					? sacrificeEffects.SoundEmitterRadiusMultiplier
					: 1f;
				SoundEmitter.EmitSound(transform.position, 2f * soundMultiplier);
				documentReader?.Open(worldItem.DocumentText);
				Debug.Log($"[Document] เปิดอ่าน {worldItem.name}", this);
				return;
			}

			if (worldItem.Item == null)
				continue;

			if (inventory == null)
				return;

			if (inventory.TryAddItem(worldItem.Item))
			{
				AudioManager.Instance?.PlaySfxAt(SoundId.PlayerItemGet, transform.position);
				float soundMultiplier = sacrificeEffects != null
					? sacrificeEffects.SoundEmitterRadiusMultiplier
					: 1f;
				SoundEmitter.EmitSound(
					transform.position,
					pickupHearingRadius * soundMultiplier
				);
				Debug.Log(
					$"[Pickup] เก็บ {worldItem.Item.Data.itemName} สำเร็จ",
					this
				);

				Destroy(worldItem.gameObject);
				return;
			}

			Debug.Log("[Pickup] Inventory เต็ม หรือเพิ่มไอเทมไม่ได้", this);
			return;
		}

		Debug.Log("[Pickup] ไม่มีไอเทมอยู่ใกล้พอ", this);
	}

	private void OnDrawGizmosSelected()
	{
		if (pickupOrigin == null)
			return;

		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(pickupOrigin.position, pickupDistance);
	}
}
