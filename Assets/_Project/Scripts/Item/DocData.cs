using UnityEngine;

[CreateAssetMenu(
	fileName = "NewDocData",
	menuName = "Thai Horror Jam/Items/Document Data"
)]
public class DocData : ItemData
{
	[Header("Document")]
	[TextArea(10, 30)] public string documentText;
}
