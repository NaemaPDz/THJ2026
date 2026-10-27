using UnityEngine;

public class WorldItem : MonoBehaviour
{
	public ItemInstance Item { get; private set; }

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
