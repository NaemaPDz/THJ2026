using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VisionOcclusionMask : MonoBehaviour
{
	[Header("References")]
	public Transform target;
	public Camera mainCamera;
	public Camera maskCamera;
	public PolygonCollider2D[] wallPolygons;

	[Header("Occlusion")]
	public LayerMask occluderMask;
	[Min(0.1f)] public float visionRadius = 8f;
	[Range(32, 512)] public int rayCount = 180;
	[Range(0.001f, 0.5f)]
	public float cornerAngleOffset = 0.05f;
	[Min(0f)] public float wallInset = 0.015f;

	private MeshFilter meshFilter;
	private MeshRenderer meshRenderer;
	private Mesh visibilityMesh;
	private RenderTexture occlusionTexture;

	private readonly List<Vector2> wallVertices = new();
	private readonly List<float> angles = new();
	private readonly List<float> uniqueAngles = new();

	private Vector3[] meshVertices;
	private int[] meshTriangles;

	public Texture OcclusionTexture => occlusionTexture;

	public bool IsReady { get; private set; }

	private void Awake()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();

		visibilityMesh = new Mesh
		{
			name = "Vision Occlusion Mesh"
		};
		visibilityMesh.MarkDynamic();
		meshFilter.sharedMesh = visibilityMesh;

		CacheWallVertices();

		if (maskCamera != null)
			maskCamera.enabled = false;
	}

	private void CacheWallVertices()
	{
		wallVertices.Clear();

		if (wallPolygons == null)
			return;

		foreach (PolygonCollider2D polygon in wallPolygons)
		{
			if (polygon == null)
				continue;

			for (int p = 0; p < polygon.pathCount; p++)
			{
				Vector2[] path = polygon.GetPath(p);

				foreach (Vector2 localPoint in path)
				{
					// Include the collider's local offset.
					Vector3 local = localPoint + polygon.offset;
					Vector3 world =
						polygon.transform.TransformPoint(local);

					wallVertices.Add(world);
				}
			}
		}
	}

	private void LateUpdate()
	{
		IsReady = false;

		if (target == null ||
			mainCamera == null ||
			maskCamera == null)
			return;

		EnsureRenderTexture();
		SyncMaskCamera();
		BuildVisibilityMesh();
		maskCamera.Render();

		IsReady = occlusionTexture != null &&
				  occlusionTexture.IsCreated();
	}

	private void EnsureRenderTexture()
	{
		int width = Mathf.Max(1, Screen.width);
		int height = Mathf.Max(1, Screen.height);

		if (occlusionTexture != null &&
			occlusionTexture.width == width &&
			occlusionTexture.height == height)
			return;

		if (occlusionTexture != null)
		{
			maskCamera.targetTexture = null;
			occlusionTexture.Release();
			Destroy(occlusionTexture);
		}

		occlusionTexture = new RenderTexture(
			width, height, 0, RenderTextureFormat.ARGB32
		);

		occlusionTexture.name = "Vision Occlusion Mask";
		occlusionTexture.filterMode = FilterMode.Bilinear;
		occlusionTexture.wrapMode = TextureWrapMode.Clamp;
		occlusionTexture.Create();

		maskCamera.targetTexture = occlusionTexture;
	}

	private void SyncMaskCamera()
	{
		maskCamera.transform.SetPositionAndRotation(
			mainCamera.transform.position,
			mainCamera.transform.rotation
		);

		maskCamera.orthographic = mainCamera.orthographic;
		maskCamera.orthographicSize = mainCamera.orthographicSize;
		maskCamera.fieldOfView = mainCamera.fieldOfView;
		maskCamera.nearClipPlane = mainCamera.nearClipPlane;
		maskCamera.farClipPlane = mainCamera.farClipPlane;
		maskCamera.aspect = mainCamera.aspect;

		// Render only the visibility mesh layer.
		maskCamera.cullingMask = 1 << gameObject.layer;
		maskCamera.clearFlags = CameraClearFlags.SolidColor;
		maskCamera.backgroundColor = Color.black;
	}

	private void BuildVisibilityMesh()
	{
		Vector2 origin = target.position;

		angles.Clear();
		uniqueAngles.Clear();

		// Uniform rays make the visibility boundary.
		for (int i = 0; i < rayCount; i++)
		{
			angles.Add(i * 360f / rayCount);
		}

		// Add rays on both sides of each wall corner.
		foreach (Vector2 vertex in wallVertices)
		{
			Vector2 delta = vertex - origin;

			if (delta.sqrMagnitude < 0.0001f)
				continue;

			float angle =
				Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;

			angles.Add(Mathf.Repeat(
				angle - cornerAngleOffset, 360f));

			angles.Add(Mathf.Repeat(
				angle, 360f));

			angles.Add(Mathf.Repeat(
				angle + cornerAngleOffset, 360f));
		}

		angles.Sort();

		// Remove duplicate angles.
		foreach (float angle in angles)
		{
			if (uniqueAngles.Count == 0 ||
				Mathf.Abs(angle -
					uniqueAngles[uniqueAngles.Count - 1]) > 0.0001f)
			{
				uniqueAngles.Add(angle);
			}
		}

		int count = uniqueAngles.Count;

		if (count < 3)
			return;

		if (meshVertices == null ||
			meshVertices.Length != count + 1)
		{
			meshVertices = new Vector3[count + 1];
			meshTriangles = new int[count * 3];
		}

		Vector3 worldOrigin = new Vector3(
			origin.x, origin.y, target.position.z);

		meshVertices[0] =
			transform.InverseTransformPoint(worldOrigin);

		for (int i = 0; i < count; i++)
		{
			float radians = uniqueAngles[i] * Mathf.Deg2Rad;

			Vector2 direction = new Vector2(
				Mathf.Cos(radians),
				Mathf.Sin(radians)
			);

			RaycastHit2D hit = Physics2D.Raycast(
				origin,
				direction,
				visionRadius,
				occluderMask
			);

			Vector2 point;

			if (hit.collider != null)
			{
				// Pull the edge slightly back from the wall.
				point = hit.point -
					direction * wallInset;
			}
			else
			{
				point = origin +
					direction * visionRadius;
			}

			Vector3 worldPoint = new Vector3(
				point.x, point.y, target.position.z);

			meshVertices[i + 1] =
				transform.InverseTransformPoint(worldPoint);

			int triangle = i * 3;
			meshTriangles[triangle] = 0;
			meshTriangles[triangle + 1] = (i + 1) % count + 1;
			meshTriangles[triangle + 2] = i + 1;
		}

		visibilityMesh.Clear();
		visibilityMesh.vertices = meshVertices;
		visibilityMesh.triangles = meshTriangles;
		visibilityMesh.RecalculateBounds();
	}

	private void OnDestroy()
	{
		if (maskCamera != null)
			maskCamera.targetTexture = null;

		if (occlusionTexture != null)
		{
			occlusionTexture.Release();
			Destroy(occlusionTexture);
		}

		if (visibilityMesh != null)
			Destroy(visibilityMesh);
	}
}
