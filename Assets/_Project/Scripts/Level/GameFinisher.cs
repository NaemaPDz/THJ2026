using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class GameFinisher : MonoBehaviour
{
	private const string GoodEndingSceneName = "GoodEnding";
	private const string BadEndingSceneName = "BadEnding";
	private bool hasFinished;

	public void FinishRun()
	{
		if (hasFinished || !FaustRunState.GameplayActive)
			return;

		string endingScene = FaustRunState.IsSacrificed(FaustId.Heart)
			? BadEndingSceneName
			: GoodEndingSceneName;

		if (!Application.CanStreamedLevelBeLoaded(endingScene))
		{
			Debug.LogError($"[GameFinisher] Scene '{endingScene}' is missing from Build Settings.", this);
			return;
		}

		hasFinished = true;
		AudioManager.Instance?.PlaySfxAt(SoundId.PlayerInteract, transform.position);
		SceneManager.LoadScene(endingScene);
	}
}
