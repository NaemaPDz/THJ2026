using System.Collections.Generic;
using UnityEngine;

public static class FaustRunState
{
	private sealed class AppliedFaust
	{
		public FaustId Id;
		public float Strength;
	}

	private static readonly List<AppliedFaust> Applied = new();
	private static readonly HashSet<int> ResolvedStages = new();
	private static bool initialized;
	private static int eyesStage;
	private static int voiceStage;

	public static bool GameplayActive { get; private set; }
	public static bool HeartSacrificed => Applied.Exists(item => item.Id == FaustId.Heart);

	public static void BeginNewRun()
	{
		Applied.Clear();
		ResolvedStages.Clear();
		int firstRandomStage = Random.Range(1, 3);
		eyesStage = firstRandomStage;
		voiceStage = firstRandomStage == 1 ? 2 : 1;
		initialized = true;
		GameplayActive = false;
		Time.timeScale = 1f;
		AudioListener.pause = false;
		AudioManager.Instance?.SetSfxVolumeOffsetDb(0f);
	}

	public static void EnsureInitialized()
	{
		if (!initialized)
			BeginNewRun();
	}

	public static void BeginStageSelection()
	{
		EnsureInitialized();
		GameplayActive = false;
		Time.timeScale = 0f;
		AudioListener.pause = true;
	}

	public static void ReleaseGameplay()
	{
		GameplayActive = true;
		Time.timeScale = 1f;
		AudioListener.pause = false;
		foreach (MonsterAI monster in Object.FindObjectsByType<MonsterAI>(FindObjectsSortMode.None))
			monster.ResumeAfterFaustSelection();
	}

	public static List<FaustDefinition> GetOffers(FaustCatalog catalog, int stage)
	{
		EnsureInitialized();
		var offers = new List<FaustDefinition>();
		if (catalog == null || ResolvedStages.Contains(stage))
			return offers;

		foreach (FaustDefinition definition in catalog.Fausts)
		{
			if (definition == null || HasBeenApplied(definition.id))
				continue;

			int offeredStage = definition.placement switch
			{
				FaustStagePlacement.Stage1 => 1,
				FaustStagePlacement.Stage2 => 2,
				FaustStagePlacement.Stage3 => 3,
				FaustStagePlacement.RandomStage1Or2 => definition.id == FaustId.Eyes ? eyesStage : voiceStage,
				_ => 0
			};

			if (offeredStage == stage)
				offers.Add(definition);
		}

		return offers;
	}

	public static bool Select(int stage, FaustDefinition definition, FaustCatalog catalog)
	{
		if (definition == null || !CanResolve(stage, definition.id, catalog))
			return false;

		Applied.Add(new AppliedFaust { Id = definition.id, Strength = 1f });
		ResolvedStages.Add(stage);
		return true;
	}

	public static bool Skip(int stage, FaustCatalog catalog)
	{
		List<FaustDefinition> offers = GetOffers(catalog, stage);
		if (offers.Count == 0)
			return false;

		foreach (FaustDefinition offer in offers)
			Applied.Add(new AppliedFaust { Id = offer.id, Strength = 0.5f });

		ResolvedStages.Add(stage);
		return true;
	}

	public static FaustEffectProfile BuildEffectProfile(FaustCatalog catalog)
	{
		var profile = new FaustEffectProfile();
		if (catalog == null)
			return profile;

		foreach (AppliedFaust applied in Applied)
		{
			FaustDefinition definition = catalog.Get(applied.Id);
			if (definition == null)
				continue;

			float strength = applied.Strength;
			switch (applied.Id)
			{
				case FaustId.Touch:
					profile.AccelerationMultiplier *= Mathf.Lerp(1f, definition.accelerationMultiplier, strength);
					profile.DecelerationMultiplier *= Mathf.Lerp(1f, definition.decelerationMultiplier, strength);
					break;

				case FaustId.Eyes:
					profile.AttackRangeMultiplier *= Mathf.Lerp(1f, definition.attackRangeMultiplier, strength);
					profile.VisionAngleOffset += definition.visionAngleOffset * strength;
					break;

				case FaustId.Ears:
					profile.SfxVolumeOffsetDb += definition.sfxVolumeOffsetDb * strength;
					profile.VisionAngleOffset += Mathf.Abs(definition.visionAngleOffset) * strength;
					break;

				case FaustId.Heart:
					profile.AttackMissChance = Mathf.Clamp01(
						profile.AttackMissChance + definition.attackMissChance * strength
					);
					profile.MissRecoveryMin = definition.missRecoveryMin;
					profile.MissRecoveryMax = definition.missRecoveryMax;
					profile.HeartSacrificed = true;
					if (strength >= 1f)
						profile.SprintDisabled = true;
					else
						profile.SprintMaxBpmOverride = definition.halfStrengthSprintMaxBpm;
					break;

				case FaustId.Voice:
					profile.SoundEmitterRadiusMultiplier *= Mathf.Lerp(
						1f,
						definition.soundEmitterRadiusMultiplier,
						strength
					);
					if (strength >= 1f && definition.hideHudAtFullStrength)
						profile.HideHud = true;
					else
						profile.HideHeartbeatHud = true;
					break;
			}
		}

		return profile;
	}

	private static bool CanResolve(int stage, FaustId id, FaustCatalog catalog)
	{
		if (catalog == null || ResolvedStages.Contains(stage) || HasBeenApplied(id))
			return false;

		return GetOffers(catalog, stage).Exists(offer => offer.id == id);
	}

	private static bool HasBeenApplied(FaustId id)
	{
		return Applied.Exists(item => item.Id == id);
	}
}
