using UnityEngine;
using UnityEngine.UI;

public class HotbarUI : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private InventorySystem inventory;
	[SerializeField] private EquipmentSystem equipment;

	[Header("Hotbar Slots")]
	[SerializeField] private HotbarSlotUI[] slots = new HotbarSlotUI[5];


	private void Awake()
	{
		if (inventory == null)
			inventory = GetComponentInParent<InventorySystem>();

		if (equipment == null)
			equipment = GetComponentInParent<EquipmentSystem>();
	}

	private void OnEnable()
	{
		if (inventory != null)
			inventory.OnInventoryChanged += RefreshUI;

		if (equipment != null)
			equipment.OnEquippedItemChanged += OnEquippedItemChanged;

		RefreshUI();
	}

	private void OnDisable()
	{
		if (inventory != null)
			inventory.OnInventoryChanged -= RefreshUI;

		if (equipment != null)
			equipment.OnEquippedItemChanged -= OnEquippedItemChanged;
	}

	private void RefreshUI()
	{
		if (inventory == null)
			return;

		for (int i = 0; i < InventorySystem.SlotCount; i++)
		{
			ItemInstance item = inventory.GetItem(i);

			if (slots != null && i < slots.Length && slots[i] != null)
			{
				slots[i].SetSlotInfo(item);
			}
		}

		RefreshSelection();
	}

	private void OnEquippedItemChanged(ItemInstance item)
	{
		RefreshSelection();
	}

	private void RefreshSelection()
	{
		if (equipment == null)
			return;

		for (int i = 0; i < InventorySystem.SlotCount; i++)
		{
			if (i >= slots.Length || slots[i] == null)
				continue;

			slots[i].SetSelected(equipment.EquippedSlotIndex == i);
		}
	}
}
