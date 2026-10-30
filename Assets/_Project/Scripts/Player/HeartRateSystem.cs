
using UnityEngine;

public class HeartRateSystem : MonoBehaviour
{
	private const float MaximumHeartbeatSoundBpm = 180f;

	[Header("Heart Rate")]
	public float restingBPM = 80f;
	public float maxBPM = 180f;
	public float recoverBPM = 120f;
	public float recoveryDelay = 2f;

	[Header("Rates (BPM per second)")]
	public float sprintIncreaseRate = 25f;
	public float recoveryRate = 15f;
	[SerializeField, Min(0f)] private float sightBpm = 100f;
	[SerializeField, Min(0f)] private float chaseBpm = 120f;
	[SerializeField, Min(0.01f)] private float sightRiseDuration = 3f;
	[SerializeField, Min(0.01f)] private float chaseRiseDuration = 4f;
	[SerializeField, Min(0f)] private float threatRecoveryDelay = 3f;

	[Header("Heartbeat Sound")]
	[SerializeField, Min(0.01f)] private float soundEmitInterval = 1f;
	[SerializeField, Min(0f)] private float minimumHearingRadius = 1f;
	[SerializeField, Min(0f)] private float maximumHearingRadius = 2f;

	public float CurrentBPM { get; private set; }
	public bool CanSprint { get; private set; } = true;

	private float recoveryTimer;
	private float heartbeatTimer;
	private float soundEmitTimer;
	private bool wasStressed;
	private bool monsterVisible;
	private bool monsterChasing;
	private SacrificeEffectReceiver sacrificeEffects;

	private void Awake()
	{
		CurrentBPM = restingBPM;
		heartbeatTimer = 60f / Mathf.Max(1f, restingBPM);
		soundEmitTimer = soundEmitInterval;
		sacrificeEffects = GetComponent<SacrificeEffectReceiver>();
	}

	public void SetMonsterThreatState(bool visible, bool chasing)
	{
		monsterVisible = visible;
		monsterChasing = chasing;

		if (monsterVisible || monsterChasing)
			recoveryTimer = Mathf.Max(recoveryTimer, threatRecoveryDelay);
	}

	public bool Tick(bool wantsSprint, float deltaTime)
	{
		bool threatActive = monsterVisible || monsterChasing;
		float threatFloor = monsterChasing
			? Mathf.Max(restingBPM, chaseBpm)
			: monsterVisible ? Mathf.Max(restingBPM, sightBpm) : restingBPM;
		float threatRiseRate = monsterChasing
			? Mathf.Max(0f, (chaseBpm - restingBPM) / chaseRiseDuration)
			: Mathf.Max(0f, (sightBpm - restingBPM) / sightRiseDuration);

		if (wantsSprint && CanSprint)
		{
			// เพิ่ม Heart Rate ขณะ Sprint
			CurrentBPM = Mathf.MoveTowards(
				CurrentBPM,
				maxBPM,
				sprintIncreaseRate * deltaTime
			);

			// เริ่มนับเวลารอก่อนฟื้นตัวใหม่ทุกครั้งที่ Sprint
			recoveryTimer = Mathf.Max(recoveryTimer, recoveryDelay);

			if (CurrentBPM >= maxBPM)
			{
				CurrentBPM = maxBPM;
				CanSprint = false;
			}
		}
		else if (threatActive)
		{
			recoveryTimer = Mathf.Max(recoveryTimer, threatRecoveryDelay);
			float rate = CurrentBPM < threatFloor ? threatRiseRate : recoveryRate;
			CurrentBPM = Mathf.MoveTowards(CurrentBPM, threatFloor, rate * deltaTime);
		}
		else
		{
			// รอให้ครบเวลาก่อนลด Heart Rate
			if (recoveryTimer > 0f)
			{
				recoveryTimer -= deltaTime;
			}
			else
			{
				CurrentBPM = Mathf.MoveTowards(
					CurrentBPM,
					restingBPM,
					recoveryRate * deltaTime
				);
			}

		}

		// ปลดล็อก Sprint เมื่อ Heart Rate ลดถึงค่าที่กำหนด
		if (!CanSprint && CurrentBPM <= recoverBPM)
			CanSprint = true;

		heartbeatTimer -= deltaTime;
		if (heartbeatTimer <= 0f)
		{
			float bpmProgress = Mathf.InverseLerp(restingBPM, maxBPM, CurrentBPM);
			float heartbeatVolume = Mathf.Lerp(0.1f, 1f, bpmProgress) * 0.5f;
			AudioManager.Instance?.PlaySfx2D(SoundId.UiHeartbeat, heartbeatVolume);
			heartbeatTimer = 60f / Mathf.Max(1f, CurrentBPM);
		}

		soundEmitTimer -= deltaTime;
		if (soundEmitTimer <= 0f)
		{
			float bpmProgress = Mathf.InverseLerp(restingBPM, MaximumHeartbeatSoundBpm, CurrentBPM);
			float hearingRadius = Mathf.Lerp(
				minimumHearingRadius,
				maximumHearingRadius,
				bpmProgress
			);
			float soundMultiplier = sacrificeEffects != null
				? sacrificeEffects.SoundEmitterRadiusMultiplier
				: 1f;
			SoundEmitter.EmitSound(transform.position, hearingRadius * soundMultiplier);
			soundEmitTimer = soundEmitInterval;
		}

		bool isStressed = CurrentBPM >= recoverBPM;
		if (isStressed && !wasStressed)
			AudioManager.Instance?.PlaySfx2D(SoundId.PlayerStress);
		wasStressed = isStressed;

		return wantsSprint && CanSprint;
	}
}
