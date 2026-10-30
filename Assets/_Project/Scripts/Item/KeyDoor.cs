using UnityEngine;

public class KeyDoor : MonoBehaviour
{
	[SerializeField] private KeyData requiredKey;
	[SerializeField] private Sprite openDoor;

	private SpriteRenderer spriteRenderer;
	private Collider2D doorCollider;

	private void Awake()
	{
		spriteRenderer = GetComponent<SpriteRenderer>();
		doorCollider = GetComponent<Collider2D>();
	}

	public bool CanOpenWith(KeyData key)
	{
		return requiredKey != null && key == requiredKey;
	}

	public void Open()
	{
		Debug.Log($"[Door] เปิดประตู {gameObject.name} แล้ว", this);

		spriteRenderer.sprite = openDoor;
		doorCollider.enabled = false;
	}
}
