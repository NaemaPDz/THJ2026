using UnityEngine;

public class RangeAttack : MonoBehaviour
{
	[Header("Attack Settings")]
	[SerializeField] private GameObject projectilePrefab;
	[SerializeField] private Transform firePoint;

	private Animator animator;
	private SacrificeEffectReceiver sacrificeEffects;

	private void Awake()
	{
		animator = GetComponentInChildren<Animator>();
		sacrificeEffects = GetComponent<SacrificeEffectReceiver>();
	}

	public bool TryAttack(WeaponInstance weapon, Vector2 aimDirection)
	{
		if (weapon == null)
			return false;

		if (weapon.WeaponData.attackType != AttackType.Projectile)
			return false;

		if (!weapon.CanAttack())
		{
			Debug.Log("[RangeAttack] อาวุธติดคูลดาวน์หรือกระสุนหมด");
			return false;
		}

		if (projectilePrefab == null || firePoint == null)
		{
			Debug.LogError("[RangeAttack] ยังไม่ได้กำหนด Projectile Prefab หรือ Fire Point");
			return false;
		}

		if (aimDirection.sqrMagnitude < 0.001f)
			return false;

		animator.SetFloat("AimY", aimDirection.y);
		animator.SetTrigger("Gun");

		if (aimDirection.x < 0)
		{
			transform.localScale = new Vector3(-1f, 1f, 1f);
		}
		else if (aimDirection.x > 0)
		{
			transform.localScale = new Vector3(1f, 1f, 1f);
		}

		GameObject projectileObject = Instantiate(
			projectilePrefab,
			firePoint.position,
			Quaternion.identity
		);

		if (!projectileObject.TryGetComponent<Projectile>(out var projectile))
		{
			Debug.LogError("[RangeAttack] Prefab ไม่มี Projectile component");
			Destroy(projectileObject);
			return false;
		}

		projectile.Initialize(
			weapon.WeaponData,
			aimDirection,
			sacrificeEffects != null ? sacrificeEffects.AttackRangeMultiplier : 1f
		);

		if (!weapon.TryStartAttack())
		{
			Destroy(projectileObject);
			return false;
		}

		if (!weapon.TryConsumeAmmo())
		{
			Destroy(projectileObject);
			return false;
		}

		Debug.Log($"[RangeAttack] ยิง {weapon.WeaponData.itemName}");
		return true;
	}
}
