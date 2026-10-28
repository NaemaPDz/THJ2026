using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	public float walkSpeed = 3f;
	public float sprintSpeed = 5f;

	private const string Stage1SceneName = "Stage1";
	private const string DeathCutsceneRootName = "MC_DEAD_SCENE";
	private const string DeathCutsceneStateName = "CUT_MC_Dead_UI";
	private const string DeathStateName = "Death";
	private const float DeathAnimationWaitTimeout = 2f;
	private const float DeathSequenceHoldDuration = 1f;

	private Rigidbody2D rb;
	private HeartRateSystem heartRate;
	private Animator animator;

	private Vector2 moveInput;
	private bool sprintHeld;
	private bool isDead;

	private void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
		heartRate = GetComponent<HeartRateSystem>();
		animator = GetComponentInChildren<Animator>();

		rb.gravityScale = 0f;
		rb.freezeRotation = true;
	}

	private void Update()
	{
		if (isDead)
			return;

		Vector2 moveDir = InputSystem.actions["Move"].ReadValue<Vector2>();
		moveInput = moveDir.normalized;

		sprintHeld = InputSystem.actions["Sprint"].IsPressed();
	}

	private void FixedUpdate()
	{
		if (isDead)
			return;

		bool wantsSprint =
		   sprintHeld && moveInput.sqrMagnitude > 0f;

		bool isSprinting = heartRate.Tick(
			wantsSprint,
			Time.fixedDeltaTime
		);

		float speed = isSprinting ? sprintSpeed : walkSpeed;

		rb.MovePosition(
			rb.position + moveInput * speed * Time.fixedDeltaTime
		);

		animator.SetFloat("Speed", moveInput.sqrMagnitude * speed);
		animator.SetBool("Sprint", isSprinting);

		if (moveInput.sqrMagnitude > 0f)
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

	public void Death()
	{
		if (isDead)
			return;

		isDead = true;

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
			Animator cutsceneAnimator = cutsceneRoot.GetComponentInChildren<Animator>(true);
			cutsceneRoot.SetActive(true);

			if (cutsceneAnimator != null)
			{
				cutsceneAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
				cutsceneAnimator.Play(DeathCutsceneStateName, 0, 0f);
				cutsceneAnimator.Update(0f);

				AnimatorStateInfo cutsceneState = cutsceneAnimator.GetCurrentAnimatorStateInfo(0);
				if (cutsceneState.IsName(DeathCutsceneStateName))
				{
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
		SceneManager.LoadScene(Stage1SceneName);
	}

	private IEnumerator WaitForDeathAnimation()
	{
		if (animator == null)
			yield break;

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
