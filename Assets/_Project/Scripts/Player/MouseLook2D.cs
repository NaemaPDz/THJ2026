
using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook2D : MonoBehaviour
{
	public Camera mainCamera;

	private void Awake()
	{
		if (mainCamera == null)
			mainCamera = Camera.main;
	}

	private void Update()
	{
		if (!FaustRunState.GameplayActive || Mouse.current == null || mainCamera == null)
			return;

		Vector2 mousePosition =
			Mouse.current.position.ReadValue();

		Ray ray = mainCamera.ScreenPointToRay(mousePosition);
		Plane plane = new Plane(Vector3.forward, transform.position);

		if (!plane.Raycast(ray, out float distance))
			return;

		Vector3 mouseWorldPosition = ray.GetPoint(distance);

		Vector2 direction =
			(Vector2)(mouseWorldPosition - transform.position);

		if (direction.sqrMagnitude < 0.001f)
			return;

		float angle = Mathf.Atan2(
			direction.y,
			direction.x
		) * Mathf.Rad2Deg;

		// สมมติว่า Sprite หันหน้าไปทางขวา (+X)
		transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
	}
}
