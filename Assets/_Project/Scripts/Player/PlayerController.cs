using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
	[Header("Movement")]
	public float walkSpeed = 3f;
	public float sprintSpeed = 5f;

	private Rigidbody2D rb;
	private HeartRateSystem heartRate;
	private Animator animator;

	private Vector2 moveInput;
	private bool sprintHeld;

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
		Vector2 moveDir = InputSystem.actions["Move"].ReadValue<Vector2>();
		moveInput = moveDir.normalized;

		sprintHeld = InputSystem.actions["Sprint"].IsPressed();
	}

	private void FixedUpdate()
	{
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
		animator.SetBool("Death", true);

		foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
		{
			if (script != this)
				script.enabled = false;
		}

		this.enabled = false;
	}
}
