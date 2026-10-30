using System;
using System.Collections.Generic;
using UnityEngine;

public enum FaustId
{
	Touch,
	Eyes,
	Heart,
	Ears,
	Voice
}

public enum FaustStagePlacement
{
	Stage1,
	Stage2,
	Stage3,
	RandomStage1Or2
}

[Serializable]
public sealed class FaustDefinition
{
	public FaustId id;
	public FaustStagePlacement placement;
	public string displayName;
	[TextArea] public string goodEffect;
	[TextArea] public string badEffect;
	public Sprite icon;

	[Header("Touch")]
	[Range(0f, 1f)] public float accelerationMultiplier = 0.5f;
	[Range(0f, 1f)] public float decelerationMultiplier = 0.5f;
	[Min(0f)] public float touchSpeedBonus = 1.5f;

	[Header("Eyes / Ears")]
	[Min(1f)] public float attackRangeMultiplier = 1.25f;
	public float visionAngleOffset = -15f;
	public float sfxVolumeOffsetDb = -6f;
	[Min(0.1f)] public float eyesNearVisionRadius = 0.8f;
	[Range(0f, 1f)] public float eyesConeVisibilityMultiplier;
	[Range(0f, 1f)] public float eyesHalfStrengthConeLengthMultiplier = 0.5f;

	[Header("Heart")]
	[Range(0f, 1f)] public float attackMissChance = 0.15f;
	[Min(0f)] public float missRecoveryMin = 1f;
	[Min(0f)] public float missRecoveryMax = 2f;
	[Min(1f)] public float halfStrengthSprintMaxBpm = 130f;

	[Header("Voice")]
	public bool hideHudAtFullStrength = true;
	[Range(0f, 1f)] public float soundEmitterRadiusMultiplier = 0.5f;
}

[CreateAssetMenu(
	fileName = "FaustCatalog",
	menuName = "Thai Horror Jam/Faust/Catalog"
)]
public sealed class FaustCatalog : ScriptableObject
{
	[SerializeField] private List<FaustDefinition> fausts = new();

	public IReadOnlyList<FaustDefinition> Fausts => fausts;

	public FaustDefinition Get(FaustId id)
	{
		return fausts.Find(faust => faust.id == id);
	}
}

public sealed class FaustEffectProfile
{
	public float AccelerationMultiplier = 1f;
	public float DecelerationMultiplier = 1f;
	public float MovementSpeedBonus;
	public float AttackRangeMultiplier = 1f;
	public float VisionAngleOffset;
	public float EyesNearVisionRadius = 0.8f;
	public float EyesSacrificeStrength;
	public float EyesConeLengthMultiplier = 1f;
	public float EyesConeVisibilityMultiplier = 1f;
	public float SfxVolumeOffsetDb;
	public float SoundEmitterRadiusMultiplier = 1f;
	public float AttackMissChance;
	public float MissRecoveryMin = 1f;
	public float MissRecoveryMax = 2f;
	public bool SprintDisabled;
	public float SprintMaxBpmOverride;
	public bool HideHud;
	public bool HideHeartbeatHud;
	public bool HeartSacrificed;
}
