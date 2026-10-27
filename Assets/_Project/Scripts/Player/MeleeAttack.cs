
using UnityEngine;

public class MeleeAttack : MonoBehaviour
{
	[Header("Attack Settings")]
	[SerializeField] private LayerMask targetLayers;

	private Animator animator;

	private void Awake()
	{
		animator = GetComponentInChildren<Animator>();
	}

	public bool TryAttack(WeaponInstance weapon, Vector2 aimDirection)
	{
		if (weapon == null)
			return false;

		if (weapon.WeaponData.attackType != AttackType.Melee)
			return false;

		if (!weapon.CanAttack())
		{
			Debug.Log("[MeleeAttack] อาวุธติดคูลดาวน์หรือกระสุนหมด");
			return false;
		}

		animator.SetFloat("AimY", aimDirection.y);
		animator.SetTrigger("Melee");

		if (aimDirection.x < 0)
		{
			transform.localScale = new Vector3(-1f, 1f, 1f);
		}
		else if (aimDirection.x > 0)
		{
			transform.localScale = new Vector3(1f, 1f, 1f);
		}

		// ตรวจจับ Collider รอบตัวผู้เล่นตามระยะอาวุธ
		Vector2 origin = transform.position;

		Collider2D[] hits = Physics2D.OverlapCircleAll(
			origin,
			weapon.WeaponData.range,
			targetLayers
		);

		IDamageable target = null;

		foreach (Collider2D hit in hits)
		{
			target = hit.GetComponentInParent<IDamageable>();

			if (target != null)
				break;
		}

		if (target == null)
		{
			Debug.Log("[MeleeAttack] ไม่พบเป้าหมายในระยะ");
			return false;
		}

		// เริ่มคูลดาวน์และใช้กระสุนเมื่อพบเป้าหมายเท่านั้น
		if (!weapon.TryStartAttack())
			return false;

		if (!weapon.TryConsumeAmmo())
			return false;

		target.TakeHit(
			weapon.WeaponData.staggerDamage,
			weapon.WeaponData.slowMultiplier,
			weapon.WeaponData.slowDuration
		);

		Debug.Log(
			$"[MeleeAttack] โจมตีโดนเป้าหมาย ระยะ {weapon.WeaponData.range}"
		);

		return true;
	}

}
