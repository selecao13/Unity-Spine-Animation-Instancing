using Spine.Unity.Editor;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Spine.Instancing
{
    [InitializeOnLoad]
    public static class EditorUtilities
    {
        public struct SpawnMenuData
        {
            public Vector3 spawnPoint;
            public Transform parent;
            public int siblingIndex;
            public SkeletonInstancingDataAsset skeletonDataAsset;
            public EditorInstantiation.InstantiateDelegate instantiateDelegate;
            public bool isUI;
        }

        static EditorUtilities()
        {
            EditorApplication.delayCall += Initialize; // delayed so that AssetDatabase is ready.
        }
        static void Initialize()
        {
            SceneView.duringSceneGui -= DragAndDropInstantiation.SceneViewDragAndDrop;
            SceneView.duringSceneGui += DragAndDropInstantiation.SceneViewDragAndDrop;

            DragAndDrop.RemoveDropHandler(DragAndDropInstantiation.HandleDragAndDrop);
            DragAndDrop.AddDropHandler(DragAndDropInstantiation.HandleDragAndDrop);
        }

        public static class DragAndDropInstantiation
        {
            public static void SceneViewDragAndDrop(SceneView sceneview)
            {
                UnityEngine.Event current = UnityEngine.Event.current;
                UnityEngine.Object[] references = DragAndDrop.objectReferences;
                if (current.type == EventType.Layout)
                    return;

                // Allow drag and drop of one SkeletonDataAsset.
                if (references.Length == 1)
                {
                    SkeletonInstancingDataAsset skeletonDataAsset = references[0] as SkeletonInstancingDataAsset;
                    if (skeletonDataAsset != null)
                    {
                        Vector2 mousePos = current.mousePosition;

                        bool invalidSkeletonData = skeletonDataAsset.GetSkeletonInstancingData(true) == null;
                        if (invalidSkeletonData)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                            Handles.BeginGUI();
                            GUI.Label(new Rect(mousePos + new Vector2(20f, 20f), new Vector2(400f, 40f)), new GUIContent(string.Format("{0} is invalid.\nCannot create new Spine GameObject.", skeletonDataAsset.name), SpineEditorUtilities.Icons.warning));
                            Handles.EndGUI();
                            return;
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                            Handles.BeginGUI();
                            GUI.Label(new Rect(mousePos + new Vector2(20f, 20f), new Vector2(400f, 20f)), new GUIContent(string.Format("Create Spine Instancing GameObject ({0})", skeletonDataAsset.name), SpineEditorUtilities.Icons.skeletonDataAssetIcon));
                            Handles.EndGUI();

                            if (current.type == EventType.DragPerform)
                            {
                                RectTransform rectTransform = (Selection.activeGameObject == null) ? null : Selection.activeGameObject.GetComponent<RectTransform>();
                                Plane plane = (rectTransform == null) ? new Plane(Vector3.back, Vector3.zero) : new Plane(-rectTransform.forward, rectTransform.position);
                                Vector3 spawnPoint = MousePointToWorldPoint2D(mousePos, sceneview.camera, plane);
                                ShowInstantiateContextMenu(skeletonDataAsset, spawnPoint, null, 0);
                                DragAndDrop.AcceptDrag();
                                current.Use();
                            }
                        }
                    }
                }
            }

            public static void ShowInstantiateContextMenu(SkeletonInstancingDataAsset skeletonDataAsset, Vector3 spawnPoint,
    Transform parent, int siblingIndex = 0)
            {
                GenericMenu menu = new GenericMenu();

                // SkeletonAnimation
                menu.AddItem(new GUIContent("SkeletonInstancing"), false, HandleSkeletonComponentDrop, new SpawnMenuData
                {
                    skeletonDataAsset = skeletonDataAsset,
                    spawnPoint = spawnPoint,
                    parent = parent,
                    siblingIndex = siblingIndex,
                    instantiateDelegate = (data) => EditorInstantiation.InstantiateSkeletonInstancing(data),
                    isUI = false
                });

                // SkeletonGraphic
                //System.Type skeletonGraphicInspectorType = System.Type.GetType("Spine.Unity.Editor.SkeletonGraphicInspector");
                //if (skeletonGraphicInspectorType != null)
                //{
                //    MethodInfo graphicInstantiateDelegate = skeletonGraphicInspectorType.GetMethod("SpawnSkeletonGraphicFromDrop", BindingFlags.Static | BindingFlags.Public);
                //    if (graphicInstantiateDelegate != null)
                //        menu.AddItem(new GUIContent("SkeletonInstancingGraphic (UI)"), false, HandleSkeletonComponentDrop, new SpawnMenuData
                //        {
                //            skeletonDataAsset = skeletonDataAsset,
                //            spawnPoint = spawnPoint,
                //            parent = parent,
                //            siblingIndex = siblingIndex,
                //            instantiateDelegate = System.Delegate.CreateDelegate(typeof(EditorInstantiation.InstantiateDelegate), graphicInstantiateDelegate) as EditorInstantiation.InstantiateDelegate,
                //            isUI = true
                //        });
                //}
                menu.ShowAsContext();
            }

            public static void HandleSkeletonComponentDrop(object spawnMenuData)
            {
                SpawnMenuData data = (SpawnMenuData)spawnMenuData;

                if (data.skeletonDataAsset.GetSkeletonInstancingData(true) == null)
                {
                    EditorUtility.DisplayDialog("Invalid Skeleton Instancing DataAsset", "Unable to create Spine Instancing GameObject.\n\nPlease check your SkeletonInstancingDataAsset.", "Ok");
                    return;
                }

                bool isUI = data.isUI;

                Component newSkeletonComponent = data.instantiateDelegate.Invoke(data.skeletonDataAsset);
                GameObject newGameObject = newSkeletonComponent.gameObject;
                Transform newTransform = newGameObject.transform;

                GameObject usedParent = data.parent != null ? data.parent.gameObject : isUI ? Selection.activeGameObject : null;
                if (usedParent)
                    newTransform.SetParent(usedParent.transform, false);
                if (data.siblingIndex != 0)
                    newTransform.SetSiblingIndex(data.siblingIndex);

                newTransform.position = isUI ? data.spawnPoint : RoundVector(data.spawnPoint, 2);

                if (isUI)
                {
                    //SkeletonGraphic skeletonGraphic = ((SkeletonGraphic)newSkeletonComponent);
                    //if (usedParent != null && usedParent.GetComponent<RectTransform>() != null)
                    //{
                    //    skeletonGraphic.MatchRectTransformWithBounds();
                    //}
                    //else
                    //    Debug.Log("Created a UI Skeleton GameObject not under a RectTransform. It may not be visible until you parent it to a canvas.");
                    //if (skeletonGraphic.HasMultipleSubmeshInstructions() && !skeletonGraphic.allowMultipleCanvasRenderers)
                    //    Debug.Log("This mesh uses multiple atlas pages or blend modes. " +
                    //        "You need to enable 'Multiple Canvas Renderers for correct rendering. " +
                    //        "Consider packing attachments to a single atlas page if possible.", skeletonGraphic);
                }

                if (!isUI && usedParent != null && usedParent.transform.localScale != Vector3.one)
                    Debug.Log("New Spine Instancing GameObject was parented to a scaled Transform. It may not be the intended size.");

                Selection.activeGameObject = newGameObject;
                //EditorGUIUtility.PingObject(newGameObject); // Doesn't work when setting activeGameObject.
                Undo.RegisterCreatedObjectUndo(newGameObject, "Create Spine Instancing GameObject");
            }

            /// <summary>
            /// Rounds off vector components to a number of decimal digits.
            /// </summary>
            public static Vector3 RoundVector(Vector3 vector, int digits)
            {
                vector.x = (float)System.Math.Round(vector.x, digits);
                vector.y = (float)System.Math.Round(vector.y, digits);
                vector.z = (float)System.Math.Round(vector.z, digits);
                return vector;
            }

            /// <summary>
            /// Converts a mouse point to a world point on a plane.
            /// </summary>
            static Vector3 MousePointToWorldPoint2D(Vector2 mousePosition, Camera camera, Plane plane)
            {
                Vector3 screenPos = new Vector3(mousePosition.x, camera.pixelHeight - mousePosition.y, 0f);
                Ray ray = camera.ScreenPointToRay(screenPos);
                float distance;
                bool hit = plane.Raycast(ray, out distance);
                return ray.GetPoint(distance);
            }


            internal static DragAndDropVisualMode HandleDragAndDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
            {
                SkeletonInstancingDataAsset skeletonDataAsset = DragAndDrop.objectReferences.Length == 0 ? null :
                    DragAndDrop.objectReferences[0] as SkeletonInstancingDataAsset;
                if (skeletonDataAsset == null)
                    return DragAndDropVisualMode.None;
                if (!perform)
                    return DragAndDropVisualMode.Copy;

                GameObject dropTargetObject = UnityEditor.EditorUtility.InstanceIDToObject(dropTargetInstanceID) as GameObject;
                Transform dropTarget = dropTargetObject != null ? dropTargetObject.transform : null;
                Transform parent = dropTarget;
                int siblingIndex = 0;
                if (parent != null)
                {
                    if (dropMode == HierarchyDropFlags.DropBetween)
                    {
                        parent = dropTarget.parent;
                        siblingIndex = dropTarget ? dropTarget.GetSiblingIndex() + 1 : 0;
                    }
                    else if (dropMode == HierarchyDropFlags.DropAbove)
                    {
                        parent = dropTarget.parent;
                        siblingIndex = dropTarget ? dropTarget.GetSiblingIndex() : 0;
                    }
                }
                DragAndDropInstantiation.ShowInstantiateContextMenu(skeletonDataAsset, Vector3.zero, parent, siblingIndex);
                return DragAndDropVisualMode.Copy;
            }
        }
    }
}
