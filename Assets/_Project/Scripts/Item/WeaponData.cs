using UnityEngine;

public enum AttackType
{
	Melee,
	Projectile
}

[CreateAssetMenu(
	fileName = "NewWeaponData",
	menuName = "Thai Horror Jam/Items/Weapon Data"
)]
public class WeaponData : ItemData
{
	[Header("Attack")]
	public AttackType attackType;
	public float staggerDamage = 17f;
	public float attackCooldown = 0.5f;
	public float range = 1.5f;

	[Header("Projectile")]
	public float projectileSpeed = 0f;

	[Header("Ammo")]
	public int maxAmmo = 0;
	public bool unlimitedAmmo = true;

	[Header("On Hit - Slow")]
	[Range(0f, 1f)]
	public float slowMultiplier = 0.5f;

	public float slowDuration = 2f;
}
