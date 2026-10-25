
using UnityEngine;

public class HeartRateSystem : MonoBehaviour
{
	[Header("Heart Rate")]
	public float restingBPM = 80f;
	public float maxBPM = 180f;
	public float recoverBPM = 120f;
	public float recoveryDelay = 2f;

	[Header("Rates (BPM per second)")]
	public float sprintIncreaseRate = 25f;
	public float recoveryRate = 15f;

	public float CurrentBPM { get; private set; }
	public bool CanSprint { get; private set; } = true;

	private float recoveryTimer;

	private void Awake()
	{
		CurrentBPM = restingBPM;
	}

	public bool Tick(bool wantsSprint, float deltaTime)
	{
		if (wantsSprint && CanSprint)
		{
			// เพิ่ม Heart Rate ขณะ Sprint
			CurrentBPM = Mathf.MoveTowards(
				CurrentBPM,
				maxBPM,
				sprintIncreaseRate * deltaTime
			);

			// เริ่มนับเวลารอก่อนฟื้นตัวใหม่ทุกครั้งที่ Sprint
			recoveryTimer = recoveryDelay;

			if (CurrentBPM >= maxBPM)
			{
				CurrentBPM = maxBPM;
				CanSprint = false;
			}
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

			// ปลดล็อก Sprint เมื่อ Heart Rate ลดถึงค่าที่กำหนด
			if (!CanSprint && CurrentBPM <= recoverBPM)
			{
				CanSprint = true;
			}
		}

		return wantsSprint && CanSprint;
	}
}