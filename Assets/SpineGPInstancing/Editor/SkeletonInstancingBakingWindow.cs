using Spine;
using Spine.Unity;
using Spine.Unity.Editor;
using UnityEditor;
using UnityEngine;
using Editor = UnityEditor.Editor;
using Icons = Spine.Unity.Editor.SpineEditorUtilities.Icons;

namespace Spine.Instancing
{

	public class SkeletonInstancingBakingWindow : EditorWindow
	{
		const bool IsUtilityWindow = true;

		[MenuItem("CONTEXT/SkeletonDataAsset/Skeleton Instancing Baking", false, 5000)]
		public static void Init(MenuCommand command)
		{
			SkeletonInstancingBakingWindow window = EditorWindow.GetWindow<SkeletonInstancingBakingWindow>(IsUtilityWindow);
			window.minSize = new Vector2(330f, 530f);
			window.maxSize = new Vector2(600f, 1000f);
			window.titleContent = new GUIContent("Skeleton Instancing Baking", Icons.spine);
			window.skeletonDataAsset = command.context as SkeletonDataAsset;
			window.Show();
		}

		public SkeletonDataAsset skeletonDataAsset;
		[SpineSkin(dataField: "skeletonDataAsset")]
		public string skinToBake = "default";

		// Settings
		//bool bakeAnimations = false;
		//bool bakeIK = true;
		bool flipX = false;
		bool flipY = false;
		int  bakeFPS = 30;
		//SendMessageOptions bakeEventOptions;

		SerializedObject so;
		Skin bakeSkin;


		void DataAssetChanged()
		{
			bakeSkin = null;
		}

		void OnGUI()
		{
			so = so ?? new SerializedObject(this);

			EditorGUIUtility.wideMode = true;
			EditorGUILayout.LabelField("Spine Skeleton GPU Animation Prefab Baking", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			SerializedProperty skeletonDataAssetProperty = so.FindProperty("skeletonDataAsset");
			EditorGUILayout.PropertyField(skeletonDataAssetProperty, new GUIContent("SkeletonDataAsset", Icons.spine));
			if (EditorGUI.EndChangeCheck())
			{
				so.ApplyModifiedProperties();
				DataAssetChanged();
			}
			EditorGUILayout.Space();

			if (skeletonDataAsset == null) return;
			SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(false);
			if (skeletonData == null) return;
			bool hasExtraSkins = skeletonData.Skins.Count > 1;

			using (new SpineInspectorUtility.BoxScope(false))
			{
				EditorGUILayout.LabelField(skeletonDataAsset.name, EditorStyles.boldLabel);
				using (new SpineInspectorUtility.IndentScope())
				{
					EditorGUILayout.LabelField(new GUIContent("Bones: " + skeletonData.Bones.Count, Icons.bone));
					EditorGUILayout.LabelField(new GUIContent("Slots: " + skeletonData.Slots.Count, Icons.slotRoot));

					if (hasExtraSkins)
					{
						EditorGUILayout.LabelField(new GUIContent("Skins: " + skeletonData.Skins.Count, Icons.skinsRoot));
						EditorGUILayout.LabelField(new GUIContent("Current skin attachments: " + (bakeSkin == null ? 0 : bakeSkin.Attachments.Count), Icons.skinPlaceholder));
					}
					else if (skeletonData.Skins.Count == 1)
					{
						EditorGUILayout.LabelField(new GUIContent("Skins: 1 (only default Skin)", Icons.skinsRoot));
					}

					int totalAttachments = 0;
					foreach (Skin s in skeletonData.Skins)
						totalAttachments += s.Attachments.Count;
					EditorGUILayout.LabelField(new GUIContent("Total Attachments: " + totalAttachments, Icons.genericAttachment));
				}
			}
			using (new SpineInspectorUtility.BoxScope(false))
			{
				EditorGUILayout.LabelField("Animations", EditorStyles.boldLabel);
				EditorGUILayout.LabelField(new GUIContent("Animations: " + skeletonData.Animations.Count, Icons.animation));
			}

			using (new SpineInspectorUtility.BoxScope(false))
			{
				EditorGUILayout.LabelField("Bake Settings", EditorStyles.boldLabel);
				flipX = EditorGUILayout.Toggle("FlipX", flipX);
				flipY = EditorGUILayout.Toggle("FlipY", flipY);
				bakeFPS = EditorGUILayout.IntField("Bake FPS", bakeFPS);
			}

			if (!string.IsNullOrEmpty(skinToBake) && UnityEngine.Event.current.type == EventType.Repaint)
				bakeSkin = skeletonData.FindSkin(skinToBake) ?? skeletonData.DefaultSkin;

			EditorGUILayout.Space();
			Texture2D prefabIcon = EditorGUIUtility.FindTexture("PrefabModel Icon");

			if (hasExtraSkins)
			{
				EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(so.FindProperty("skinToBake"));
				if (EditorGUI.EndChangeCheck())
				{
					so.ApplyModifiedProperties();
					Repaint();
				}

				if (SpineInspectorUtility.LargeCenteredButton(new GUIContent(string.Format("Bake Skeleton with Skin ({0})", (bakeSkin == null ? "default" : bakeSkin.Name)), prefabIcon)))
				{
					SkeletonInstancingBaker.Bake(skeletonDataAsset, new ExposedList<Skin>(new[] { bakeSkin }), bakeFPS,flipX,flipY);
				}

				if (SpineInspectorUtility.LargeCenteredButton(new GUIContent(string.Format("Bake All ({0} skins)", skeletonData.Skins.Count), prefabIcon)))
				{
					SkeletonInstancingBaker.Bake(skeletonDataAsset, skeletonData.Skins, bakeFPS,flipX,flipY);
				}
			}
			else
			{
				if (SpineInspectorUtility.LargeCenteredButton(new GUIContent("Bake Skeleton", prefabIcon)))
				{
					SkeletonInstancingBaker.Bake(skeletonDataAsset, new ExposedList<Skin>(new[] { bakeSkin }), bakeFPS,flipX, flipY);
				}
			}
		}
	}
}
