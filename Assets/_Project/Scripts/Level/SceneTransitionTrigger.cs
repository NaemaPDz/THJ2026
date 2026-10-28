using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionTrigger : MonoBehaviour
{
	[SerializeField] private string destinationScene;

	private bool hasTriggered;

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (hasTriggered || !other.CompareTag("Player"))
			return;

		if (string.IsNullOrWhiteSpace(destinationScene))
		{
			Debug.LogError("[SceneTransition] ยังไม่ได้กำหนด Scene ปลายทาง", this);
			return;
		}

		if (!Application.CanStreamedLevelBeLoaded(destinationScene))
		{
			Debug.LogError(
				$"[SceneTransition] ไม่พบ Scene '{destinationScene}' ใน Build Settings",
				this
			);
			return;
		}

		hasTriggered = true;
		SceneManager.LoadScene(destinationScene);
	}
}
