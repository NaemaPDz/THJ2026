#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Creates AnimationClips from already-sliced Sprite Sheets and adds/updates
/// matching states in an Animator Controller without creating transitions.
/// Place this file inside an Assets/Editor folder.
/// </summary>
public class AnimatorGeneratorWindow : EditorWindow
{
	[Serializable]
	private class AnimationEntry
	{
		public Texture2D spriteSheet;
		public string stateName;
		public float frameRate = 12f;
		public bool loop = true;
	}

	private readonly List<AnimationEntry> entries = new List<AnimationEntry>();
	private Texture2D spriteToAdd;
	private string outputFolder = "Assets/_Project/Animations";
	private string controllerName = "";
	private string prefix = "";
	private Vector2 scrollPosition;
	private string statusMessage;
	private MessageType statusType = MessageType.Info;

	[MenuItem("Tools/Animation/Animator Generator")]
	private static void OpenWindow()
	{
		AnimatorGeneratorWindow window = GetWindow<AnimatorGeneratorWindow>();
		window.titleContent = new GUIContent("Animator Generator");
		window.minSize = new Vector2(520f, 420f);
		window.Show();
	}

	private void OnGUI()
	{
		EditorGUILayout.LabelField("Animator Generator", EditorStyles.boldLabel);
		EditorGUILayout.HelpBox(
			"Add Sprite Sheets that have already been sliced in Sprite Editor. " +
			"Each list entry creates one Animation Clip and one Animator State. " +
			"No transitions are created.",
			MessageType.Info);

		DrawAddSection();
		EditorGUILayout.Space(8);
		DrawAnimationList();
		EditorGUILayout.Space(8);
		DrawOutputSettings();
		EditorGUILayout.Space(10);

		using (new EditorGUI.DisabledScope(entries.Count == 0))
		{
			if (GUILayout.Button("Generate / Update Animator", GUILayout.Height(34)))
				Generate();
		}

		if (!string.IsNullOrEmpty(statusMessage))
		{
			EditorGUILayout.Space(6);
			EditorGUILayout.HelpBox(statusMessage, statusType);
		}
	}

	private void DrawAddSection()
	{
		EditorGUILayout.LabelField("1. Add Sprite Sheet", EditorStyles.boldLabel);
		prefix = EditorGUILayout.TextField("Prefix", prefix);
		using (new EditorGUILayout.HorizontalScope())
		{
			spriteToAdd = (Texture2D)EditorGUILayout.ObjectField(
				"Sprite Sheet", spriteToAdd, typeof(Texture2D), false);

			if (GUILayout.Button("Add to List", GUILayout.Width(100)))
				AddSpriteSheet(spriteToAdd);
		}
	}

	private void DrawAnimationList()
	{
		EditorGUILayout.LabelField($"2. Animation List ({entries.Count})", EditorStyles.boldLabel);

		if (entries.Count == 0)
		{
			EditorGUILayout.HelpBox("Choose a sliced Sprite Sheet above, then click Add to List.", MessageType.None);
			return;
		}

		scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.MinHeight(130));
		for (int i = 0; i < entries.Count; i++)
		{
			AnimationEntry entry = entries[i];
			using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
			{
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(22));
					EditorGUILayout.LabelField(
						entry.spriteSheet != null ? entry.spriteSheet.name : "(Missing Sprite Sheet)",
						EditorStyles.boldLabel);

					using (new EditorGUI.DisabledScope(i == 0))
					{
						if (GUILayout.Button("↑", GUILayout.Width(28)))
						{
							SwapEntries(i, i - 1);
							break;
						}
					}

					using (new EditorGUI.DisabledScope(i >= entries.Count - 1))
					{
						if (GUILayout.Button("↓", GUILayout.Width(28)))
						{
							SwapEntries(i, i + 1);
							break;
						}
					}

					if (GUILayout.Button("Remove", GUILayout.Width(62)))
					{
						entries.RemoveAt(i);
						break;
					}
				}

				entry.spriteSheet = (Texture2D)EditorGUILayout.ObjectField(
					"Source", entry.spriteSheet, typeof(Texture2D), false);
				entry.stateName = EditorGUILayout.TextField("State Name", entry.stateName);
				using (new EditorGUILayout.HorizontalScope())
				{
					entry.frameRate = EditorGUILayout.FloatField("FPS", entry.frameRate);
					entry.loop = EditorGUILayout.ToggleLeft("Loop", entry.loop, GUILayout.Width(70));
				}

