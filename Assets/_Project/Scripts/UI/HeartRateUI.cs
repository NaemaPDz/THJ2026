using TMPro;
using UnityEngine;

public class HeartRateUI : MonoBehaviour
{
	private const float BaseAnimationBpm = 80f;
	private const float TripleSpeedBpm = 180f;
	private const float MaximumAnimationSpeed = 3f;

	public HeartRateSystem heartRate;
	private Animator heartbeatAnimator;

	private void Awake()
	{
		heartbeatAnimator = GetComponent<Animator>();
	}

	private void Update()
	{
		if (heartRate == null)
			return;

		float bpm = heartRate.CurrentBPM;
		if (heartbeatAnimator != null)
		{
			float bpmProgress = Mathf.InverseLerp(BaseAnimationBpm, TripleSpeedBpm, bpm);
			heartbeatAnimator.speed = Mathf.Lerp(1f, MaximumAnimationSpeed, bpmProgress);
		}
	}
}
