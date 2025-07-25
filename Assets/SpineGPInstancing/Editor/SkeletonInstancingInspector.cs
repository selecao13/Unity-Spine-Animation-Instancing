using UnityEngine;
using UnityEditor;
using System.Linq;
using Spine.Unity.Editor;
using System;

namespace Spine.Instancing
{
    [CustomEditor(typeof(SkeletonInstancing))]
    public class SkeletonInstancingInspector : Editor
    {
        class Styles
        {
            public readonly GUIContent LoopLabel = new GUIContent("Loop", "Whether or not .AnimationName should loop. This only applies to the initial animation specified in the inspector, or any subsequent Animations played through .AnimationName. Animations set through state.SetAnimation are unaffected.");
            public readonly GUIContent TimeScaleLabel = new GUIContent("Time Scale", "The rate at which animations progress over time. 1 means normal speed. 0.5 means 50% speed.");
            public readonly GUIContent UnscaledTimeLabel = new GUIContent("Unscaled Time",
                "If enabled, AnimationState uses unscaled game time (Time.unscaledDeltaTime), " +
                    "running animations independent of e.g. game pause (Time.timeScale). " +
                    "Instance SkeletonAnimation.timeScale will still be applied.");
            public readonly GUIContent DataAssetLabel = new GUIContent("SkeletonInstanceData Asset");
            public readonly GUIContent InitialAnimationLabel = new GUIContent("Initial Animation");
            public readonly string NoInstancingAssetWarning = "–Ë“™…Ë÷√ Instancing Data Asset!";
            public readonly string ReloadButtonString = "Reload";
            public GUILayoutOption reloadButtonWidth;
            public GUILayoutOption ReloadButtonWidth { get { return reloadButtonWidth = reloadButtonWidth ?? GUILayout.Width(GUI.skin.label.CalcSize(new GUIContent(ReloadButtonString)).x + 20); } }
            public GUIStyle ReloadButtonStyle { get { return EditorStyles.miniButton; } }
        }

        Styles styles = new Styles();
        SerializedProperty instancingDataAsset;
        SerializedProperty animationName;
        SerializedProperty loop;
        SerializedProperty timeScale;
        SerializedProperty unscaledTime;
        SpineInspectorUtility.SerializedSortingProperties sortingProperties;

        SkeletonInstancing component;
        string[] animationNames;
        int animationIndex = 0;
        bool forceReloadQueued;
        bool wasAnimationParameterChanged = false;
        protected virtual void OnEnable()
        {
            component = target as SkeletonInstancing;
            component.Initialize(false);

            instancingDataAsset = serializedObject.FindProperty("dataAsset");
            animationName = serializedObject.FindProperty("m_animationName");
            loop = serializedObject.FindProperty("loop");
            timeScale = serializedObject.FindProperty("timeScale");
            unscaledTime = serializedObject.FindProperty("unscaledTime");

            SerializedObject renderersSerializedObject = SpineInspectorUtility.GetRenderersSerializedObject(serializedObject); // Allows proper multi-edit behavior.
            sortingProperties = new SpineInspectorUtility.SerializedSortingProperties(renderersSerializedObject);

            if (component.dataAsset != null)
            {
                animationNames = GetAnimationNames(component);
                animationIndex = Array.IndexOf(animationNames, component.animationName);
                animationIndex = animationIndex < 0 ? 0 : animationIndex;
            }
        }

        private string[] GetAnimationNames(SkeletonInstancing skeletonInstancing) 
        {
            var animations = skeletonInstancing.dataAsset.GetSkeletonInstancingData().animations;
            var animNames = new string[animations.Length + 1];
            animNames[0] = "None";
            for (int i = 0; i < animations.Length; i++) 
            {
                animNames[i + 1] = animations[i].name;
            }
            return animNames;
        }

        public void OnSceneGUI()
        {
            if (component == null || component.dataAsset == null)
            {
                return;
            }
            var skeletonInstancingData = component.dataAsset.GetSkeletonInstancingData();
            if (skeletonInstancingData == null)
            {
                return;
            }
            var track = component.animationSate?.GetCurrent();
            int frame = track == null? 0 : track.GetAnimatedData().prevFrame;
            DrawBones(component.transform, skeletonInstancingData, frame);
        }

        public override void OnInspectorGUI()
        {
            SkeletonInstancing component = (SkeletonInstancing)target;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(instancingDataAsset, styles.DataAssetLabel);
                if (EditorGUI.EndChangeCheck()) 
                {
                    serializedObject.ApplyModifiedProperties();
                    component.Initialize(true);
                    if (component.dataAsset != null)
                    {
                        animationNames = GetAnimationNames(component);
                        animationIndex = Array.IndexOf(animationNames, component.animationName);
                        animationIndex = animationIndex < 0 ? 0 : animationIndex;
                    }
                }
            }
            SpineInspectorUtility.SortingPropertyFields(sortingProperties, applyModifiedProperties: true);
            TrySetAnimation(component);

            if (this.component.valid)
            {
                EditorGUI.BeginChangeCheck();
                animationIndex = EditorGUILayout.Popup(styles.InitialAnimationLabel, animationIndex, animationNames);
                if (EditorGUI.EndChangeCheck())
                {
                    if (animationIndex == 0)
                    {
                        animationName.stringValue = null;
                    }
                    else 
                    {
                        animationName.stringValue = animationNames[animationIndex];
                    }
                    wasAnimationParameterChanged = true;
                }
                EditorGUILayout.PropertyField(loop, styles.LoopLabel);
                EditorGUILayout.PropertyField(timeScale, styles.TimeScaleLabel);
                EditorGUILayout.PropertyField(unscaledTime, styles.UnscaledTimeLabel);
            }

