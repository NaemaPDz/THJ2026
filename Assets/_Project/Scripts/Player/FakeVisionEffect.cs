
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
	private float visionAngleOffset;

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

	private void OnEnable()
	{
		targetCamera = GetComponent<Camera>();

		if (selectiveCamera != null)
			selectiveCamera.enabled = false;
	}

	private void Update()
	{
		if (targetCamera == null)
			targetCamera = GetComponent<Camera>();

		if (player != null && visionMaterial != null)
			UpdateVisionParameters();
	}

	private void UpdateVisionParameters()
	{
		Vector3 mouseScreen = Mouse.current != null
			? (Vector3)Mouse.current.position.ReadValue()
			: new Vector3(
				Screen.width * 0.5f,
				Screen.height * 0.5f,
				0f
			);

		mouseScreen.z = Mathf.Abs(
			targetCamera.transform.position.z -
			player.position.z
		);

		Vector3 mouseWorld =
			targetCamera.ScreenToWorldPoint(mouseScreen);

		Vector2 aimDirection = new Vector2(
			mouseWorld.x - player.position.x,
			mouseWorld.y - player.position.y
		);

		if (aimDirection.sqrMagnitude < 0.0001f)
			aimDirection = Vector2.up;

		aimDirection.Normalize();

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
				aimDirection.x,
				aimDirection.y,
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

	private void EnsureSelectiveTexture(
		RenderTexture source
	)
	{
		if (selectiveTexture != null &&
			selectiveTexture.width == source.width &&
			selectiveTexture.height == source.height)
		{
			return;
		}

		ReleaseSelectiveTexture();

		selectiveTexture = new RenderTexture(
			source.width,
			source.height,
			0,
			RenderTextureFormat.ARGB32
		);

		selectiveTexture.name = "Selective Vision Texture";
		selectiveTexture.filterMode = FilterMode.Bilinear;
		selectiveTexture.wrapMode = TextureWrapMode.Clamp;
		selectiveTexture.Create();
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
			EnsureSelectiveTexture(source);
			SyncSelectiveCamera();

			selectiveCamera.targetTexture = selectiveTexture;
			selectiveCamera.Render();

			visionMaterial.SetTexture(
				SelectiveTexID,
				selectiveTexture
			);
		}

		Graphics.Blit(
			source,
			destination,
			visionMaterial
		);
	}

	private void ReleaseSelectiveTexture()
	{
		if (selectiveCamera != null &&
			selectiveCamera.targetTexture == selectiveTexture)
		{
			selectiveCamera.targetTexture = null;
		}

		if (selectiveTexture != null)
		{
			selectiveTexture.Release();
			Destroy(selectiveTexture);
			selectiveTexture = null;
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
