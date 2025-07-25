using UnityEngine;
using UnityEditor;
using static Spine.Unity.Editor.SpineEditorUtilities;
using System;

namespace Spine.Instancing
{

    [CustomEditor(typeof(SkeletonInstancingDataAsset)), CanEditMultipleObjects]
    public class SkeletonInstancingDataAssetInspector : Editor
    {
        class Styles
        {

        }
        GUIStyle activePlayButtonStyle, idlePlayButtonStyle;
        SerializedProperty animationInfosAsset;
        SkeletonInstancingDataAsset targetSkeletonInstancingDataAsset;
        SkeletonInstancingData targetSkeletonInstacingData;
        bool showAnimationList;
        bool showAnimationTexture;
        bool showUVAnimationTexture;
        bool showVertexColorAnimationTexutre;
        GameObject previewGameObject;
        SkeletonInstanceInspectorPreview preview = new SkeletonInstanceInspectorPreview();

        private void OnEnable()
        {
            InitializeEditor();
        }

        private void OnDisable()
        {
            HandleOnDestroyPreview();
        }

        void OnDestroy()
        {
            HandleOnDestroyPreview();
        }

        public override bool HasPreviewGUI()
        {
            if (serializedObject.isEditingMultipleObjects) return false;
            return targetSkeletonInstancingDataAsset != null && targetSkeletonInstancingDataAsset.GetSkeletonInstancingData(false) != null;
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            preview.Initialize(this.Repaint, targetSkeletonInstancingDataAsset);
            preview.HandleInteractivePreviewGUI(r, background);
        }


        void InitializeEditor()
        {
            targetSkeletonInstancingDataAsset = (SkeletonInstancingDataAsset)target;
            animationInfosAsset = serializedObject.FindProperty("animationInfosAsset");

            if (targetSkeletonInstancingDataAsset.animationDataAsset != null)
            {
                targetSkeletonInstacingData = targetSkeletonInstancingDataAsset.GetSkeletonInstancingData();
            }

            EditorApplication.update -= preview.HandleEditorUpdate;
            EditorApplication.update += preview.HandleEditorUpdate;

            if (targetSkeletonInstacingData != null)
            {
                preview.Initialize(this.Repaint, targetSkeletonInstancingDataAsset);
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            { // Lazy initialization because accessing EditorStyles values in OnEnable during a recompile causes UnityEditor to throw null exceptions. (Unity 5.3.5)
                idlePlayButtonStyle = idlePlayButtonStyle ?? new GUIStyle(EditorStyles.miniButton);
                if (activePlayButtonStyle == null)
                {
                    activePlayButtonStyle = new GUIStyle(idlePlayButtonStyle);
                    activePlayButtonStyle.normal.textColor = Color.red;
                }
            }

            EditorGUILayout.Space(20);
            if (targetSkeletonInstacingData != null)
            {
                showAnimationList = EditorGUILayout.Foldout(showAnimationList, new GUIContent(string.Format("Animations [{0}]", targetSkeletonInstacingData.animations.Length), Icons.animationRoot));
                if (showAnimationList)
                {
                    //Title
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Name", GUILayout.Width(100));
                    EditorGUILayout.LabelField("FrameCount", GUILayout.Width(80));
                    EditorGUILayout.LabelField("FrameOffset", GUILayout.Width(100));
                    EditorGUILayout.LabelField("FPS", GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();

                    foreach (var animInfo in targetSkeletonInstacingData.animations)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (preview.IsValid)
                        {
                            bool active = preview.ActiveTrack != null && preview.ActiveTrack.animation.name == animInfo.name;
                            //bool sameAndPlaying = active && activeTrack.TimeScale > 0f;
                            if (GUILayout.Button("\u25BA", active ? activePlayButtonStyle : idlePlayButtonStyle, GUILayout.Width(24)))
                            {
                                preview.PlayPauseAnimation(animInfo.name, true);
                            }
                        }
                        else
                        {
                            GUILayout.Label("-", GUILayout.Width(24));
                        }
                        EditorGUILayout.LabelField(new GUIContent(animInfo.name, Icons.animation), GUILayout.Width(120));
                        EditorGUILayout.LabelField(animInfo.frameCount.ToString(), GUILayout.Width(80));
                        EditorGUILayout.LabelField(animInfo.frameOffset.ToString(), GUILayout.Width(80));
                        EditorGUILayout.LabelField(animInfo.fps.ToString(), GUILayout.Width(80));
                        EditorGUILayout.EndHorizontal();
                    }
                }

                if (targetSkeletonInstacingData.boneTexture != null)
                {
                    showAnimationTexture = EditorGUILayout.Foldout(showAnimationTexture, new GUIContent("Bone Texture", Icons.image));
                    if (showAnimationTexture)
                    {
                        var animTexutre = targetSkeletonInstacingData.boneTexture;
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(animTexutre.height), GUILayout.Width(animTexutre.width));
                        EditorGUI.DrawTextureTransparent(rect, animTexutre);
                    }
                }
                if (targetSkeletonInstacingData.vertexColorTexture != null)
                {
                    showVertexColorAnimationTexutre = EditorGUILayout.Foldout(showVertexColorAnimationTexutre, new GUIContent("Vertex Color Texture", Icons.image));
                    if (showVertexColorAnimationTexutre)
                    {
                        var animTexutre = targetSkeletonInstacingData.vertexColorTexture;
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(animTexutre.height), GUILayout.Width(animTexutre.width));
                        EditorGUI.DrawTextureTransparent(rect, animTexutre);
                    }
                }

                if (targetSkeletonInstacingData.uvTexture != null)
                {
                    showUVAnimationTexture = EditorGUILayout.Foldout(showUVAnimationTexture, new GUIContent("UV Texture", Icons.image));
                    if (showUVAnimationTexture)
                    {
                        var animTexutre = targetSkeletonInstacingData.uvTexture;
                        var rect = EditorGUILayout.GetControlRect(GUILayout.Height(animTexutre.height), GUILayout.Width(animTexutre.width));
                        EditorGUI.DrawTextureTransparent(rect, animTexutre);
                    }
                }
            }

