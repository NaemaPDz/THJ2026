using UnityEngine;

public class WorldItem : MonoBehaviour
{
	[SerializeField] private ItemData itemData;

	[Header("Document")]
	[SerializeField] private bool isDocument;
	[SerializeField, TextArea(5, 20)] private string documentText;

	public ItemInstance Item { get; private set; }
	public bool IsDocument => isDocument || Item?.Data is DocData;
	public string DocumentText => !string.IsNullOrEmpty(documentText)
		? documentText
		: (Item?.Data as DocData)?.documentText ?? string.Empty;

	private void Awake()
	{
		if (Item == null && itemData != null)
		{
			if (itemData is WeaponData weaponData)
			{
				Item = new WeaponInstance(weaponData);
			}
			else if (itemData is DocData docData)
			{
				Item = new ItemInstance(docData);
			}
			else
			{
				Item = new ItemInstance(itemData);
			}
		}	

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
