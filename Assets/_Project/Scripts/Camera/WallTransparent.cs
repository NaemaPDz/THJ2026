using UnityEngine;
using UnityEngine.Tilemaps;

public class WallTransparent : MonoBehaviour
{
	private Tilemap tilemap;

	[Header("Settings")]
	[Range(0f, 1f)] public float targetAlpha = 0.3f; // ความโปร่งใสตอนอยู่หลังกำแพง (0 ใสเลย, 1 ทึบ)
	public float fadeSpeed = 5f; // ความเร็วในการเฟดเข้า-ออก

	private float currentAlpha = 1f;

	void Start()
	{
		// ดึงคอมโพเนนต์ Tilemap จากวัตถุพ่อ (Walls)
		tilemap = GetComponentInParent<Tilemap>();
	}

	void Update()
	{
		// ค่อยๆ ปรับค่า Alpha ของ Tilemap ให้สมูทขึ้น
		Color currentColor = tilemap.color;
		float newAlpha = Mathf.MoveTowards(currentColor.a, currentAlpha, fadeSpeed * Time.deltaTime);
		tilemap.color = new Color(currentColor.r, currentColor.g, currentColor.b, newAlpha);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		// เมื่อวัตถุที่มี Tag ว่า "Player" เดินเข้ามาข้างหลังกำแพง
		if (other.CompareTag("Player"))
		{
			currentAlpha = targetAlpha;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		// เมื่อ "Player" เดินออกไป ให้กำแพงกลับมาทึบเหมือนเดิม
		if (other.CompareTag("Player"))
		{
			currentAlpha = 1f;
		}
	}
}
