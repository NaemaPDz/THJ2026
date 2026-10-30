using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private InventorySystem inventory;
	[SerializeField] private EquipmentSystem equipment;
	[SerializeField] private Transform dropOrigin;

	[Header("Drop Settings")]
	[SerializeField] private float dropDistance = 0.8f;

	private void Awake()
	{
		if (inventory == null)
			inventory = GetComponent<InventorySystem>();

		if (equipment == null)
			equipment = GetComponent<EquipmentSystem>();

		if (dropOrigin == null)
			dropOrigin = transform;
	}

	private void Update()
	{
		if (!FaustRunState.GameplayActive || equipment == null || inventory == null)
			return;

		if (InputSystem.actions["Slot1"].WasPressedThisFrame())
			SelectSlot(0);

		if (InputSystem.actions["Slot2"].WasPressedThisFrame())
			SelectSlot(1);

		if (InputSystem.actions["Slot3"].WasPressedThisFrame())
			SelectSlot(2);

		if (InputSystem.actions["Slot4"].WasPressedThisFrame())
			SelectSlot(3);

		if (InputSystem.actions["Slot5"].WasPressedThisFrame())
			SelectSlot(4);

		if (InputSystem.actions["Drop"].WasPressedThisFrame())
			DropEquippedItem();
	}

	private void SelectSlot(int slotIndex)
	{
		if (equipment.TryEquipSlot(slotIndex))
		{
			Debug.Log($"[Hotbar] เลือกช่อง {slotIndex + 1}");
		}
	}

	private void DropEquippedItem()
	{
		ItemInstance item = equipment.EquippedItem;
		int slotIndex = equipment.EquippedSlotIndex;

		if (item == null || slotIndex < 0)
		{
			Debug.Log("[Drop] ไม่มีไอเทมที่กำลังถือ");
			return;
		}

		// ตรวจสอบว่าไอเทมยังอยู่ในช่องที่กำลังเลือก
		if (inventory.GetItem(slotIndex) != item)
		{
			Debug.LogWarning("[Drop] สถานะ Inventory ไม่ตรงกับ Equipment");
			return;
		}

		GameObject prefab = item.Data.worldPrefab;

		if (prefab == null)
		{
			Debug.LogWarning(
				$"[Drop] ยังไม่ได้กำหนด World Prefab ให้ {item.Data.itemName}"
			);
			return;
		}

		if (!prefab.TryGetComponent<WorldItem>(out _))
		{
			Debug.LogError(
				"[Drop] Prefab ต้องมี WorldItem Component"
			);
			return;
		}

		Vector3 dropPosition = GetDropPosition();

		// สร้างวัตถุบนพื้นก่อน โดยยังไม่เปลี่ยน Instance
		GameObject droppedObject =
			Instantiate(prefab, dropPosition, Quaternion.identity);

		WorldItem worldItem = droppedObject.GetComponent<WorldItem>();
		worldItem.Initialize(item);

		// นำ Instance ตัวเดิมออกจาก Inventory
		ItemInstance removedItem = inventory.RemoveItem(slotIndex);

		if (removedItem != item)
		{
			Destroy(droppedObject);
			Debug.LogError("[Drop] นำไอเทมออกจาก Inventory ไม่สำเร็จ");
			return;
		}

		// เคลียร์ Equipment โดยไม่สร้าง Instance ใหม่
		ItemInstance unequippedItem = equipment.UnequipCurrentItem();

		if (unequippedItem != item)
		{
			Debug.LogError("[Drop] Equipment มีสถานะไม่ตรงกับไอเทมที่ทิ้ง");
			return;
		}

		AudioManager.Instance?.PlaySfxAt(SoundId.PlayerDropItem, dropPosition);
		Debug.Log($"[Drop] ทิ้ง {item.Data.itemName} สำเร็จ");
	}

	private Vector3 GetDropPosition()
	{
		Vector3 origin = dropOrigin.position;

		Camera cam = Camera.main;

		if (cam == null || Mouse.current == null)
			return origin;

		Vector2 mousePosition = Mouse.current.position.ReadValue();

		float cameraDepth = Mathf.Abs(
			origin.z - cam.transform.position.z
		);

		Vector3 mouseWorld = cam.ScreenToWorldPoint(
			new Vector3(mousePosition.x, mousePosition.y, cameraDepth)
		);

		// ใช้เฉพาะทิศทางบนระนาบ 2D
		Vector2 direction = (Vector2)(mouseWorld - origin);

		if (direction.sqrMagnitude < 0.001f)
			direction = Vector2.right;

		direction.Normalize();

		Vector3 position = origin + (Vector3)(direction * dropDistance);

		// ให้ไอเทมอยู่ระนาบ Z เดียวกับตัวละคร
		position.z = origin.z;

		return position;
	}
}
