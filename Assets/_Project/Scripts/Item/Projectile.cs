using UnityEngine;

public class Projectile : MonoBehaviour
{
	[SerializeField] private LayerMask obstaclesLayer;

	private WeaponData weaponData;
	private Vector2 moveDirection;
	private float projectileSpeed;
	private float maxTravelDistance;
	private Vector3 startPosition;
	private bool hasHit;

	public void Initialize(
		WeaponData data,
		Vector2 direction,
		float rangeMultiplier = 1f
	)
	{
		weaponData = data;
		moveDirection = direction.normalized;
		projectileSpeed = data.projectileSpeed;
		maxTravelDistance = Mathf.Max(0f, data.range * rangeMultiplier);
		startPosition = transform.position;

		// หัน Sprite ไปตามทิศทางที่ยิง
		if (moveDirection.sqrMagnitude > 0f)
		{
			float angle = Mathf.Atan2(
				moveDirection.y,
				moveDirection.x
			) * Mathf.Rad2Deg;

			transform.rotation = Quaternion.Euler(
				0f, 0f, angle
			);
		}

	}

	private void Update()
	{
		if (!FaustRunState.GameplayActive)
			return;

		transform.position += (Vector3)(projectileSpeed * Time.deltaTime * moveDirection);
		if (Vector2.Distance(startPosition, transform.position) >= maxTravelDistance)
			Destroy(gameObject);
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		Debug.Log($"Projectile hit: {other.name}");

		if ((obstaclesLayer.value & (1 << other.gameObject.layer)) != 0)
		{
			Destroy(gameObject);
			return;
		}

		if (hasHit || weaponData == null)
			return;

		if (!other.TryGetComponent(out IDamageable target))
			return;

		hasHit = true;

		target.TakeHit(
			weaponData.staggerDamage,
			weaponData.slowMultiplier,
			weaponData.slowDuration
		);

		Destroy(gameObject);
	}
}
