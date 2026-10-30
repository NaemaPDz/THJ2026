using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class MonsterAI : MonoBehaviour, IDamageable
{
	public enum MonsterState
	{
		Patrol,
		Investigate,
		Alert,
		Chase,
		Stagger,
		Stunned,
		Kill
	}

	[Header("Stagger System")]
	[SerializeField] private float maxStaggerHP = 100f;
	[SerializeField] private float currentStaggerHP = 100f;
	[SerializeField] private float staggerDuration = 5f;

	[Header("Target")]
	[SerializeField] private Transform target;
	[SerializeField] private float killDistance = 0.7f;
	[SerializeField, Min(0f)] private float missRecoveryMin = 1f;
	[SerializeField, Min(0f)] private float missRecoveryMax = 2f;
	[SerializeField] private PlayerController playerController;
	[SerializeField] private Rigidbody2D playerRb;

	[Header("Patrol")]
	[SerializeField] private Transform[] patrolPoints;
	[SerializeField] private float patrolSpeed = 2f;
	[SerializeField] private float waitAtPoint = 1f;

	[Header("Chase")]
	[SerializeField, Min(0f)] private float chaseSpeed = 4f;

	[Header("Search After Losing Sight")]
	[SerializeField] private float loseSightDelay = 0.5f;
	[SerializeField] private float searchDuration = 3f;

	private Vector3 lastKnownPosition;
	private float lostSightTimer;
	private float searchTimer;
	private bool reachedSearchPosition;

	[Header("Hit Visual")]
	[SerializeField] private SpriteRenderer spriteRenderer;
	[SerializeField] private float hitFlashDuration = 0.1f;

	private Color originalColor;
	private Coroutine hitFlashCoroutine;

	private NavMeshAgent agent;
	private MonsterVision vision;
	private FakeVisionEffect playerVision;
	private Animator animator;

	private MonsterState currentState = MonsterState.Patrol;
	private MonsterState previousState;

	private float normalSpeed;
	private int currentPointIndex;
	private float waitTimer;
	private bool isWaiting;
	private bool wasChasingBeforeStagger;

	private Coroutine slowCoroutine;
	private Coroutine staggerCoroutine;
	private Coroutine screamCoroutine;
	private bool isAttackRecovering;
	private SacrificeEffectReceiver targetSacrificeEffects;
	private HeartRateSystem targetHeartRate;

	private void OnEnable()
	{
		SoundEmitter.OnSoundMade += HandleSoundHeard;
	}

	private void OnDisable()
	{
		SoundEmitter.OnSoundMade -= HandleSoundHeard;
		targetHeartRate?.SetMonsterThreatState(false, false);
	}

	private void Start()
	{
		agent = GetComponent<NavMeshAgent>();
		vision = GetComponent<MonsterVision>();
		playerVision = Camera.main != null
			? Camera.main.GetComponent<FakeVisionEffect>()
			: FindFirstObjectByType<FakeVisionEffect>();

		if (vision != null)
		{
			vision.SetTarget(target);
		}
		else
		{
			Debug.LogWarning(
				"[MonsterAI] MonsterVision is missing!",
				this
			);
		}

		CacheTargetReferences();

		if (agent == null)
		{
			Debug.LogError("[MonsterAI] Missing NavMeshAgent!", this);
			enabled = false;
			return;
		}

		agent.updateRotation = false;
		agent.updateUpAxis = false;
		agent.isStopped = !FaustRunState.GameplayActive;

		normalSpeed = agent.speed;
		agent.speed = patrolSpeed;

		if (spriteRenderer == null)
			spriteRenderer = GetComponentInChildren<SpriteRenderer>();
		animator = GetComponentInChildren<Animator>();

		if (spriteRenderer != null)
			originalColor = spriteRenderer.color;
		else
			Debug.LogWarning("[MonsterAI] No SpriteRenderer found!", this);

		if (patrolPoints != null && patrolPoints.Length > 0)
		{
			currentPointIndex = 0;
			MoveToPatrolPoint();
		}
	}

	private void Update()
	{
		if (!FaustRunState.GameplayActive)
			return;

		PublishThreatState();
		UpdateAnimation();

		if (agent == null || !agent.enabled || !agent.isOnNavMesh)
			return;

		// ระหว่าง Stagger หรือ Stunned ห้าม AI สั่งเดิน
		if (currentState == MonsterState.Stagger ||
			currentState == MonsterState.Stunned ||
			currentState == MonsterState.Kill ||
			currentState == MonsterState.Alert)
		{
			return;
		}

		if (vision != null)
		{
			if (vision.CanSeeTarget)
			{
				lostSightTimer = 0f;

				if (currentState == MonsterState.Patrol ||
					currentState == MonsterState.Investigate)
				{
					StartDetectionScream();
				}

				if (currentState == MonsterState.Chase && target != null)
				{
					lastKnownPosition = target.position;
				}
			}
			else if (currentState == MonsterState.Chase)
			{
				lostSightTimer += Time.deltaTime;

				if (lostSightTimer >= loseSightDelay)
				{
					lostSightTimer = 0f;
					StartSearching();
				}
			}
		}

		if (currentState == MonsterState.Alert || CheckKillDistance())
			return;

		switch (currentState)
		{
			case MonsterState.Patrol:
				UpdatePatrol();
				break;

			case MonsterState.Investigate:
				UpdateInvestigate();
				break;

			case MonsterState.Chase:
				UpdateChase();
				break;
		}
	}

	private void StartDetectionScream()
	{
		if (screamCoroutine != null)
			return;

		AudioManager.Instance?.PlaySfxAt(SoundId.WorldAlert, transform.position);
		AudioManager.Instance?.PlayRandomSfxAt(
			transform.position,
			SoundId.MonsterAlert1,
			SoundId.MonsterAlert2,
			SoundId.MonsterAlert3
		);

		SetState(MonsterState.Alert);
		animator?.ResetTrigger("Detect");
		animator?.SetTrigger("Detect");
		screamCoroutine = StartCoroutine(ScreamThenChase());
	}

	private IEnumerator ScreamThenChase()
	{
		if (animator != null)
		{
			yield return WaitForAnimationToFinish("Base Layer.ScreamIntro");
			yield return WaitForAnimationToFinish("Base Layer.Scream");
		}

		screamCoroutine = null;
		if (currentState != MonsterState.Alert)
			yield break;

		if (target != null)
			Chase(target);
		else
			SetState(MonsterState.Investigate);
	}

	private IEnumerator WaitForAnimationToFinish(string stateName)
	{
		const float timeout = 3f;
		float elapsed = 0f;
		bool stateStarted = false;

		while (elapsed < timeout)
		{
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			if (stateInfo.IsName(stateName))
			{
				stateStarted = true;
			}
			else if (stateStarted)
			{
				yield break;
			}

			elapsed += Time.deltaTime;
			yield return null;
		}
	}

	private void UpdateAnimation()
	{
		if (agent == null)
			return;

		Vector2 velocity = agent.enabled ? agent.velocity : Vector2.zero;
		if (spriteRenderer != null && Mathf.Abs(velocity.x) > 0.05f)
			spriteRenderer.flipX = velocity.x < 0f;

		if (animator == null)
			return;

		bool locked = currentState == MonsterState.Alert ||
			currentState == MonsterState.Stagger ||
			currentState == MonsterState.Stunned ||
			currentState == MonsterState.Kill;
		animator.SetFloat("Speed", locked ? 0f : velocity.magnitude);
	}

	// =========================
	// PATROL
	// =========================

	private void UpdatePatrol()
	{
		if (patrolPoints == null || patrolPoints.Length == 0)
			return;

		if (!agent.pathPending && HasArrived())
		{
			if (!isWaiting)
			{
				isWaiting = true;
				waitTimer = waitAtPoint;
				agent.isStopped = true;
			}

			waitTimer -= Time.deltaTime;

			if (waitTimer <= 0f)
			{
				isWaiting = false;

				currentPointIndex =
					(currentPointIndex + 1) % patrolPoints.Length;

				MoveToPatrolPoint();
			}
		}
	}

	private void MoveToPatrolPoint()
	{
		if (patrolPoints == null ||
			patrolPoints.Length == 0 ||
			patrolPoints[currentPointIndex] == null)
		{
			return;
		}

		agent.speed = patrolSpeed;
		agent.isStopped = !FaustRunState.GameplayActive;

		agent.SetDestination(
			patrolPoints[currentPointIndex].position
		);
	}

	private void ReturnToNearestPatrolPoint()
	{
		if (patrolPoints == null || patrolPoints.Length == 0)
		{
			SetState(MonsterState.Patrol);
			return;
		}

		int nearestIndex = -1;
		float nearestDistance = float.MaxValue;

		for (int i = 0; i < patrolPoints.Length; i++)
		{
			if (patrolPoints[i] == null)
				continue;

			float distance = Vector3.Distance(
				transform.position,
				patrolPoints[i].position
			);

			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearestIndex = i;
			}
		}

		if (nearestIndex >= 0)
		{
			currentPointIndex = nearestIndex;
			isWaiting = false;

			SetState(MonsterState.Patrol);
		}
	}

	// =========================
	// INVESTIGATE
	// =========================

	public void Investigate(Vector3 position)
	{
		if (IsLockedState())
			return;

		SetState(MonsterState.Investigate);

		agent.isStopped = false;
		agent.SetDestination(position);
	}

	private void UpdateInvestigate()
	{
		if (agent.pathPending)
			return;

		if (!reachedSearchPosition)
		{
			if (!HasArrived())
				return;

			reachedSearchPosition = true;
			searchTimer = searchDuration;

			agent.isStopped = true;
			agent.ResetPath();
			return;
		}

		searchTimer -= Time.deltaTime;

		if (searchTimer <= 0f)
		{
			ReturnToNearestPatrolPoint();
		}
	}

	private void HandleSoundHeard(Vector3 soundPosition, float hearingRadius)
	{
		if (agent == null || !agent.enabled) return;

		// ไม่รับเสียงขณะอยู่ในสถานะที่ไม่ควรถูกรบกวน
		if (currentState == MonsterState.Chase ||
			currentState == MonsterState.Alert ||
			currentState == MonsterState.Stagger ||
			currentState == MonsterState.Stunned ||
			currentState == MonsterState.Kill)
		{
			return;
		}

		float distance = Vector2.Distance(
			transform.position,
			soundPosition
		);

		if (distance > hearingRadius) return;

		lastKnownPosition = soundPosition;
		StartSearching();
	}

	// =========================
	// CHASE
	// =========================

	public void Chase(Transform newTarget)
	{
		if (newTarget == null)
			return;

		lostSightTimer = 0f;
		reachedSearchPosition = false;

		if (newTarget != null)
		{
			lastKnownPosition = newTarget.position;
		}

		// ไม่ให้ขัดจังหวะสถานะพิเศษ
		if (currentState == MonsterState.Stagger ||
			currentState == MonsterState.Stunned ||
			currentState == MonsterState.Kill)
			return;

		target = newTarget;
		CacheTargetReferences();

		if (vision != null)
		{
			vision.SetTarget(target);
		}

		SetState(MonsterState.Chase);

		agent.isStopped = false;
	}

	private void CacheTargetReferences()
	{
		targetSacrificeEffects = target != null
			? target.GetComponent<SacrificeEffectReceiver>()
			: null;
		targetHeartRate = target != null
			? target.GetComponent<HeartRateSystem>()
			: null;

		if (target != null)
		{
			if (playerController == null)
				playerController = target.GetComponent<PlayerController>();
			if (playerRb == null)
				playerRb = target.GetComponent<Rigidbody2D>();
		}
	}

	private void PublishThreatState()
	{
		if (targetHeartRate == null)
			return;

		targetHeartRate.SetMonsterThreatState(
			playerVision != null && playerVision.CanSeePosition(transform.position),
			currentState == MonsterState.Chase
		);
	}

	private void UpdateChase()
	{
		if (target == null)
		{
			SetState(MonsterState.Patrol);
			return;
		}

		// อัปเดตตำแหน่งผู้เล่นระหว่างไล่ตาม
		agent.SetDestination(target.position);
	}


	private bool CheckKillDistance()
	{
		if (isAttackRecovering || currentState == MonsterState.Kill)
			return true;
		if (currentState != MonsterState.Chase)
			return false;

		if (vision == null || vision.Target == null)
			return false;

		float distance = Vector2.Distance(
			transform.position,
			vision.Target.position
		);

		if (distance > killDistance)
			return false;

		PlayAttackAnimation();
		AudioManager.Instance?.PlayRandomSfxAt(
			transform.position,
			SoundId.MonsterAttack1,
			SoundId.MonsterAttack2,
			SoundId.MonsterAttack3,
			SoundId.MonsterAttack4
		);

		float missChance = targetSacrificeEffects != null
			? targetSacrificeEffects.AttackMissChance
			: 0f;
		if (Random.value < missChance)
			StartCoroutine(MissedAttackRecovery());
		else
			KillPlayer();

		return true;
	}

	private void PlayAttackAnimation()
	{
		string attackTrigger = GetAttackTrigger();
		animator?.ResetTrigger(attackTrigger);
		animator?.SetTrigger(attackTrigger);
	}

	private string GetAttackTrigger()
	{
		Vector2 playerFromMonster = (Vector2)(target.position - transform.position);
		if (Mathf.Abs(playerFromMonster.y) <= 0.7f)
		{
			if (spriteRenderer != null && Mathf.Abs(playerFromMonster.x) > 0.01f)
				spriteRenderer.flipX = playerFromMonster.x < 0f;
			return "AttackSide";
		}

		return playerFromMonster.y > 0f ? "AttackUp" : "AttackDown";
	}

	private IEnumerator MissedAttackRecovery()
	{
		isAttackRecovering = true;
		if (playerController != null)
			playerController.PlayHurt();

		if (agent != null && agent.enabled && agent.isOnNavMesh)
		{
			agent.isStopped = true;
			agent.ResetPath();
		}

		float recoveryMin = targetSacrificeEffects != null
			? targetSacrificeEffects.MissRecoveryMin
			: missRecoveryMin;
		float recoveryMax = targetSacrificeEffects != null
			? targetSacrificeEffects.MissRecoveryMax
			: missRecoveryMax;
		float min = Mathf.Min(recoveryMin, recoveryMax);
		float max = Mathf.Max(recoveryMin, recoveryMax);
		yield return new WaitForSeconds(Random.Range(min, max));

		isAttackRecovering = false;
		if (currentState == MonsterState.Chase && agent != null && agent.enabled && agent.isOnNavMesh)
			agent.isStopped = false;
	}

	public void ResumeAfterFaustSelection()
	{
		if (agent != null && agent.enabled && agent.isOnNavMesh &&
			currentState != MonsterState.Stagger &&
			currentState != MonsterState.Stunned &&
			currentState != MonsterState.Kill &&
			currentState != MonsterState.Alert)
		{
			agent.isStopped = false;
		}
	}

	private void KillPlayer()
	{
		SetState(MonsterState.Kill);

		// หยุดมอนสเตอร์
		if (agent != null && agent.enabled)
		{
			agent.isStopped = true;
			agent.ResetPath();
		}

		// หยุดการควบคุมผู้เล่น
		if (playerController != null)
			playerController.Death();

		// หยุดฟิสิกส์ของผู้เล่น
		if (playerRb != null)
		{
			playerRb.linearVelocity = Vector2.zero;
			playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
		}

		Debug.Log("Player killed by monster!");
	}


	private void StartSearching()
	{
		if (currentState == MonsterState.Stagger ||
			currentState == MonsterState.Stunned ||
			currentState == MonsterState.Kill ||
			currentState == MonsterState.Alert)
			return;

		SetState(MonsterState.Investigate);

		reachedSearchPosition = false;
		searchTimer = searchDuration;

		agent.isStopped = false;
		agent.SetDestination(lastKnownPosition);
	}

	// =========================
	// DAMAGE
	// =========================

	public void TakeHit(
		float staggerDamage,
		float slowMultiplier,
		float slowDuration)
	{
		if (currentState == MonsterState.Kill ||
			currentState == MonsterState.Stagger ||
			currentState == MonsterState.Stunned)
		{
			return;
		}

		FlashRed();
		AudioManager.Instance?.PlayRandomSfxAt(
			transform.position,
			SoundId.MonsterHurt1,
			SoundId.MonsterHurt2,
			SoundId.MonsterHurt3,
			SoundId.MonsterHurt4
		);

		currentStaggerHP -= staggerDamage;
		currentStaggerHP = Mathf.Max(0f, currentStaggerHP);

		Debug.Log(
			$"[MonsterAI] Stagger HP: {currentStaggerHP}/{maxStaggerHP}",
			this
		);

		if (currentStaggerHP <= 0f)
		{
			ApplyStagger();
			return;
		}

		if (slowDuration > 0f)
		{
			ApplySlow(slowMultiplier, slowDuration);
		}
	}

	private void ApplyStagger()
	{
		if (staggerCoroutine != null)
			StopCoroutine(staggerCoroutine);
		if (screamCoroutine != null)
		{
			StopCoroutine(screamCoroutine);
			screamCoroutine = null;
		}
		wasChasingBeforeStagger =
			currentState == MonsterState.Chase;

		previousState = currentState == MonsterState.Alert
			? MonsterState.Investigate
			: currentState;

		isAttackRecovering = false;
		SetState(MonsterState.Stagger);

		SetStaggerColor(true);

		staggerCoroutine = StartCoroutine(StaggerRoutine());
	}

	private IEnumerator StaggerRoutine()
	{
		agent.isStopped = true;
		agent.ResetPath();

		yield return new WaitForSeconds(staggerDuration);


		currentStaggerHP = maxStaggerHP;
		SetStaggerColor(false);

		staggerCoroutine = null;
		SetState(previousState);

		if (wasChasingBeforeStagger)
		{
			StartSearching();
			yield break;
		}

		switch (previousState)
		{
			case MonsterState.Patrol:
				SetState(MonsterState.Patrol);
				break;

			case MonsterState.Investigate:
				StartSearching();
				break;

			default:
				SetState(MonsterState.Patrol);
				break;
		}
	}

	private void ApplySlow(float multiplier, float duration)
	{
		if (slowCoroutine != null)
			StopCoroutine(slowCoroutine);

		slowCoroutine = StartCoroutine(
			SlowRoutine(multiplier, duration)
		);
	}

	private IEnumerator SlowRoutine(float multiplier, float duration)
	{
		agent.speed = GetStateMovementSpeed() * Mathf.Clamp01(multiplier);

		yield return new WaitForSeconds(duration);

		agent.speed = GetStateMovementSpeed();

		slowCoroutine = null;
	}

	private float GetStateMovementSpeed()
	{
		return currentState switch
		{
			MonsterState.Patrol => patrolSpeed,
			MonsterState.Chase => chaseSpeed,
			_ => normalSpeed
		};
	}

	private void FlashRed()
	{
		if (spriteRenderer == null)
			return;

		// ถ้ากำลัง Stagger ให้สีดำมีความสำคัญกว่า
		if (currentState == MonsterState.Stagger)
			return;

		if (hitFlashCoroutine != null)
			StopCoroutine(hitFlashCoroutine);

		hitFlashCoroutine = StartCoroutine(HitFlashRoutine());
	}

	private IEnumerator HitFlashRoutine()
	{
		spriteRenderer.color = new Color(
			1f, 0f, 0f, originalColor.a
		);

		yield return new WaitForSeconds(hitFlashDuration);

		// คืนสีเดิม เฉพาะเมื่อไม่ได้กำลัง Stagger
		if (currentState != MonsterState.Stagger)
			spriteRenderer.color = originalColor;

		hitFlashCoroutine = null;
	}

	private void SetStaggerColor(bool active)
	{
		if (spriteRenderer == null)
			return;

		if (active)
		{
			// ยกเลิกแฟลชแดงที่อาจกำลังทำงาน
			if (hitFlashCoroutine != null)
			{
				StopCoroutine(hitFlashCoroutine);
				hitFlashCoroutine = null;
			}

			spriteRenderer.color = new Color(
				0f, 0f, 0f, originalColor.a
			);
		}
		else
		{
			spriteRenderer.color = originalColor;
		}
	}

	// =========================
	// STATE / HELPERS
	// =========================

	public void SetState(MonsterState newState)
	{
		if (staggerCoroutine != null &&
			newState != MonsterState.Stagger)
		{
			StopCoroutine(staggerCoroutine);
			staggerCoroutine = null;
		}

		currentState = newState;
		if (agent != null)
			agent.speed = GetStateMovementSpeed();

		if (animator != null)
			animator.SetBool("Chase", newState == MonsterState.Chase);

		if (agent == null || !agent.enabled || !agent.isOnNavMesh)
			return;

		switch (newState)
		{
			case MonsterState.Patrol:
				isWaiting = false;
				agent.isStopped = false;
				MoveToPatrolPoint();
				break;

			case MonsterState.Investigate:
			case MonsterState.Chase:
				agent.isStopped = false;
				break;

			case MonsterState.Alert:
			case MonsterState.Stagger:
			case MonsterState.Stunned:
			case MonsterState.Kill:
				agent.isStopped = true;
				agent.ResetPath();
				break;
		}
	}

	private bool HasArrived()
	{
		return !agent.pathPending &&
			   agent.remainingDistance <=
			   agent.stoppingDistance + 0.05f;
	}

	private bool IsLockedState()
	{
		return currentState == MonsterState.Stagger ||
			   currentState == MonsterState.Stunned ||
			   currentState == MonsterState.Kill ||
			   currentState == MonsterState.Alert;
	}
}
