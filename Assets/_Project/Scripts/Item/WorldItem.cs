using UnityEngine;

public class WorldItem : MonoBehaviour
{
	[SerializeField] private ItemData itemData;

	public ItemInstance Item { get; private set; }

	private void Awake()
	{
		if (Item == null && itemData != null)
			Item = new ItemInstance(itemData);
	}

	public void Initialize(ItemInstance item)
	{
		if (item == null)
		{
			Debug.LogError("[WorldItem] Item is null", this);
			return;
		}

		Item = item;

		// แสดงชื่อไอเทมไว้ใน Console ระหว่างทดสอบ
		Debug.Log($"[WorldItem] วางไอเทมบนพื้น: {Item.Data.itemName}", this);
	}
}