            serializedObject.ApplyModifiedProperties();
        }

        static void ReinitializeComponent(SkeletonInstancing component)
        {
            if (component == null)
            {
                return;
            }
            if (component.dataAsset == null || component.dataAsset.GetSkeletonInstancingData() == null)
            {
                return;
            }
            component.Initialize(true);
            component.ApplyAnimation();
        }

        void TrySetAnimation(SkeletonInstancing component) 
        {
            if (component is null) { return; }
            if (!this.component.valid || this.component.animationSate == null)
                return;
            TrackEntry current = this.component.animationSate.GetCurrent();
            string activeAnimation = current != null ? current.animation.name : "";
            bool activeLoop = current != null ? current.isLoop : false ;
            bool animationParameterChanged = this.wasAnimationParameterChanged &&
            ((activeAnimation != animationName.stringValue) || (activeLoop != loop.boolValue));
            if (animationParameterChanged) 
            {
                this.wasAnimationParameterChanged = false;
                var state = component.animationSate;
                if (!Application.isPlaying)
                {
                    if (state != null) 
                        state.ClearTrack();
                }
                var animationName = animationNames[animationIndex];
                var animationToUse = this.component.instanceData.FindAnimation(animationName);
                if (!Application.isPlaying)
                {
                    if (animationToUse.IsValid)
                    {
                        component.animationSate.SetAnimation(animationToUse, loop.boolValue);
                    }
                    this.component.Update(0f);
                }
                else
                {
                    if (animationToUse.IsValid)
                        state.SetAnimation(animationToUse, loop.boolValue);
                    else
                        state.ClearTrack();
                }
            }

        }

        public static void DrawBones(Transform transform, SkeletonInstancingData instanceData, int frame = 0, float positionScale = 1f,
              Vector2? positionOffset = null)
        {
            if (UnityEngine.Event.current.type != EventType.Repaint) return;

            Vector2 offset = positionOffset == null ? Vector2.zero : positionOffset.Value;
            float boneScale = 1.8f; // Draw the root bone largest;

            DrawCrosshairs2D(transform.TransformPoint(instanceData.GetBoneTransform(0, frame).GetPosition()), 0.08f, positionScale);

            var boneCount = instanceData.GetBoneCount();
            for (int i = 0; i < boneCount; i++)
            {
                var boneTransform = instanceData.GetBoneTransform(i, frame);
                var boneLength = instanceData.GetBoneLength(i);
                DrawBone(transform, boneTransform, boneLength, boneScale);
                boneScale = 1f;
            }
        }

        static void DrawCrosshairs2D(Vector3 position, float scale, float skeletonRenderScale = 1f)
        {
            if (UnityEngine.Event.current.type != EventType.Repaint) return;

            scale *= SpineEditorUtilities.Preferences.handleScale * skeletonRenderScale;
            Handles.DrawLine(position + new Vector3(-scale, 0), position + new Vector3(scale, 0));
            Handles.DrawLine(position + new Vector3(0, -scale), position + new Vector3(0, scale));
        }

        static void DrawBoneCircle(Vector3 pos, Color outlineColor, Vector3 normal, float scale = 1f)
        {
            if (UnityEngine.Event.current.type != EventType.Repaint) return;

            scale *= SpineEditorUtilities.Preferences.handleScale;

            Color o = Handles.color;
            Handles.color = outlineColor;
            float firstScale = 0.08f * scale;
            Handles.DrawSolidDisc(pos, normal, firstScale);
            const float Thickness = 0.03f;
            float secondScale = firstScale - (Thickness * SpineEditorUtilities.Preferences.handleScale * scale);

            if (secondScale > 0f)
            {
                Handles.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
                Handles.DrawSolidDisc(pos, normal, secondScale);
            }

            Handles.color = o;
        }

        public static void DrawBone(Transform transform, Matrix4x4 boneTarnsform, float length, float boneScale, float skeletonRenderScale = 1f,
            Vector2? positionOffset = null)
        {
            //var worldPostion = boneTarnsform.GetPosition();
            var a = boneTarnsform.m00;
            var b = boneTarnsform.m10;
            var c = boneTarnsform.m01;
            var d = boneTarnsform.m11;
            var wroldX = boneTarnsform.m30;
            var worldY = boneTarnsform.m31;
            var worldRotationX = Spine.MathUtils.Atan2Deg(c, a);
            var worldScaleX = (float)Math.Sqrt(a * a + c * c);

            if (UnityEngine.Event.current.type != EventType.Repaint) return;

            Vector2 offset = positionOffset == null ? Vector2.zero : positionOffset.Value;
            Vector3 pos = new Vector3(wroldX * skeletonRenderScale + offset.x, worldY * skeletonRenderScale + offset.y, 0);

            if (length > 0)
            {
                Quaternion rot = Quaternion.Euler(0, 0, worldRotationX);
                Vector3 scale = Vector3.one * length * worldScaleX * skeletonRenderScale;
                const float my = 1.5f;
                scale.y *= (SpineEditorUtilities.Preferences.handleScale + 1f) * 0.5f;
                scale.y = Mathf.Clamp(scale.x, -my * skeletonRenderScale, my * skeletonRenderScale);
                SpineHandles.GetBoneMaterial().SetPass(0);
                Graphics.DrawMeshNow(SpineHandles.BoneMesh, transform.localToWorldMatrix * Matrix4x4.TRS(pos, rot, scale));
            }
            else
            {
                Vector3 wp = transform.TransformPoint(pos);
                DrawBoneCircle(wp, SpineHandles.BoneColor, transform.forward, boneScale * skeletonRenderScale);
            }
        }
    }
}