            if (GUILayout.Button("Reload"))
            {
                if (targetSkeletonInstancingDataAsset.animationDataAsset != null)
                {
                    targetSkeletonInstacingData = targetSkeletonInstancingDataAsset.GetSkeletonInstancingData(true);
                }
            }
        }

        void HandleOnDestroyPreview()
        {
            EditorApplication.update -= preview.HandleEditorUpdate;
            preview.OnDestroy();
        }

        public override GUIContent GetPreviewTitle() { return new GUIContent("Preview"); }
        public override Texture2D RenderStaticPreview(string assetPath, UnityEngine.Object[] subAssets, int width, int height) { return preview.GetStaticPreview(width, height); }

        internal class SkeletonInstanceInspectorPreview
        {
            Color OriginColor = new Color(0.3f, 0.3f, 0.3f, 1);
            static readonly int SliderHash = "Slider".GetHashCode();

            static float CurrentTime { get { return (float)EditorApplication.timeSinceStartup; } }

            Texture previewTexture;
            PreviewRenderUtility previewRenderUtility;
            GameObject previewGameObject;
            internal bool requiresRefresh;
            Action Repaint;
            SkeletonInstancing skeletonInstancingComp;
            float animationLastTime;

            static Vector3 lastCameraPositionGoal;
            static float lastCameraOrthoGoal;
            float cameraOrthoGoal = 1;
            Vector3 cameraPositionGoal = new Vector3(0, 0, -10);
            double cameraAdjustEndFrame = 0;

            public TrackEntry ActiveTrack { get { return IsValid ? skeletonInstancingComp.animationSate.GetCurrent() : null; } }
            Camera PreviewUtilityCamera
            {
                get
                {
                    if (previewRenderUtility == null) return null;
                    return previewRenderUtility.camera;
                }
            }

            public bool IsValid { get { return skeletonInstancingComp != null && skeletonInstancingComp.valid; } }

