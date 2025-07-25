using UnityEngine;
using System;
namespace Spine.Instancing
{
    [Serializable]
    public struct BoneData 
    {
        public string name;
        public int index;
        public Matrix4x4 bindPose;
        public float length;
    }

    public class SkeletonInstancingDataAsset : ScriptableObject
    {
        [SerializeField]
        public Material sharedMaterial;

        [SerializeField]
        public Mesh sharedMesh;

        [SerializeField]
        public TextAsset animationDataAsset;

        [HideInInspector]
        [SerializeField]
        public BoneData[] bonesData; 

        private SkeletonInstancingData m_skeletonGPUAniamtionData;

        public SkeletonInstancingData GetSkeletonInstancingData(bool realod = false)
        {
            if (animationDataAsset == null)
            {
                return null;
            }

            if (m_skeletonGPUAniamtionData != null && !realod)
            {
                return m_skeletonGPUAniamtionData;
            }

            m_skeletonGPUAniamtionData = new SkeletonInstancingData(this);
            return m_skeletonGPUAniamtionData;
        }

        public void Clear()
        {
            animationDataAsset = null;
        }
    }
}