				int frameCount = GetSlicedSprites(entry.spriteSheet).Length;
				EditorGUILayout.LabelField(
					frameCount > 0 ? $"Sliced frames: {frameCount}" : "Sliced frames: 0 (check Sprite Editor settings)",
					EditorStyles.miniLabel);
			}
		}
		EditorGUILayout.EndScrollView();
	}

	private void DrawOutputSettings()
	{
		EditorGUILayout.LabelField("3. Output Settings", EditorStyles.boldLabel);

		using (new EditorGUILayout.HorizontalScope())
		{
			outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
			if (GUILayout.Button("Browse", GUILayout.Width(72)))
				BrowseOutputFolder();
		}

		controllerName = EditorGUILayout.TextField("Controller Name", controllerName);
	}

	private void AddSpriteSheet(Texture2D texture)
	{
		if (texture == null)
		{
			SetStatus("Choose a Texture2D asset first.", MessageType.Warning);
			return;
		}

		string path = AssetDatabase.GetAssetPath(texture);
		if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets/", StringComparison.Ordinal))
		{
			SetStatus("Please select a Sprite Sheet asset inside the Assets folder.", MessageType.Warning);
			return;
		}

		if (entries.Any(e => e.spriteSheet == texture))
		{
			SetStatus("That Sprite Sheet is already in the list.", MessageType.Warning);
			return;
		}

		Sprite[] sprites = GetSlicedSprites(texture);
		if (sprites.Length == 0)
		{
			SetStatus(
				"No Sprite sub-assets were found. Set Texture Type to Sprite (2D and UI), " +
				"slice the image in Sprite Editor, then click Apply.",
				MessageType.Warning);
			return;
		}

		string suggestedName = texture.name;
		if (suggestedName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
			suggestedName = suggestedName.Substring(prefix.Length);

		entries.Add(new AnimationEntry
		{
			spriteSheet = texture,
			stateName = suggestedName,
			frameRate = 12f,
			loop = true
		});

		spriteToAdd = null;
		SetStatus($"Added {texture.name} ({sprites.Length} sliced frames).", MessageType.Info);
	}

	private void BrowseOutputFolder()
	{
		string startPath = Path.GetFullPath(string.IsNullOrEmpty(outputFolder) ? "Assets" : outputFolder);
		if (!Directory.Exists(startPath))
			startPath = Application.dataPath;

		string selected = EditorUtility.OpenFolderPanel("Choose Animator Output Folder", startPath, "");
		if (string.IsNullOrEmpty(selected))
			return;

		string assetsPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
		selected = Path.GetFullPath(selected).Replace('\\', '/');

		if (selected == assetsPath || selected.StartsWith(assetsPath + "/", StringComparison.Ordinal))
		{
			outputFolder = "Assets" + selected.Substring(assetsPath.Length);
			if (string.IsNullOrEmpty(outputFolder))
				outputFolder = "Assets";
			SetStatus("Output folder selected.", MessageType.Info);
		}
		else
		{
			SetStatus("Choose a folder inside this Unity project's Assets folder.", MessageType.Warning);
		}
	}

	private void Generate()
	{
		if (string.IsNullOrWhiteSpace(controllerName))
		{
			SetStatus("Controller Name cannot be empty.", MessageType.Error);
			return;
		}

		if (!IsValidAssetsFolderPath(outputFolder))
		{
			SetStatus("Output Folder must be a valid path inside Assets, such as Assets/Animations/Player.", MessageType.Error);
			return;
		}

		List<AnimationEntry> validEntries = entries
			.Where(e => e.spriteSheet != null)
			.ToList();

		if (validEntries.Count == 0)
		{
			SetStatus("There are no valid Sprite Sheets in the list.", MessageType.Warning);
			return;
		}

		HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (AnimationEntry entry in validEntries)
		{
			if (string.IsNullOrWhiteSpace(entry.stateName))
			{
				SetStatus("Every entry needs a State Name.", MessageType.Error);
				return;
			}

			if (!names.Add(entry.stateName.Trim()))
			{
				SetStatus($"Duplicate State Name: {entry.stateName}. State names must be unique.", MessageType.Error);
				return;
			}

			if (entry.frameRate <= 0f)
			{
				SetStatus($"FPS must be greater than zero for {entry.stateName}.", MessageType.Error);
				return;
			}

			if (GetSlicedSprites(entry.spriteSheet).Length == 0)
			{
				SetStatus($"No sliced Sprite frames found for {entry.spriteSheet.name}.", MessageType.Error);
				return;
			}
		}

		EnsureFolderExists(outputFolder);
		string clipsFolder = outputFolder.TrimEnd('/') + "/Clips";
		EnsureFolderExists(clipsFolder);

		string controllerPath = outputFolder.TrimEnd('/') + "/" + controllerName.Trim() + ".controller";
		AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

		if (controller == null)
		{
			if (AssetDatabase.LoadMainAssetAtPath(controllerPath) != null)
			{
				SetStatus($"A non-Animator asset already exists at {controllerPath}. Choose another Controller Name.", MessageType.Error);
				return;
			}

			controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
		}

		if (controller == null || controller.layers.Length == 0)
		{
			SetStatus("Could not create or load an Animator Controller with a Base Layer.", MessageType.Error);
			return;
		}

		AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
		int createdOrUpdated = 0;
		List<string> skipped = new List<string>();

		try
		{
			for (int i = 0; i < validEntries.Count; i++)
			{
				AnimationEntry entry = validEntries[i];
				Sprite[] sprites = GetSlicedSprites(entry.spriteSheet);

				string clipPath = clipsFolder + "/" + MakeSafeFileName(entry.stateName.Trim()) + ".anim";
				AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

				if (clip == null)
				{
					if (AssetDatabase.LoadMainAssetAtPath(clipPath) != null)
					{
						skipped.Add($"{entry.stateName}: a different asset already exists at {clipPath}");
						continue;
					}

					clip = new AnimationClip();
					AssetDatabase.CreateAsset(clip, clipPath);
				}

				clip.frameRate = entry.frameRate;
				ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[sprites.Length];

				for (int frame = 0; frame < sprites.Length; frame++)
				{
					keys[frame] = new ObjectReferenceKeyframe
					{
						time = frame / entry.frameRate,
						value = sprites[frame]
					};
				}

				EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(
					string.Empty, typeof(SpriteRenderer), "m_Sprite");

				AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

				AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
				settings.loopTime = entry.loop;
				AnimationUtility.SetAnimationClipSettings(clip, settings);
				EditorUtility.SetDirty(clip);

				string stateName = entry.stateName.Trim();
				AnimatorState state = stateMachine.states
					.Select(child => child.state)
					.FirstOrDefault(s => s.name == stateName);

				if (state == null)
				{
					state = stateMachine.AddState(
						stateName,
						new Vector3(250f + (i % 4) * 220f, 100f + (i / 4) * 100f, 0f));
				}

				// Updating motion on an existing state preserves its transitions.
				state.motion = clip;
				EditorUtility.SetDirty(state);
				createdOrUpdated++;
			}

			EditorUtility.SetDirty(controller);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Selection.activeObject = controller;
			EditorGUIUtility.PingObject(controller);

			string message = $"Done! Created/updated {createdOrUpdated} Animation State(s).\nController: {controllerPath}\nClips: {clipsFolder}";
			if (skipped.Count > 0)
				message += "\nSkipped:\n" + string.Join("\n", skipped);

			SetStatus(message, skipped.Count > 0 ? MessageType.Warning : MessageType.Info);
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			SetStatus("Generation failed. See the Unity Console for details.", MessageType.Error);
		}
	}

	private static Sprite[] GetSlicedSprites(Texture2D texture)
	{
		if (texture == null)
			return Array.Empty<Sprite>();

		string path = AssetDatabase.GetAssetPath(texture);
		if (string.IsNullOrEmpty(path))
			return Array.Empty<Sprite>();

		return AssetDatabase.LoadAllAssetsAtPath(path)
			.OfType<Sprite>()
			.OrderBy(sprite => sprite.name, Comparer<string>.Create(EditorUtility.NaturalCompare))
			.ToArray();
	}

	private static bool IsValidAssetsFolderPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !(path == "Assets" || path.StartsWith("Assets/", StringComparison.Ordinal)))
			return false;

		string normalized = path.Replace('\\', '/').TrimEnd('/');
		if (normalized.Split('/').Any(part => part == "." || part == ".."))
			return false;

		return normalized == "Assets" || AssetDatabase.IsValidFolder(normalized) || CanCreateFolderPath(normalized);
	}

	private static bool CanCreateFolderPath(string path)
	{
		string current = "Assets";
		string[] parts = path.Split('/');

		for (int i = 1; i < parts.Length; i++)
		{
			current += "/" + parts[i];
			if (AssetDatabase.IsValidFolder(current))
				continue;

			// A file at the intended folder path prevents creation.
			if (AssetDatabase.LoadMainAssetAtPath(current) != null)
				return false;
		}

		return true;
	}

	private static void EnsureFolderExists(string path)
	{
		string normalized = path.Replace('\\', '/').TrimEnd('/');
		string[] parts = normalized.Split('/');
		string current = parts[0];

		for (int i = 1; i < parts.Length; i++)
		{
			string next = current + "/" + parts[i];
			if (!AssetDatabase.IsValidFolder(next))
			{
				if (AssetDatabase.LoadMainAssetAtPath(next) != null)
					throw new IOException($"Cannot create folder because an asset exists at: {next}");

				string guid = AssetDatabase.CreateFolder(current, parts[i]);
				if (string.IsNullOrEmpty(guid) && !AssetDatabase.IsValidFolder(next))
					throw new IOException($"Could not create folder: {next}");
			}

			current = next;
		}
	}

	private static string MakeSafeFileName(string name)
	{
		foreach (char invalid in Path.GetInvalidFileNameChars())
			name = name.Replace(invalid, '_');

		name = name.Replace('/', '_').Replace('\\', '_');
		return string.IsNullOrWhiteSpace(name) ? "Animation" : name;
	}

	private void SwapEntries(int a, int b)
	{
		AnimationEntry temp = entries[a];
		entries[a] = entries[b];
		entries[b] = temp;
		GUI.FocusControl(null);
		Repaint();
	}

	private void SetStatus(string message, MessageType type)
	{
		statusMessage = message;
		statusType = type;
		Repaint();
	}
}

#endif