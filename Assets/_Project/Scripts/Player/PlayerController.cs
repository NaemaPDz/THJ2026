using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	public float walkSpeed = 3f;
	public float sprintSpeed = 5f;
	[SerializeField, Min(0f)] private float walkAcceleration = 16f;
	[SerializeField, Min(0f)] private float sprintAcceleration = 20f;
	[SerializeField, Min(0f)] private float walkDeceleration = 24f;
	[SerializeField, Min(0f)] private float sprintDeceleration = 28f;
	[SerializeField, Min(0f)] private float walkFootstepHearingRadius = 3f;
	[SerializeField, Min(0f)] private float sprintFootstepHearingRadius = 5f;

	private const string Stage1SceneName = "Stage1";
	private const string DeathCutsceneRootName = "MC_DEAD_SCENE";
	private const string DeathCutsceneStateName = "CUT_MC_Dead_UI";
	private const string DeathStateName = "Death";
	private const float DeathAnimationWaitTimeout = 2f;
	private const float DeathSequenceHoldDuration = 1f;
	private const float CutsceneStateStartTimeout = 1f;

	private Rigidbody2D rb;
	private HeartRateSystem heartRate;
	private Animator animator;
	private SacrificeEffectReceiver sacrificeEffects;

	private Vector2 moveInput;
	private Vector2 currentVelocity;
	private bool sprintHeld;
	private bool isDead;
	private float footstepTimer;
	private static readonly SoundId[] FootstepSounds =
	{
		SoundId.PlayerFootstep1,
		SoundId.PlayerFootstep2,
		SoundId.PlayerFootstep3,
		SoundId.PlayerFootstep4
	};

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		heartRate = GetComponent<HeartRateSystem>();
		animator = GetComponentInChildren<Animator>();
		sacrificeEffects = GetComponent<SacrificeEffectReceiver>();

		rb.gravityScale = 0f;
		rb.freezeRotation = true;
	}

	private void Update()
	{
		if (isDead || !FaustRunState.GameplayActive)
		{
			moveInput = Vector2.zero;
			sprintHeld = false;
			return;
		}

		Vector2 moveDir = InputSystem.actions["Move"].ReadValue<Vector2>();
		moveInput = moveDir.normalized;

		sprintHeld = InputSystem.actions["Sprint"].IsPressed() &&
			(sacrificeEffects == null || !sacrificeEffects.SprintDisabled);
	}

	private void FixedUpdate()
	{
		if (isDead || !FaustRunState.GameplayActive)
			return;

		bool wantsSprint =
		   sprintHeld && moveInput.sqrMagnitude > 0f;

		bool isSprinting = heartRate.Tick(
			wantsSprint,
			Time.fixedDeltaTime
		);

		float speed = isSprinting ? sprintSpeed : walkSpeed;
		if (sacrificeEffects != null)
			speed += sacrificeEffects.MovementSpeedBonus;
		float acceleration = isSprinting ? sprintAcceleration : walkAcceleration;
		float deceleration = isSprinting ? sprintDeceleration : walkDeceleration;
		if (sacrificeEffects != null)
		{
			acceleration *= sacrificeEffects.AccelerationMultiplier;
			deceleration *= sacrificeEffects.DecelerationMultiplier;
		}

		Vector2 targetVelocity = moveInput * speed;
		float responseRate = moveInput.sqrMagnitude > 0f ? acceleration : deceleration;
		currentVelocity = Vector2.MoveTowards(
			currentVelocity,
			targetVelocity,
			responseRate * Time.fixedDeltaTime
		);

		rb.MovePosition(
			rb.position + currentVelocity * Time.fixedDeltaTime
		);

		bool isMoving = currentVelocity.sqrMagnitude > 0.01f;
		if (isMoving)
		{
			footstepTimer -= Time.fixedDeltaTime;
			if (footstepTimer <= 0f)
			{
				AudioManager.Instance?.PlayRandomSfxAt(
					transform.position,
					FootstepSounds
				);
				float footstepHearingRadius = isSprinting
					? sprintFootstepHearingRadius
					: walkFootstepHearingRadius;
				SoundEmitter.EmitSound(
					transform.position,
					footstepHearingRadius * GetSoundEmitterRadiusMultiplier()
				);
				footstepTimer = isSprinting ? 0.3f : 0.45f;
			}
		}
		else
		{
			footstepTimer = 0f;
		}

		animator.SetFloat("Speed", currentVelocity.magnitude);
		animator.SetBool("Sprint", isSprinting);

		if (isMoving)
		{
			if (moveInput.x < 0f)
			{
				transform.localScale = new Vector3(-1f, 1f, 1f);
			}
			else if (moveInput.x > 0f)
			{
				transform.localScale = new Vector3(1f, 1f, 1f);
			}
		}
	}

	private float GetSoundEmitterRadiusMultiplier()
	{
		return sacrificeEffects != null
			? sacrificeEffects.SoundEmitterRadiusMultiplier
			: 1f;
	}

	public void PlayHurt()
	{
		if (isDead)
			return;

		animator?.SetTrigger("Hurt");
		AudioManager.Instance?.PlayRandomSfxAt(
			transform.position,
			SoundId.PlayerHurt1,
			SoundId.PlayerHurt2
		);
		SoundEmitter.EmitSound(transform.position, 7f * GetSoundEmitterRadiusMultiplier());
	}

	public void Death()
	{
		if (isDead)
			return;

		isDead = true;
		AudioManager.Instance?.PlaySfxAt(SoundId.PlayerDeath, transform.position);

		if (rb != null)
		{
			rb.linearVelocity = Vector2.zero;
			rb.constraints = RigidbodyConstraints2D.FreezeAll;
		}

		if (animator != null)
		{
			animator.updateMode = AnimatorUpdateMode.UnscaledTime;
			animator.SetTrigger("Death");
		}

		foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
		{
			if (script != this)
				script.enabled = false;
		}

		StartCoroutine(PlayDeathCutsceneThenReturnToStage1());
	}

	private IEnumerator PlayDeathCutsceneThenReturnToStage1()
	{
		yield return WaitForDeathAnimation();
		yield return new WaitForSecondsRealtime(DeathSequenceHoldDuration);

		GameObject cutsceneRoot = FindDeathCutsceneRoot();
		if (cutsceneRoot != null)
		{
			cutsceneRoot.SetActive(true);
			Animator cutsceneAnimator = cutsceneRoot.GetComponentInChildren<Animator>(true);

			if (cutsceneAnimator != null)
			{
				cutsceneAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
				cutsceneAnimator.Play(DeathCutsceneStateName, 0, 0f);
				float startElapsed = 0f;
				while (!cutsceneAnimator.GetCurrentAnimatorStateInfo(0).IsName(DeathCutsceneStateName) &&
					startElapsed < CutsceneStateStartTimeout)
				{
					startElapsed += Time.unscaledDeltaTime;
					yield return null;
				}

				if (cutsceneAnimator.GetCurrentAnimatorStateInfo(0).IsName(DeathCutsceneStateName))
				{
					AnimatorStateInfo cutsceneState = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
					float elapsed = 0f;
					float timeout = cutsceneState.length + 1f;
					while (cutsceneAnimator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f && elapsed < timeout)
					{
						elapsed += Time.unscaledDeltaTime;
						yield return null;
					}
					yield return new WaitForSecondsRealtime(DeathSequenceHoldDuration);
				}
				else
				{
					Debug.LogWarning($"Could not play Animator state {DeathCutsceneStateName}. Returning to {Stage1SceneName}.", cutsceneAnimator);
				}
			}
			else
			{
				Debug.LogWarning($"{DeathCutsceneRootName} has no Animator. Returning to {Stage1SceneName} without playing the cutscene.", cutsceneRoot);
			}
		}
		else
		{
			Debug.LogWarning($"Could not find {DeathCutsceneRootName} in the active scene. Returning to {Stage1SceneName} without playing the cutscene.", this);
		}

		Time.timeScale = 1f;
		FaustRunState.BeginNewRun();
		SceneManager.LoadScene(Stage1SceneName);
	}

	private IEnumerator WaitForDeathAnimation()
	{
		if (animator == null)
			yield break;

		// Play the short death clip directly so an exit-time transition from a looping
		// movement/weapon animation cannot delay it past the fallback timeout.
		animator.Play(DeathStateName, 0, 0f);
		float elapsed = 0f;
		while (elapsed < DeathAnimationWaitTimeout)
		{
			AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
			if (stateInfo.IsName(DeathStateName) && stateInfo.normalizedTime >= 1f)
				yield break;

			elapsed += Time.unscaledDeltaTime;
			yield return null;
		}
	}

	private static GameObject FindDeathCutsceneRoot()
	{
		GameObject[] sceneRoots = SceneManager.GetActiveScene().GetRootGameObjects();
		foreach (GameObject sceneRoot in sceneRoots)
		{
			foreach (Transform child in sceneRoot.GetComponentsInChildren<Transform>(true))
			{
				if (child.name == DeathCutsceneRootName)
					return child.gameObject;
			}
		}

		return null;
	}
}
