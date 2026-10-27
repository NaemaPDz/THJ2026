using System;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
	public const int SlotCount = 5;

	private readonly ItemInstance[] slots =
		new ItemInstance[SlotCount];

	public event Action OnInventoryChanged;

	public ItemInstance GetItem(int slotIndex)
	{
		if (!IsValidSlot(slotIndex))
			return null;

		return slots[slotIndex];
	}

	public bool TryAddItem(ItemInstance item)
	{
		if (item == null)
			return false;

		// ป้องกันการเพิ่ม Instance ตัวเดิมซ้ำ
		if (ContainsItem(item))
			return false;

		for (int i = 0; i < slots.Length; i++)
		{
			if (slots[i] != null)
				continue;

			slots[i] = item;
			OnInventoryChanged?.Invoke();
			return true;
		}

		// ไม่มีช่องว่าง
		return false;
	}

	public ItemInstance RemoveItem(int slotIndex)
	{
		if (!IsValidSlot(slotIndex))
			return null;

		ItemInstance item = slots[slotIndex];

		if (item == null)
			return null;

		slots[slotIndex] = null;
		OnInventoryChanged?.Invoke();

		// คืน Instance เดิม ไม่สร้างตัวใหม่
		return item;
	}

	public bool IsSlotEmpty(int slotIndex)
	{
		return GetItem(slotIndex) == null;
	}

	public bool IsFull()
	{
		foreach (ItemInstance item in slots)
		{
			if (item == null)
				return false;
		}

		return true;
	}

	private bool ContainsItem(ItemInstance item)
	{
		foreach (ItemInstance slotItem in slots)
		{
			if (ReferenceEquals(slotItem, item))
				return true;
		}

		return false;
	}

	private bool IsValidSlot(int slotIndex)
	{
		return slotIndex >= 0 && slotIndex < SlotCount;
	}
}
