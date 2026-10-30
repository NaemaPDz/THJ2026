
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
	public Transform target;
	public float smoothSpeed = 5f;
	public Vector3 offset = new Vector3(0f, 0f, -10f);

	private void LateUpdate()
	{
		if (target == null)
			return;

		Vector3 targetPosition = target.position + offset;

		float smooth = 1f - Mathf.Exp(
			-smoothSpeed * Time.deltaTime
		);

		transform.position = Vector3.Lerp(
			transform.position,
			targetPosition,
			smooth
		);
	}
}
