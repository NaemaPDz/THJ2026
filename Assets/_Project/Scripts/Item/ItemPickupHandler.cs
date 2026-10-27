using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickupHandler : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private InventorySystem inventory;

	[Header("Pickup Settings")]
	[SerializeField] private float pickupDistance = 0.8f;
	[SerializeField] private Transform pickupOrigin;

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
	}

	private void Update()
	{
		if (inventory == null)
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
			WorldItem worldItem = hit.GetComponent<WorldItem>();

			if (worldItem == null)
				continue;

			if (worldItem.Item == null)
				continue;

			if (inventory.TryAddItem(worldItem.Item))
			{
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
