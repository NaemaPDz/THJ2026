using UnityEngine;

public class HotbarTestSetup : MonoBehaviour
{
	[SerializeField] private WeaponData stunGunData;
	[SerializeField] private WeaponData batonData;
	[SerializeField] private WeaponData pipeData;

	private void Start()
	{
		InventorySystem inventory = GetComponent<InventorySystem>();

		if (inventory == null)
		{
			Debug.LogError("[Hotbar Test] ไม่พบ InventorySystem", this);
			return;
		}

		if (stunGunData == null ||
			batonData == null ||
			pipeData == null)
		{
			Debug.LogError(
				"[Hotbar Test] กรุณากำหนด WeaponData ให้ครบ",
				this
			);
			return;
		}

		inventory.TryAddItem(new WeaponInstance(stunGunData));
		inventory.TryAddItem(new WeaponInstance(batonData));
		inventory.TryAddItem(new WeaponInstance(pipeData));

		Debug.Log("[Hotbar Test] เพิ่มอาวุธ 3 ชิ้นแล้ว");
	}
}