            public bool IsPlayingAnimation
            {
                get
                {
                    if (!IsValid) return false;
                    TrackEntry currentTrack = skeletonInstancingComp.animationSate.GetCurrent();
                    return currentTrack != null && currentTrack.timeScale > 0;
                }
            }

            public void Initialize(Action repaintCallback, SkeletonInstancingDataAsset skeletonInstancingDataAsset)
            {
                if (skeletonInstancingDataAsset == null)
                {
                    return;
                }
                if (skeletonInstancingDataAsset.GetSkeletonInstancingData(false) == null)
                {
                    DestroyPreviewGameObject();
                    return;
                }
                this.Repaint = repaintCallback;

                const int PreviewLayer = 30;
                const int PreviewCameraCullingMask = 1 << PreviewLayer;

                if (previewRenderUtility == null)
                {
                    previewRenderUtility = new PreviewRenderUtility(true);
                    animationLastTime = CurrentTime;

                    {
                        Camera c = this.PreviewUtilityCamera;
                        c.orthographic = true;
                        c.cullingMask = PreviewCameraCullingMask;
                        c.nearClipPlane = 0.01f;
                        c.farClipPlane = 1000f;
                        c.orthographicSize = lastCameraOrthoGoal;
                        c.transform.position = lastCameraPositionGoal;
                    }


                    DestroyPreviewGameObject();
                }

                if (previewGameObject == null)
                {
                    try
                    {
                        previewGameObject =EditorInstantiation.InstantiateSkeletonInstancing(skeletonInstancingDataAsset, useObjectFactory: false).gameObject;

                        if (previewGameObject != null)
                        {
                            previewGameObject.hideFlags = HideFlags.HideAndDontSave;
                            previewGameObject.layer = PreviewLayer;
                            skeletonInstancingComp = previewGameObject.GetComponent<SkeletonInstancing>();
                            skeletonInstancingComp.Update(0);
   

                            previewRenderUtility.AddSingleGO(previewGameObject);
                        }
                        previewRenderUtility.AddSingleGO(previewGameObject);
                        if (this.ActiveTrack != null) cameraAdjustEndFrame = EditorApplication.timeSinceStartup + skeletonInstancingComp.animationSate.GetCurrent().animationEnd;
                        AdjustCameraGoals();
                    }
                    catch
                    {
                        DestroyPreviewGameObject();
                    }

                    RefreshOnNextUpdate();
                }
            }

            void AdjustCameraGoals()
            {
                if (previewGameObject == null) return;

                Bounds bounds = previewGameObject.GetComponent<Renderer>().bounds;
                cameraOrthoGoal = bounds.size.y;
                cameraPositionGoal = bounds.center + new Vector3(0, 0, -10f);
            }


            public void AdjustCamera()
            {
                if (previewRenderUtility == null)
                    return;

                if (CurrentTime < cameraAdjustEndFrame)
                    AdjustCameraGoals();

                lastCameraPositionGoal = cameraPositionGoal;
                lastCameraOrthoGoal = cameraOrthoGoal;

                Camera c = this.PreviewUtilityCamera;
                float orthoSet = Mathf.Lerp(c.orthographicSize, cameraOrthoGoal, 0.1f);

                c.orthographicSize = orthoSet;

                float dist = Vector3.Distance(c.transform.position, cameraPositionGoal);
                if (dist > 0f)
                {
                    Vector3 pos = Vector3.Lerp(c.transform.position, cameraPositionGoal, 0.1f);
                    pos.x = 0;
                    c.transform.position = pos;
                    c.transform.rotation = Quaternion.identity;
                    RefreshOnNextUpdate();
                }
            }
            public void RefreshOnNextUpdate()
            {
                requiresRefresh = true;
            }

            public void HandleEditorUpdate()
            {
                AdjustCamera();
                if (IsPlayingAnimation)
                {
                    RefreshOnNextUpdate();
                    Repaint();
                }
                else if (requiresRefresh)
                {
                    Repaint();
                }
            }

