using UnityEngine;

public class KeyDoor : MonoBehaviour
{
	[SerializeField] private KeyData requiredKey;

	public bool CanOpenWith(KeyData key)
	{
		return requiredKey != null && key == requiredKey;
	}

	public void Open()
	{
		Debug.Log($"[Door] เปิดประตู {gameObject.name} แล้ว", this);
		gameObject.SetActive(false);
	}
}
