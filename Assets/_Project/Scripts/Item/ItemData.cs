using UnityEngine;

[CreateAssetMenu(
	fileName = "NewItemData",
	menuName = "Thai Horror Jam/Items/Item Data"
)]
public class ItemData : ScriptableObject
{
	[Header("Basic Info")]
	public string itemId;
	public string itemName;
	[TextArea] public string description;

	[Header("Visuals")]
	public Sprite icon;
	public GameObject worldPrefab;
}