            public void HandleInteractivePreviewGUI(Rect r, GUIStyle background)
            {
                if (UnityEngine.Event.current.type == EventType.Repaint)
                {
                    if (requiresRefresh)
                    {
                        previewRenderUtility.BeginPreview(r, background);
                        DoRenderPreview();
                        previewTexture = previewRenderUtility.EndPreview();
                        requiresRefresh = false;
                    }
                    if (previewTexture != null)
                        GUI.DrawTexture(r, previewTexture, ScaleMode.StretchToFill, false);
                }
                DrawTimeBar(r);
            }

            public void DoRenderPreview()
            {
                if (this.PreviewUtilityCamera.activeTexture == null || this.PreviewUtilityCamera.targetTexture == null)
                    return;

                GameObject go = previewGameObject;
                if (requiresRefresh && go != null)
                {
                    Renderer renderer = go.GetComponent<Renderer>();
                    renderer.enabled = true;

                    previewRenderUtility.Render();
                    if (!EditorApplication.isPlaying)
                    {
                        float current = CurrentTime;
                        float deltaTime = (current - animationLastTime);
                        skeletonInstancingComp.Update(deltaTime);
                        animationLastTime = current;
                    }
                    renderer.enabled = false;
                }
            }

            public Texture2D GetStaticPreview(int width, int height)
            {
                Camera c = this.PreviewUtilityCamera;
                if (c == null)
                    return null;

                RefreshOnNextUpdate();
                AdjustCameraGoals();
                c.orthographicSize = cameraOrthoGoal / 2;
                c.transform.position = cameraPositionGoal;
                previewRenderUtility.BeginStaticPreview(new Rect(0, 0, width, height));
                DoRenderPreview();
                Texture2D tex = previewRenderUtility.EndStaticPreview();

                return tex;
            }

            public void PlayPauseAnimation(string animationName, bool loop)
            {

                if (skeletonInstancingComp == null || !skeletonInstancingComp.valid) return;

                if (string.IsNullOrEmpty(animationName))
                {
                    skeletonInstancingComp.animationSate.ClearTrack();
                    return;
                }
                var currentTrack = skeletonInstancingComp.animationSate.GetCurrent();
                if (currentTrack != null && currentTrack.animation.name == animationName)
                {
                    currentTrack.timeScale = (currentTrack.timeScale == 0) ? 1f : 0f; // pause/play
                }
                else
                {
                    skeletonInstancingComp.animationSate.SetAnimation(animationName, loop);
                }

            }

            void DrawTimeBar(Rect r)
            {
                if (skeletonInstancingComp == null)
                    return;

                Rect barRect = new Rect(r);
                barRect.height = 32;
                barRect.x += 4;
                barRect.width -= 4;

                GUI.Box(barRect, "");

                Rect lineRect = new Rect(barRect);
                float lineRectWidth = lineRect.width;
                TrackEntry t = skeletonInstancingComp.animationSate.GetCurrent();

                if (t != null && Icons.userEvent != null)
                { // when changing to play mode, Icons.userEvent  will not be reset
                    float currentTime = t.trackTime;
                    float normalizedTime = currentTime / t.animation.duration;
                    float wrappedTime = normalizedTime % 1f;

                    lineRect.x = barRect.x + (lineRectWidth * wrappedTime) - 0.5f;
                    lineRect.width = 2;

                    GUI.color = Color.red;
                    GUI.DrawTexture(lineRect, EditorGUIUtility.whiteTexture);
                    GUI.color = Color.white;
                }
            }


            public void Clear()
            {
                DisposePreviewRenderUtility();
                DestroyPreviewGameObject();
            }


            void DisposePreviewRenderUtility()
            {
                if (previewRenderUtility != null)
                {
                    previewRenderUtility.Cleanup();
                    previewRenderUtility = null;
                }
            }

            void DestroyPreviewGameObject()
            {
                if (previewGameObject != null)
                {
                    GameObject.DestroyImmediate(previewGameObject);
                    previewGameObject = null;
                }
            }

            public void OnDestroy()
            {
                DisposePreviewRenderUtility();
                DestroyPreviewGameObject();
            }

        }
    }
}

