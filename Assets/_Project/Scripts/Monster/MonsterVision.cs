using UnityEngine;
using UnityEngine.AI;

public class MonsterVision : MonoBehaviour
{
	[Header("Target")]
	[SerializeField] private Transform target;

	[Header("Vision Settings")]
	[SerializeField, Min(0f)] private float visionRange = 4f;
	[SerializeField, Range(0f, 180f)]
	private float visionAngle = 90f;

	[SerializeField] private LayerMask obstacleMask;
	[SerializeField, Min(0.02f)]
	private float visionCheckInterval = 0.1f;

	private NavMeshAgent agent;
	private Vector2 facingDirection = Vector2.down;
	private float visionTimer;

	public Transform Target => target;
	public bool CanSeeTarget { get; private set; }

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();
	}

	private void Update()
	{
		UpdateFacingDirection();

		visionTimer -= Time.deltaTime;

		if (visionTimer > 0f)
			return;

		visionTimer = visionCheckInterval;
		CanSeeTarget = CheckVision();
	}

	public void SetTarget(Transform newTarget)
	{
		target = newTarget;
	}

	private void UpdateFacingDirection()
	{
		if (agent == null || !agent.enabled || !agent.isOnNavMesh)
			return;

		Vector2 movementDirection = agent.desiredVelocity;

		// จำทิศเดินล่าสุดเมื่อมอนหยุดนิ่ง
		if (movementDirection.sqrMagnitude > 0.01f)
		{
			facingDirection = movementDirection.normalized;
		}
	}

	private bool CheckVision()
	{
		if (target == null)
			return false;

		Vector2 origin = transform.position;
		Vector2 targetPosition = target.position;
		Vector2 toTarget = targetPosition - origin;

		// ตรวจระยะ
		if (toTarget.sqrMagnitude > visionRange * visionRange)
			return false;

		if (toTarget.sqrMagnitude < 0.0001f)
			return true;

		// ตรวจมุมกรวยสายตา
		float angle = Vector2.Angle(
			facingDirection,
			toTarget.normalized
		);

		if (angle > visionAngle * 0.5f)
			return false;

		// ตรวจว่ามีกำแพงขวางหรือไม่
		RaycastHit2D hit = Physics2D.Raycast(
			origin,
			toTarget.normalized,
			toTarget.magnitude,
			obstacleMask
		);

		return hit.collider == null;
	}

	private void OnDrawGizmosSelected()
	{
		Vector2 origin = transform.position;

		Vector2 direction = facingDirection;

		if (Application.isPlaying)
		{
			// ใช้ทิศที่จำไว้ระหว่างเล่น
			direction = facingDirection;
		}

		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(origin, visionRange);

		float halfAngle = visionAngle * 0.5f;

		Vector2 leftDirection = RotateVector(direction, -halfAngle);
		Vector2 rightDirection = RotateVector(direction, halfAngle);

		Gizmos.color = Color.cyan;

		Gizmos.DrawLine(
			origin,
			origin + leftDirection * visionRange
		);

		Gizmos.DrawLine(
			origin,
			origin + rightDirection * visionRange
		);
	}

	private Vector2 RotateVector(Vector2 vector, float degrees)
	{
		float radians = degrees * Mathf.Deg2Rad;

		float sin = Mathf.Sin(radians);
		float cos = Mathf.Cos(radians);

		return new Vector2(
			vector.x * cos - vector.y * sin,
			vector.x * sin + vector.y * cos
		);
	}
}
