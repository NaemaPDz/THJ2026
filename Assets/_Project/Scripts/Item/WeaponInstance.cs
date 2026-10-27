
using System;
using UnityEngine;

public class WeaponInstance : ItemInstance
{
	public WeaponData WeaponData { get; }

	public int CurrentAmmo { get; private set; }

	public event Action<int> OnAmmoChanged;

	private float nextAttackTime;

	public WeaponInstance(WeaponData data) : base(data)
	{
		WeaponData = data;

		CurrentAmmo = data.unlimitedAmmo
			? 0
			: data.maxAmmo;
	}

	public bool HasAmmo()
	{
		return WeaponData.unlimitedAmmo || CurrentAmmo > 0;
	}

	public bool CanAttack()
	{
		return Time.time >= nextAttackTime && HasAmmo();
	}

	public bool TryStartAttack()
	{
		if (!CanAttack())
			return false;

		nextAttackTime = Time.time + WeaponData.attackCooldown;
		return true;
	}

	public bool TryConsumeAmmo()
	{
		if (WeaponData.unlimitedAmmo)
			return true;

		if (CurrentAmmo <= 0)
			return false;

		CurrentAmmo--;
		OnAmmoChanged?.Invoke(CurrentAmmo);

		return true;
	}
}
