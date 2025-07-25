using UnityEditor;
using UnityEngine;

namespace Spine.Instancing
{
    public static class EditorInstantiation
    {
		public delegate Component InstantiateDelegate(SkeletonInstancingDataAsset skeletonDataAsset);
		public static SkeletonInstancing InstantiateSkeletonInstancing(SkeletonInstancingDataAsset skeletonDataAsset,
			bool destroyInvalid = true, bool useObjectFactory = true)
		{

			var data = skeletonDataAsset.GetSkeletonInstancingData(true);

			//if (data == null)
			//{
			//	for (int i = 0; i < skeletonDataAsset.atlasAssets.Length; i++)
			//	{
			//		string reloadAtlasPath = AssetDatabase.GetAssetPath(skeletonDataAsset.atlasAssets[i]);
			//		skeletonDataAsset.atlasAssets[i] = (AtlasAssetBase)AssetDatabase.LoadAssetAtPath(reloadAtlasPath, typeof(AtlasAssetBase));
			//	}
			//	data = skeletonDataAsset.GetSkeletonData(false);
			//}

			//if (data == null)
			//{
			//	Debug.LogWarning("InstantiateSkeletonAnimation tried to instantiate a skeleton from an invalid SkeletonDataAsset.", skeletonDataAsset);
			//	return null;
			//}

			string spineGameObjectName = string.Format(skeletonDataAsset.name.Substring(0,skeletonDataAsset.name.IndexOf('_')));
			GameObject go = EditorInstantiation.NewGameObject(spineGameObjectName, useObjectFactory,
				typeof(MeshFilter), typeof(MeshRenderer), typeof(SkeletonInstancing));
			var newSkeletonInstancing = go.GetComponent<SkeletonInstancing>();
			newSkeletonInstancing.dataAsset = skeletonDataAsset;

			// Initialize
			try
			{
				newSkeletonInstancing.Initialize(false);
			}
			catch (System.Exception e)
			{
				if (destroyInvalid)
				{
					Debug.LogWarning("Editor-instantiated SkeletonAnimation threw an Exception. Destroying GameObject to prevent orphaned GameObject.\n" + e.Message, skeletonDataAsset);
					GameObject.DestroyImmediate(go);
				}
				throw e;
			}
			newSkeletonInstancing.loop = false;
			newSkeletonInstancing.Update(0);

			return newSkeletonInstancing;
		}


		/// <summary>Handles creating a new GameObject in the Unity Editor. This uses the new ObjectFactory API where applicable.</summary>
		public static GameObject NewGameObject(string name, bool useObjectFactory)
		{
			if (useObjectFactory)
				return ObjectFactory.CreateGameObject(name);
			return new GameObject(name);
		}

		public static GameObject NewGameObject(string name, bool useObjectFactory, params System.Type[] components)
		{
			if (useObjectFactory)
				return ObjectFactory.CreateGameObject(name, components);
			return new GameObject(name, components);
		}
	}
}
