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
	[SerializeField, Min(0f)] private float keyUseHearingRadius = 10f;
	private SacrificeEffectReceiver sacrificeEffects;
	private DocumentReaderUI documentReader;

	private void Awake()
	{
		if (equipment == null)
			equipment = GetComponent<EquipmentSystem>();
		if (inventory == null)
			inventory = GetComponent<InventorySystem>();
		sacrificeEffects = GetComponent<SacrificeEffectReceiver>();

		if (equipment == null)
		{
			Debug.LogError(
				"[ItemUseController] ไม่พบ EquipmentSystem",
				this
			);
		}
	}

	private void Start()
	{
		documentReader = FindFirstObjectByType<DocumentReaderUI>();
	}

	private void Update()
	{
		if (documentReader != null && documentReader.IsOpen)
		{
			if (InputSystem.actions["UseItem"].WasPressedThisFrame() ||
				InputSystem.actions["Cancel"].WasPressedThisFrame())
				documentReader.Close();
			return;
		}

		if (!FaustRunState.GameplayActive || equipment == null)
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
			if (UseKey(key, item))
				PlayUseSound(item.Data);
			return;
		}

		if (item.Data is DocData document)
		{
			SoundEmitter.EmitSound(
				transform.position,
				GetAdjustedHearingRadius(2f)
			);
			documentReader?.Open(document);
			return;
		}

		SoundEmitter.EmitSound(
			transform.position,
			GetAdjustedHearingRadius(3f)
		);
		PlayUseSound(item.Data);
		Debug.Log($"[ItemUse] ใช้ไอเทม: {item.Data.itemName}");
	}

	private bool UseKey(KeyData key, ItemInstance item)
	{
		SoundEmitter.EmitSound(
			transform.position,
			GetAdjustedHearingRadius(keyUseHearingRadius)
		);

		KeyDoor door = FindNearestDoor();
		if (door == null)
		{
			Debug.Log("[Key] ใช้กุญแจไม่ได้: ไม่มีประตูอยู่ใกล้พอ", this);
			return false;
		}

		if (!door.CanOpenWith(key))
		{
			Debug.Log("[Key] ใช้กุญแจไม่ได้: กุญแจไม่ตรงกับประตู", this);
			return false;
		}

		int slotIndex = equipment.EquippedSlotIndex;
		if (inventory == null || inventory.GetItem(slotIndex) != item)
		{
			Debug.LogWarning("[Key] ใช้กุญแจไม่ได้: สถานะ Inventory ไม่ตรงกับ Equipment", this);
			return false;
		}

		ItemInstance consumedItem = inventory.RemoveItem(slotIndex);
		if (consumedItem != item)
		{
			Debug.LogError("[Key] ใช้กุญแจไม่ได้: นำกุญแจออกจาก Inventory ไม่สำเร็จ", this);
			return false;
		}

		equipment.ClearEquipment();
		door.Open();
		Debug.Log($"[Key] ใช้ {key.itemName} เปิดประตูสำเร็จ", this);
		return true;
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

		SoundEmitter.EmitSound(transform.position, GetAdjustedHearingRadius(7f));

		if (weapon.WeaponData.attackType == AttackType.Melee)
		{
			if (meleeAttack != null && meleeAttack.TryAttack(weapon, GetAimDirection()))
				PlayUseSound(weapon.WeaponData);

			return;
		}

		if (weapon.WeaponData.attackType == AttackType.Projectile)
		{
			if (rangeAttack != null && rangeAttack.TryAttack(weapon, GetAimDirection()))
				PlayUseSound(weapon.WeaponData);

			return;
		}
	}

	private float GetAdjustedHearingRadius(float baseRadius)
	{
		return baseRadius * (sacrificeEffects != null
			? sacrificeEffects.SoundEmitterRadiusMultiplier
			: 1f);
	}

	private void PlayUseSound(ItemData itemData)
	{
		if (itemData != null)
			AudioManager.Instance?.PlaySfxAt(itemData.useSound, transform.position);
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
