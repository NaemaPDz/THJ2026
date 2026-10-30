using UnityEngine;

/// <summary>
/// Runtime effect values consumed by player systems. Sacrifice choices can update
/// these values without changing the underlying weapon or movement data.
/// </summary>
public sealed class SacrificeEffectReceiver : MonoBehaviour
{
	[SerializeField, Min(0f)] private float attackRangeMultiplier = 1f;
	[SerializeField, Min(0f)] private float movementSpeedBonus;
	[SerializeField, Range(0f, 1f)] private float soundEmitterRadiusMultiplier = 1f;
	[SerializeField, Range(0f, 1f)] private float accelerationMultiplier = 1f;
	[SerializeField, Range(0f, 1f)] private float decelerationMultiplier = 1f;
	[SerializeField] private bool sprintDisabled;
	[SerializeField, Range(0f, 1f)] private float attackMissChance;
	[SerializeField, Min(0f)] private float missRecoveryMin = 1f;
	[SerializeField, Min(0f)] private float missRecoveryMax = 2f;

	public float AttackRangeMultiplier => attackRangeMultiplier;
	public float MovementSpeedBonus => movementSpeedBonus;
	public float SoundEmitterRadiusMultiplier => soundEmitterRadiusMultiplier;
	public float AccelerationMultiplier => accelerationMultiplier;
	public float DecelerationMultiplier => decelerationMultiplier;
	public bool SprintDisabled => sprintDisabled;
	public float AttackMissChance => attackMissChance;
	public float MissRecoveryMin => missRecoveryMin;
	public float MissRecoveryMax => missRecoveryMax;

	public void Apply(FaustEffectProfile profile)
	{
		if (profile == null)
			return;

		attackRangeMultiplier = Mathf.Max(0f, profile.AttackRangeMultiplier);
		movementSpeedBonus = Mathf.Max(0f, profile.MovementSpeedBonus);
		soundEmitterRadiusMultiplier = Mathf.Clamp01(profile.SoundEmitterRadiusMultiplier);
		accelerationMultiplier = Mathf.Clamp01(profile.AccelerationMultiplier);
		decelerationMultiplier = Mathf.Clamp01(profile.DecelerationMultiplier);
		sprintDisabled = profile.SprintDisabled;
		attackMissChance = Mathf.Clamp01(profile.AttackMissChance);
		missRecoveryMin = Mathf.Max(0f, profile.MissRecoveryMin);
		missRecoveryMax = Mathf.Max(missRecoveryMin, profile.MissRecoveryMax);
	}
}
