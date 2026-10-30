
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class FakeVisionEffect : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private VisionOcclusionMask occlusionMask;
	[SerializeField] private Transform player;
	[SerializeField] private Material visionMaterial;

	[Header("Selective Rendering")]
	[SerializeField] private Camera selectiveCamera;
	[SerializeField] private LayerMask selectiveLayers;
	[SerializeField] private LayerMask monsterLayers = 1 << 7;

	[Header("Ambient Monster Vision")]
	[Min(0.1f)]
	[SerializeField] private float ambientMonsterRadius = 5f;
	[Range(0f, 1f)]
	[SerializeField] private float ambientMonsterMinAlpha = 0.1f;

	[Header("Near Vision")]
	[Min(0.1f)]
	[SerializeField] private float nearRadius = 1.8f;

	[Min(0.01f)]
	[SerializeField] private float nearSoftness = 0.45f;

	[Header("Cone Vision")]
	[Min(0.1f)]
	[SerializeField] private float coneLength = 6f;

	[Range(10f, 160f)]
	[SerializeField] private float coneAngle = 70f;

	[Min(0.01f)]
	[SerializeField] private float coneSoftness = 0.35f;

	[Range(0f, 30f)]
	[SerializeField] private float coneAngleSoftness = 5f;

	[Header("Darkness")]
	[Range(0f, 1f)]
	[SerializeField] private float darkness = 0.94f;

	private Camera targetCamera;
	private RenderTexture selectiveTexture;
	private RenderTexture unoccludedMonsterMaskTexture;
	private Renderer[] selectiveRenderers = System.Array.Empty<Renderer>();
	private bool[] selectiveRendererOriginalStates = System.Array.Empty<bool>();
	private Color[] selectiveRendererOriginalColors = System.Array.Empty<Color>();
	private bool[] renderUnoccludedMonster = System.Array.Empty<bool>();
	private float nextRendererRefreshTime;
	private float baseNearRadius;
	private float baseConeLength;
	private float visionAngleOffset;
	private float coneVisibilityMultiplier = 1f;
	private Vector2 currentAimDirection = Vector2.up;

	private static readonly int PlayerPosID =
		Shader.PropertyToID("_PlayerPos");
	private static readonly int AimDirID =
		Shader.PropertyToID("_AimDir");
	private static readonly int CameraPosID =
		Shader.PropertyToID("_CameraPos");
	private static readonly int OrthoSizeID =
		Shader.PropertyToID("_OrthoSize");
	private static readonly int AspectID =
		Shader.PropertyToID("_Aspect");
	private static readonly int NearRadiusID =
		Shader.PropertyToID("_NearRadius");
	private static readonly int NearSoftnessID =
		Shader.PropertyToID("_NearSoftness");
	private static readonly int ConeLengthID =
		Shader.PropertyToID("_ConeLength");
	private static readonly int ConeAngleID =
		Shader.PropertyToID("_ConeAngle");
	private static readonly int ConeSoftnessID =
		Shader.PropertyToID("_ConeSoftness");
	private static readonly int ConeAngleSoftnessID =
		Shader.PropertyToID("_ConeAngleSoftness");
	private static readonly int DarknessID =
		Shader.PropertyToID("_Darkness");
	private static readonly int OcclusionTexID =
		Shader.PropertyToID("_OcclusionTex");
	private static readonly int SelectiveTexID =
		Shader.PropertyToID("_SelectiveTex");
	private static readonly int UnoccludedMonsterTexID =
		Shader.PropertyToID("_UnoccludedMonsterTex");
	private const float RendererRefreshInterval = 0.5f;

	private void OnEnable()
	{
		targetCamera = GetComponent<Camera>();
		baseNearRadius = nearRadius;
		baseConeLength = coneLength;

		if (selectiveCamera != null)
			selectiveCamera.enabled = false;
	}

	private void Update()
	{
		if (targetCamera == null)
			targetCamera = GetComponent<Camera>();

		if (Time.unscaledTime >= nextRendererRefreshTime)
			RefreshSelectiveRenderers();

		if (player != null && visionMaterial != null)
			UpdateVisionParameters();
	}

	private void RefreshSelectiveRenderers()
	{
		nextRendererRefreshTime = Time.unscaledTime + RendererRefreshInterval;
		Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
		int count = 0;

		foreach (Renderer candidate in allRenderers)
		{
			if (candidate != null &&
				(selectiveLayers.value & (1 << candidate.gameObject.layer)) != 0)
				count++;
		}

		selectiveRenderers = new Renderer[count];
		selectiveRendererOriginalStates = new bool[count];
		selectiveRendererOriginalColors = new Color[count];
		renderUnoccludedMonster = new bool[count];
		int index = 0;
		foreach (Renderer candidate in allRenderers)
		{
			if (candidate == null ||
				(selectiveLayers.value & (1 << candidate.gameObject.layer)) == 0)
				continue;

			selectiveRenderers[index++] = candidate;
		}
	}

	private void UpdateVisionParameters()
	{
		currentAimDirection = GetAimDirection();

		visionMaterial.SetVector(
			PlayerPosID,
			new Vector4(
				player.position.x,
				player.position.y,
				0f,
				0f
			)
		);

		visionMaterial.SetVector(
			AimDirID,
			new Vector4(
				currentAimDirection.x,
				currentAimDirection.y,
				0f,
				0f
			)
		);

		visionMaterial.SetFloat(NearRadiusID, nearRadius);
		visionMaterial.SetFloat(NearSoftnessID, nearSoftness);
		visionMaterial.SetFloat(ConeLengthID, coneLength);
		visionMaterial.SetFloat(
			ConeAngleID,
			Mathf.Clamp(coneAngle + visionAngleOffset, 10f, 160f)
		);
		visionMaterial.SetFloat(ConeSoftnessID, coneSoftness);

		visionMaterial.SetFloat(
			ConeAngleSoftnessID,
			coneAngleSoftness
		);

		visionMaterial.SetFloat(DarknessID, darkness);

		visionMaterial.SetVector(
			CameraPosID,
			new Vector4(
				targetCamera.transform.position.x,
				targetCamera.transform.position.y,
				0f,
				0f
			)
		);

		visionMaterial.SetFloat(
			OrthoSizeID,
			targetCamera.orthographicSize
		);

		visionMaterial.SetFloat(
			AspectID,
			targetCamera.aspect
		);
	}

	public void SetVisionAngleOffset(float offset)
	{
		visionAngleOffset = offset;
	}

	public void SetEyesSacrifice(
		float targetNearRadius,
		float strength,
		float targetConeLengthMultiplier,
		float targetConeVisibility)
	{
		strength = Mathf.Clamp01(strength);
		nearRadius = Mathf.Lerp(baseNearRadius, targetNearRadius, strength);
		coneLength = baseConeLength * Mathf.Clamp01(targetConeLengthMultiplier);
		coneVisibilityMultiplier = Mathf.Clamp01(targetConeVisibility);
	}

	public bool CanSeePosition(Vector2 worldPosition)
	{
		if (player == null)
			return false;

		Vector2 origin = player.position;
		Vector2 toPosition = worldPosition - origin;
		float distance = toPosition.magnitude;
		float effectiveConeLength = Mathf.Max(nearRadius, coneLength);

		if (distance > effectiveConeLength)
			return false;

		if (distance > nearRadius)
		{
			Vector2 aimDirection = GetAimDirection();
			float effectiveConeAngle = Mathf.Clamp(
				coneAngle + visionAngleOffset,
				10f,
				160f
			);

			if (Vector2.Angle(aimDirection, toPosition / distance) > effectiveConeAngle * 0.5f)
				return false;
		}

		if (distance <= 0.001f || occlusionMask == null || occlusionMask.occluderMask.value == 0)
			return true;

		return HasClearLineOfSight(worldPosition);
	}

	private bool HasClearLineOfSight(Vector2 worldPosition)
	{
		if (player == null)
			return false;

		Vector2 origin = player.position;
		Vector2 toPosition = worldPosition - origin;
		float distance = toPosition.magnitude;
		if (distance <= 0.001f || occlusionMask == null || occlusionMask.occluderMask.value == 0)
			return true;

		RaycastHit2D hit = Physics2D.Raycast(
			origin,
			toPosition / distance,
			distance,
			occlusionMask.occluderMask
		);
		return hit.collider == null;
	}

	private Vector2 GetAimDirection()
	{
		if (player == null || targetCamera == null)
			return Vector2.up;

		Vector3 mouseScreen = Mouse.current != null
			? (Vector3)Mouse.current.position.ReadValue()
			: new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
		mouseScreen.z = Mathf.Abs(targetCamera.transform.position.z - player.position.z);

		Vector3 mouseWorld = targetCamera.ScreenToWorldPoint(mouseScreen);
		Vector2 aimDirection = (Vector2)(mouseWorld - player.position);
		return aimDirection.sqrMagnitude > 0.0001f
			? aimDirection.normalized
			: Vector2.up;
	}

	private bool IsInsideVision(Vector2 position)
	{
		Vector2 toPosition = position - (Vector2)player.position;
		float distance = toPosition.magnitude;
		if (distance <= nearRadius)
			return true;

		if (coneVisibilityMultiplier <= 0f || distance > coneLength)
			return false;

		return Vector2.Angle(currentAimDirection, toPosition / distance) <=
			Mathf.Clamp(coneAngle + visionAngleOffset, 10f, 160f) * 0.5f + 0.01f;
	}

	private bool BoundsIntersectVision(Bounds bounds)
	{
		Vector2 min = bounds.min;
		Vector2 max = bounds.max;
		Vector2 origin = player.position;
		Vector2 closest = new Vector2(
			Mathf.Clamp(origin.x, min.x, max.x),
			Mathf.Clamp(origin.y, min.y, max.y));

		if (IsInsideVision(closest) ||
			IsInsideVision(new Vector2(min.x, min.y)) ||
			IsInsideVision(new Vector2(min.x, max.y)) ||
			IsInsideVision(new Vector2(max.x, min.y)) ||
			IsInsideVision(new Vector2(max.x, max.y)))
			return true;

		if (coneVisibilityMultiplier <= 0f)
			return false;

		Vector2 aim = currentAimDirection;
		float halfAngle = Mathf.Clamp(coneAngle + visionAngleOffset, 10f, 160f) * 0.5f;
		Vector2 left = Rotate(aim, -halfAngle);
		Vector2 right = Rotate(aim, halfAngle);
		if (RayIntersectsBounds(origin, left, min, max) ||
			RayIntersectsBounds(origin, right, min, max))
			return true;

		return CircleIntersectsBounds(origin, coneLength, min, max) ||
			(nearRadius > 0f && CircleIntersectsBounds(origin, nearRadius, min, max));
	}

	private bool BoundsWithinRadius(Bounds bounds, float radius)
	{
		Vector2 min = bounds.min;
		Vector2 max = bounds.max;
		Vector2 origin = player.position;
		Vector2 closest = new Vector2(
			Mathf.Clamp(origin.x, min.x, max.x),
			Mathf.Clamp(origin.y, min.y, max.y));
		return (closest - origin).sqrMagnitude <= radius * radius;
	}

	private float GetAmbientMonsterAlpha(Bounds bounds)
	{
		float distance = Vector2.Distance(player.position, bounds.center);
		float fade = Mathf.InverseLerp(nearRadius, ambientMonsterRadius, distance);
		return Mathf.Lerp(1f, ambientMonsterMinAlpha, fade);
	}

	private bool BoundsHasDirectVision(Bounds bounds)
	{
		Vector2 min = bounds.min;
		Vector2 max = bounds.max;
		Vector2 origin = player.position;
		Vector2 closest = new Vector2(
			Mathf.Clamp(origin.x, min.x, max.x),
			Mathf.Clamp(origin.y, min.y, max.y));

		return CanSeePosition(bounds.center) || CanSeePosition(closest) ||
			CanSeePosition(new Vector2(min.x, min.y)) ||
			CanSeePosition(new Vector2(min.x, max.y)) ||
			CanSeePosition(new Vector2(max.x, min.y)) ||
			CanSeePosition(new Vector2(max.x, max.y));
	}

	private static Vector2 Rotate(Vector2 vector, float degrees)
	{
		float radians = degrees * Mathf.Deg2Rad;
		float sin = Mathf.Sin(radians);
		float cos = Mathf.Cos(radians);
		return new Vector2(vector.x * cos - vector.y * sin,
			vector.x * sin + vector.y * cos);
	}

	private bool RayIntersectsBounds(Vector2 origin, Vector2 direction, Vector2 min, Vector2 max)
	{
		if (Mathf.Abs(direction.x) > 0.00001f)
		{
			float distance = (min.x - origin.x) / direction.x;
			if (CheckRayPoint(distance, new Vector2(min.x, origin.y + direction.y * distance), min, max))
				return true;
			distance = (max.x - origin.x) / direction.x;
			if (CheckRayPoint(distance, new Vector2(max.x, origin.y + direction.y * distance), min, max))
				return true;
		}

		if (Mathf.Abs(direction.y) > 0.00001f)
		{
			float distance = (min.y - origin.y) / direction.y;
			if (CheckRayPoint(distance, new Vector2(origin.x + direction.x * distance, min.y), min, max))
				return true;
			distance = (max.y - origin.y) / direction.y;
			if (CheckRayPoint(distance, new Vector2(origin.x + direction.x * distance, max.y), min, max))
				return true;
		}

		return false;
	}

	private bool CheckRayPoint(float distance, Vector2 point, Vector2 min, Vector2 max)
	{
		return distance >= 0f && distance <= coneLength &&
			point.x >= min.x && point.x <= max.x &&
			point.y >= min.y && point.y <= max.y &&
			IsInsideVision(point);
	}

	private bool CircleIntersectsBounds(Vector2 center, float radius, Vector2 min, Vector2 max)
	{
		float closestX = Mathf.Clamp(center.x, min.x, max.x);
		float closestY = Mathf.Clamp(center.y, min.y, max.y);
		float closestDistance = (new Vector2(closestX, closestY) - center).sqrMagnitude;
		if (closestDistance <= radius * radius && IsInsideVision(new Vector2(closestX, closestY)))
			return true;

		return CircleIntersectsVerticalEdge(center, radius, min.x, min.y, max.y) ||
			CircleIntersectsVerticalEdge(center, radius, max.x, min.y, max.y) ||
			CircleIntersectsHorizontalEdge(center, radius, min.y, min.x, max.x) ||
			CircleIntersectsHorizontalEdge(center, radius, max.y, min.x, max.x);
	}

	private bool CircleIntersectsVerticalEdge(Vector2 center, float radius, float x, float minY, float maxY)
	{
		float remaining = radius * radius - (x - center.x) * (x - center.x);
		if (remaining < 0f)
			return false;

		float offset = Mathf.Sqrt(remaining);
		float lowerY = center.y - offset;
		float upperY = center.y + offset;
		return (lowerY >= minY && lowerY <= maxY && IsInsideVision(new Vector2(x, lowerY))) ||
			(upperY >= minY && upperY <= maxY && IsInsideVision(new Vector2(x, upperY)));
	}

	private bool CircleIntersectsHorizontalEdge(Vector2 center, float radius, float y, float minX, float maxX)
	{
		float remaining = radius * radius - (y - center.y) * (y - center.y);
		if (remaining < 0f)
			return false;

		float offset = Mathf.Sqrt(remaining);
		float lowerX = center.x - offset;
		float upperX = center.x + offset;
		return (lowerX >= minX && lowerX <= maxX && IsInsideVision(new Vector2(lowerX, y))) ||
			(upperX >= minX && upperX <= maxX && IsInsideVision(new Vector2(upperX, y)));
	}

	private void SyncSelectiveCamera()
	{
		if (selectiveCamera == null)
			return;

		selectiveCamera.transform.SetPositionAndRotation(
			targetCamera.transform.position,
			targetCamera.transform.rotation
		);

		selectiveCamera.orthographic = targetCamera.orthographic;
		selectiveCamera.orthographicSize =
			targetCamera.orthographicSize;
		selectiveCamera.aspect = targetCamera.aspect;
		selectiveCamera.fieldOfView = targetCamera.fieldOfView;
		selectiveCamera.nearClipPlane = targetCamera.nearClipPlane;
		selectiveCamera.farClipPlane = targetCamera.farClipPlane;

		selectiveCamera.cullingMask = selectiveLayers.value;
		selectiveCamera.clearFlags = CameraClearFlags.SolidColor;
		selectiveCamera.backgroundColor =
			new Color(0f, 0f, 0f, 0f);
		selectiveCamera.allowHDR = false;
		selectiveCamera.allowMSAA = false;
	}

	private RenderTexture CreateSelectiveTexture(RenderTexture source, string textureName)
	{
		RenderTexture texture = new RenderTexture(
			source.width,
			source.height,
			0,
			RenderTextureFormat.ARGB32
		);

		texture.name = textureName;
		texture.filterMode = FilterMode.Bilinear;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.Create();
		return texture;
	}

	private void EnsureSelectiveTextures(RenderTexture source)
	{
		if (selectiveTexture != null &&
			selectiveTexture.width == source.width &&
			selectiveTexture.height == source.height &&
			unoccludedMonsterMaskTexture != null &&
			unoccludedMonsterMaskTexture.width == source.width &&
			unoccludedMonsterMaskTexture.height == source.height)
			return;

		ReleaseSelectiveTexture();
		selectiveTexture = CreateSelectiveTexture(source, "Selective Vision Texture");
		unoccludedMonsterMaskTexture = CreateSelectiveTexture(source, "Unoccluded Monster Mask");
	}

	private void OnRenderImage(
		RenderTexture source,
		RenderTexture destination
	)
	{
		if (visionMaterial == null ||
			player == null ||
			targetCamera == null ||
			!targetCamera.orthographic)
		{
			Graphics.Blit(source, destination);
			return;
		}

		UpdateVisionParameters();

		Texture maskTexture =
			occlusionMask != null &&
			occlusionMask.IsReady &&
			occlusionMask.OcclusionTexture != null
				? occlusionMask.OcclusionTexture
				: Texture2D.whiteTexture;

		visionMaterial.SetTexture(
			OcclusionTexID,
			maskTexture
		);

		if (selectiveCamera != null)
		{
			EnsureSelectiveTextures(source);
			SyncSelectiveCamera();

			RenderSelectiveObjects();

			visionMaterial.SetTexture(
				SelectiveTexID,
				selectiveTexture
			);
			visionMaterial.SetTexture(
				UnoccludedMonsterTexID,
				unoccludedMonsterMaskTexture
			);
		}
		else
		{
			visionMaterial.SetTexture(SelectiveTexID, Texture2D.blackTexture);
			visionMaterial.SetTexture(UnoccludedMonsterTexID, Texture2D.blackTexture);
		}

		Graphics.Blit(
			source,
			destination,
			visionMaterial
		);
	}

	private void RenderSelectiveObjects()
	{
		try
		{
			for (int i = 0; i < selectiveRenderers.Length; i++)
			{
				Renderer renderer = selectiveRenderers[i];
				if (renderer == null)
					continue;

				selectiveRendererOriginalStates[i] = renderer.forceRenderingOff;
				selectiveRendererOriginalColors[i] = renderer is SpriteRenderer spriteRenderer
					? spriteRenderer.color
					: Color.white;

				bool isMonster =
					(monsterLayers.value & (1 << renderer.gameObject.layer)) != 0;
				bool intersectsLitRegion = player != null && BoundsIntersectVision(renderer.bounds);
				bool directlyLit = isMonster && player != null &&
					BoundsHasDirectVision(renderer.bounds);
				bool inAmbientMonsterRange = isMonster && player != null &&
					BoundsWithinRadius(renderer.bounds, ambientMonsterRadius);
				bool shouldRender = intersectsLitRegion || inAmbientMonsterRange;
				renderUnoccludedMonster[i] = isMonster && shouldRender &&
					HasClearLineOfSight(renderer.bounds.center);

				renderer.forceRenderingOff = renderer.forceRenderingOff || !shouldRender;

				if (isMonster && inAmbientMonsterRange && !directlyLit &&
					renderer is SpriteRenderer fadedSprite)
				{
					Color color = fadedSprite.color;
					color.a *= GetAmbientMonsterAlpha(renderer.bounds);
					fadedSprite.color = color;
				}
			}

			selectiveCamera.targetTexture = selectiveTexture;
			selectiveCamera.cullingMask = selectiveLayers.value;
			selectiveCamera.Render();

			// Keep color output in one sorted pass; this pass only marks clear-ray monsters.
			for (int i = 0; i < selectiveRenderers.Length; i++)
			{
				Renderer renderer = selectiveRenderers[i];
				if (renderer == null)
					continue;

				renderer.forceRenderingOff = selectiveRendererOriginalStates[i] ||
					!renderUnoccludedMonster[i];
				if (renderUnoccludedMonster[i] && renderer is SpriteRenderer spriteRenderer)
				{
					Color maskColor = spriteRenderer.color;
					maskColor.a = 1f;
					spriteRenderer.color = maskColor;
				}
			}

			selectiveCamera.cullingMask = monsterLayers.value;
			selectiveCamera.targetTexture = unoccludedMonsterMaskTexture;
			selectiveCamera.Render();
		}
		finally
		{
			for (int i = 0; i < selectiveRenderers.Length; i++)
			{
				Renderer renderer = selectiveRenderers[i];
				if (renderer == null)
					continue;

				renderer.forceRenderingOff = selectiveRendererOriginalStates[i];
				if (renderer is SpriteRenderer spriteRenderer)
					spriteRenderer.color = selectiveRendererOriginalColors[i];
			}

			selectiveCamera.cullingMask = selectiveLayers.value;
			selectiveCamera.targetTexture = selectiveTexture;
		}
	}

	private void ReleaseSelectiveTexture()
	{
		if (selectiveCamera != null)
			selectiveCamera.targetTexture = null;

		if (selectiveTexture != null)
		{
			selectiveTexture.Release();
			Destroy(selectiveTexture);
			selectiveTexture = null;
		}

		if (unoccludedMonsterMaskTexture != null)
		{
			unoccludedMonsterMaskTexture.Release();
			Destroy(unoccludedMonsterMaskTexture);
			unoccludedMonsterMaskTexture = null;
		}
	}

	private void OnDisable()
	{
		ReleaseSelectiveTexture();

		if (selectiveCamera != null)
			selectiveCamera.enabled = false;
	}

	private void OnDestroy()
	{
		ReleaseSelectiveTexture();
	}
}
