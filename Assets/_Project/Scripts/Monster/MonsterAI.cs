
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterAI : MonoBehaviour, IDamageable
{
	[SerializeField] private Transform target;

	private NavMeshAgent agent;

	private float normalSpeed;
	private Coroutine slowCoroutine;

	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();

		agent.updateRotation = false;
		agent.updateUpAxis = false;

		normalSpeed = agent.speed;

		Debug.Log($"[MonsterAI] Normal Speed: {normalSpeed}", this);
	}

	private void Update()
	{
		if (target != null)
		{
			agent.SetDestination(target.position);
		}
	}

	public void TakeHit(
		float staggerDamage,
		float slowMultiplier,
		float slowDuration)
	{
		Debug.Log(
			$"[MonsterAI] ถูกโจมตี! Stagger: {staggerDamage}",
			this
		);

		ApplySlow(slowMultiplier, slowDuration);
	}

	private void ApplySlow(float multiplier, float duration)
	{
		if (agent == null)
			return;

		if (slowCoroutine != null)
			StopCoroutine(slowCoroutine);

		slowCoroutine = StartCoroutine(
			SlowRoutine(multiplier, duration)
		);
	}

	private IEnumerator SlowRoutine(float multiplier, float duration)
	{
		agent.speed = normalSpeed * Mathf.Clamp01(multiplier);

		yield return new WaitForSeconds(duration);

		agent.speed = normalSpeed;
		slowCoroutine = null;
	}
}
