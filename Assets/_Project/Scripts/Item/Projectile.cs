using UnityEngine;

public class Projectile : MonoBehaviour
{
	[SerializeField] private LayerMask obstaclesLayer;

	private WeaponData weaponData;
	private Vector2 moveDirection;
	private float projectileSpeed;
	private float maxLifetime = 5f;
	private bool hasHit;

	public void Initialize(
		WeaponData data,
		Vector2 direction
	)
	{
		weaponData = data;
		moveDirection = direction.normalized;
		projectileSpeed = data.projectileSpeed;

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

		Destroy(gameObject, maxLifetime);
	}

	private void Update()
	{
		transform.position += (Vector3)(projectileSpeed * Time.deltaTime * moveDirection);
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
