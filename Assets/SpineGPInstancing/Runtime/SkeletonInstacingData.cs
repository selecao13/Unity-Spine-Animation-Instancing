using UnityEngine;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;

namespace Spine.Instancing
{
    public struct InstancingAnimationInfo
    {
        public string animationName;
        public int frameOffset;
        public int frameCount;
        public int fps;

        public bool IsValid => frameCount > 0 && fps > 0;
    }

    public class SkeletonInstancingData
    {
        readonly static int BONE_TEX = Shader.PropertyToID("_BoneTex");
        readonly static int VERTEX_COLOR_TEX = Shader.PropertyToID("_VertColorTex");
        readonly static int UV_TEX = Shader.PropertyToID("_UVTex");

        public string name { get; private set; }
        public Texture2D boneTexture { get; private set; }
        public Texture2D vertexColorTexture { get; private set; }
        public bool hasVertexColorAnim { get; private set; }
        public Texture2D uvTexture { get; private set; }
        public bool hasUVAnim { get; private set; }
        public Material sharedMaterial { get; private set; }
        public Mesh sharedMesh { get; private set; }

        public Animation[] animations;

        public BoneData[] bonesData{ get ; private set;}

        public SkeletonInstancingData(SkeletonInstancingDataAsset dataAsset)
        {
            InitAnimationData(dataAsset.animationDataAsset.bytes);
            sharedMaterial = dataAsset.sharedMaterial;
            sharedMesh = dataAsset.sharedMesh;
            bonesData = dataAsset.bonesData;
            sharedMaterial.SetTexture(BONE_TEX, boneTexture);
            sharedMaterial.enableInstancing = true;
            sharedMaterial.DisableKeyword("_VERTEX_COLOR_ANIM");
            sharedMaterial.DisableKeyword("_UV_ANIM");
            if (hasVertexColorAnim)
            {
                sharedMaterial.SetTexture(VERTEX_COLOR_TEX, vertexColorTexture);
                sharedMaterial.EnableKeyword("_VERTEX_COLOR_ANIM");
            }

            if (hasUVAnim)
            {
                sharedMaterial.SetTexture(UV_TEX, uvTexture);
                sharedMaterial.EnableKeyword("_UV_ANIM");
            }
        }

        private void InitAnimationData(byte[] buffer)
        {
            BinaryReader reader = new BinaryReader(new MemoryStream(buffer));
            var animationCount = reader.ReadInt32();
            animations = new Animation[animationCount];
            for (int i = 0; i < animationCount; i++)
            {
                var animName = reader.ReadString();
                var frameOffset = reader.ReadInt32();
                var frameCount = reader.ReadInt32();
                var fps = reader.ReadInt32();
                var animation = new Animation(animName,fps,frameOffset,frameCount);
                animations[i] = animation;
            }

            var readTextureWidth = reader.ReadInt32();
            var readTextureHeight = reader.ReadInt32();
            var byteLength = reader.ReadInt32();
            var b = reader.ReadBytes(byteLength);
            boneTexture = new Texture2D(readTextureWidth, readTextureHeight, TextureFormat.RGBAHalf, false);
            boneTexture.LoadRawTextureData(b);
            boneTexture.filterMode = FilterMode.Point;
            boneTexture.Apply();

            hasVertexColorAnim = reader.ReadBoolean();
            if (hasVertexColorAnim)
            {
                readTextureWidth = reader.ReadInt32();
                readTextureHeight = reader.ReadInt32();
                byteLength = reader.ReadInt32();
                b = reader.ReadBytes(byteLength);
                vertexColorTexture = new Texture2D(readTextureWidth, readTextureHeight, TextureFormat.ASTC_6x6, false);
                vertexColorTexture.LoadRawTextureData(b);
                vertexColorTexture.filterMode = FilterMode.Point;
                vertexColorTexture.Apply();
            }

            hasUVAnim = reader.ReadBoolean();
            if (hasUVAnim)
            {
                readTextureWidth = reader.ReadInt32();
                readTextureHeight = reader.ReadInt32();
                byteLength = reader.ReadInt32();
                b = reader.ReadBytes(byteLength);
                uvTexture = new Texture2D(readTextureWidth, readTextureHeight, TextureFormat.RGBAHalf, false);
                uvTexture.LoadRawTextureData(b);
                uvTexture.filterMode = FilterMode.Point;
                uvTexture.Apply();
            }
            reader.Dispose();
        }

        public Animation FindAnimation(string name)
        {
            foreach (var animation in animations) 
            {
                if (animation.name == name)
                    return animation;
            }
            Debug.LogError($"Can not find animation info with name:{name}");
            return default;
        }
        public int GetBoneCount()
        {
            if (bonesData == null)
            {
                Debug.LogError("There's no bone data.");
                return 0;
            }
            return bonesData.Length;
        }

        public BoneData GetBone(string boneName) 
        {
            if (bonesData == null) 
            {
                Debug.LogError("There's no bone data.");
                return default;
            }
            return bonesData.FirstOrDefault((b) => b.name == boneName);
        }

        public Vector3 GetBoneWorldPos(string boneName,int frame) 
        {
            var bone = GetBone(boneName);
            var boneTransform = GetBoneTransform(bone.index, frame);
            return boneTransform.GetPosition();
        }

        public Matrix4x4 GetBoneTransform(int boneIndex, int frame = 0)
        {
            if (bonesData == null)
            {
                Debug.LogError("There's no bone data.");
                return default;
            }

            if (frame == 0) 
            {
                return bonesData[boneIndex].bindPose;
            }

            if (boneTexture == null) 
            {
                Debug.LogError("There's no boneTexture data.");
                return default;
            }

            int x = frame * 2;
            int y = boneIndex;
            var c0 = boneTexture.GetPixel(x, y);
            var c1 = boneTexture.GetPixel(x + 1, y);
            var boneTransform = new Matrix4x4();
            boneTransform.SetColumn(0, c0);
            boneTransform.SetColumn(1, c1);
            boneTransform.SetColumn(2, new Vector4(0, 0, 1, 0));
            boneTransform.SetColumn(3, new Vector4(0, 0, 0, 1));

            var boneData = bonesData[boneIndex];
            return boneData.bindPose * boneTransform;
        }

        public float GetBoneLength(int boneIndex)
        {
            return bonesData[boneIndex].length;
        }
    }
}