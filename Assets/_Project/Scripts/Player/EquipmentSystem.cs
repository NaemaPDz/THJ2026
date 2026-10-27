using System;
using UnityEngine;

public class EquipmentSystem : MonoBehaviour
{
	[SerializeField] private InventorySystem inventory;

	public int EquippedSlotIndex { get; private set; } = -1;

	public ItemInstance EquippedItem { get; private set; }

	public event Action<ItemInstance> OnEquippedItemChanged;

	private void Awake()
	{
		if (inventory == null)
		{
			inventory = GetComponent<InventorySystem>();
		}

		if (inventory == null)
		{
			Debug.LogError(
				"[EquipmentSystem] ไม่พบ InventorySystem",
				this
			);
		}
	}

	public bool TryEquipSlot(int slotIndex)
	{
		if (inventory == null)
			return false;

		if (slotIndex < 0 || slotIndex >= InventorySystem.SlotCount)
			return false;

		ItemInstance item = inventory.GetItem(slotIndex);

		// ช่องว่าง: ไม่เปลี่ยนไอเทมที่ถืออยู่
		if (item == null)
			return false;

		EquippedSlotIndex = slotIndex;
		EquippedItem = item;

		OnEquippedItemChanged?.Invoke(EquippedItem);

		Debug.Log(
			$"[EquipmentSystem] Equipped slot {slotIndex + 1}: " +
			$"{EquippedItem.Data.itemName}",
			this
		);

		return true;
	}


	public ItemInstance UnequipCurrentItem()
	{
		ItemInstance item = EquippedItem;

		if (item == null)
			return null;

		EquippedItem = null;
		EquippedSlotIndex = -1;

		OnEquippedItemChanged?.Invoke(null);

		return item;
	}


	public void ClearEquipment()
	{
		EquippedSlotIndex = -1;
		EquippedItem = null;

		OnEquippedItemChanged?.Invoke(null);
	}
}