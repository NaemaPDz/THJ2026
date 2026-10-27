using UnityEngine;
using UnityEngine.InputSystem;

public class ItemUseController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private EquipmentSystem equipment;
	[SerializeField] private MeleeAttack meleeAttack;
	[SerializeField] private RangeAttack rangeAttack;

	private void Awake()
	{
		if (equipment == null)
			equipment = GetComponent<EquipmentSystem>();

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

		Debug.Log($"[ItemUse] ใช้ไอเทม: {item.Data.itemName}");
	}

	private void UseWeapon(WeaponInstance weapon)
	{
		if (weapon == null)
			return;

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
