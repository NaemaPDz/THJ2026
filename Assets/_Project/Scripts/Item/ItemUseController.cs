using UnityEngine;
using UnityEngine.InputSystem;

public class ItemUseController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private EquipmentSystem equipment;
	[SerializeField] private InventorySystem inventory;
	[SerializeField] private MeleeAttack meleeAttack;
	[SerializeField] private RangeAttack rangeAttack;
	[SerializeField, Min(0f)] private float keyUseRange = 1f;
	[SerializeField, Min(0f)] private float keyUseHearingRadius = 5f;

	private void Awake()
	{
		if (equipment == null)
			equipment = GetComponent<EquipmentSystem>();
		if (inventory == null)
			inventory = GetComponent<InventorySystem>();

		if (equipment == null)
		{
			Debug.LogError(
				"[ItemUseController] ไม่พบ EquipmentSystem",
				this
			);
		}
	}

	private void Update()
	{
		if (equipment == null)
			return;

		if (InputSystem.actions["UseItem"].WasPressedThisFrame())
		{
			TryUseEquippedItem();
		}
	}

	private void TryUseEquippedItem()
	{
		ItemInstance item = equipment.EquippedItem;

		if (item == null)
		{
			Debug.Log("[ItemUse] ไม่มีไอเทมที่กำลังถือ");
			return;
		}

		if (item is WeaponInstance weapon)
		{
			UseWeapon(weapon);
			return;
		}

		if (item.Data is KeyData key)
		{
			UseKey(key, item);
			return;
		}

		Debug.Log($"[ItemUse] ใช้ไอเทม: {item.Data.itemName}");
	}

	private void UseKey(KeyData key, ItemInstance item)
	{
		SoundEmitter.EmitSound(transform.position, keyUseHearingRadius);

		KeyDoor door = FindNearestDoor();
		if (door == null)
		{
			Debug.Log("[Key] ใช้กุญแจไม่ได้: ไม่มีประตูอยู่ใกล้พอ", this);
			return;
		}

		if (!door.CanOpenWith(key))
		{
			Debug.Log("[Key] ใช้กุญแจไม่ได้: กุญแจไม่ตรงกับประตู", this);
			return;
		}

		int slotIndex = equipment.EquippedSlotIndex;
		if (inventory == null || inventory.GetItem(slotIndex) != item)
		{
			Debug.LogWarning("[Key] ใช้กุญแจไม่ได้: สถานะ Inventory ไม่ตรงกับ Equipment", this);
			return;
		}

		ItemInstance consumedItem = inventory.RemoveItem(slotIndex);
		if (consumedItem != item)
		{
			Debug.LogError("[Key] ใช้กุญแจไม่ได้: นำกุญแจออกจาก Inventory ไม่สำเร็จ", this);
			return;
		}

		equipment.ClearEquipment();
		door.Open();
		Debug.Log($"[Key] ใช้ {key.itemName} เปิดประตูสำเร็จ", this);
	}

	private KeyDoor FindNearestDoor()
	{
		Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, keyUseRange);
		KeyDoor nearestDoor = null;
		float nearestDistance = float.MaxValue;

		foreach (Collider2D hit in hits)
		{
			KeyDoor door = hit.GetComponentInParent<KeyDoor>();
			if (door == null)
				continue;

			float distance = Vector2.Distance(transform.position, door.transform.position);
			if (distance >= nearestDistance)
				continue;

			nearestDistance = distance;
			nearestDoor = door;
		}

		return nearestDoor;
	}

	private void UseWeapon(WeaponInstance weapon)
	{
		if (weapon == null)
			return;

		SoundEmitter.EmitSound(transform.position, 5f);

		if (weapon.WeaponData.attackType == AttackType.Melee)
		{
			if (meleeAttack != null)
				meleeAttack.TryAttack(weapon, GetAimDirection());

			return;
		}

		if (weapon.WeaponData.attackType == AttackType.Projectile)
		{
			if (rangeAttack != null)
				rangeAttack.TryAttack(weapon, GetAimDirection());

			return;
		}
	}

	private Vector2 GetAimDirection()
	{
		if (Camera.main == null || Mouse.current == null)
			return Vector2.right;

		Vector2 mousePosition = Mouse.current.position.ReadValue();

		Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(
			new Vector3(
				mousePosition.x,
				mousePosition.y,
				Mathf.Abs(transform.position.z - Camera.main.transform.position.z)
			)
		);

		Vector2 direction = (Vector2)(mouseWorld - transform.position);

		return direction.sqrMagnitude > 0.001f
			? direction.normalized
			: Vector2.right;
	}
}
