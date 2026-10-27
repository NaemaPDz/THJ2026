using System;
using UnityEngine;

public static class SoundEmitter
{
	public static event Action<Vector3, float> OnSoundMade;

	public static void EmitSound(Vector3 position, float hearingRadius)
	{
		if (hearingRadius <= 0f) return;

		OnSoundMade?.Invoke(position, hearingRadius);

		Debug.Log($"Sound emitted at {position}");
	}
}
