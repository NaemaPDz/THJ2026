using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class MonsterMovement : MonoBehaviour
{
	private NavMeshAgent agent;

	public bool IsMoving =>
		agent != null &&
		agent.enabled &&
		agent.isOnNavMesh &&
		agent.hasPath &&
		agent.velocity.sqrMagnitude > 0.01f;

	public bool HasArrived
	{
		get
		{
			if (!agent.enabled || !agent.isOnNavMesh)
				return false;

			if (agent.pathPending)
				return false;

			return agent.remainingDistance
				<= agent.stoppingDistance + 0.05f;
		}
	}

	private void Awake()
	{
		agent = GetComponent<NavMeshAgent>();

		// สำหรับเกม Top-down 2D บนระนาบ XY
		agent.updateRotation = false;
		agent.updateUpAxis = false;
	}

	public bool MoveTo(Vector3 destination)
	{
		if (!agent.enabled || !agent.isOnNavMesh)
			return false;

		// รักษาระดับ Z ของมอนสเตอร์
		destination.z = transform.position.z;

		agent.isStopped = false;
		return agent.SetDestination(destination);
	}

	public void Stop()
	{
		if (agent == null || !agent.enabled ||
			!agent.isOnNavMesh)
			return;

		agent.isStopped = true;
		agent.ResetPath();
	}

	public void Resume()
	{
		if (agent == null || !agent.enabled ||
			!agent.isOnNavMesh)
			return;

		agent.isStopped = false;
	}

	public void SetSpeed(float speed)
	{
		if (agent == null)
			return;

		agent.speed = Mathf.Max(0f, speed);
	}
}
