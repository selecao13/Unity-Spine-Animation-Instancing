using Spine;
using Spine.Unity;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Spine.Instancing
{
    public class SkeletonInstancingBaker
    {

        static InstacingMeshGenerator s_instanceMeshGenerator = new InstacingMeshGenerator();

        struct SkeletonAnimationBakeData
        {
            public string animationName;
            public int frameCount;
            public int frameOffset;
            public int fps;
            public int attachmentCount;
            public int sequenceVertexCount;
            /// <summary>
            /// Index1:BoneIndex
            /// Index2:FrameIndex
            /// </summary>
            public Matrix4x4[,] boneMatrices;
            public Color32[,] vertexColors;
            public Vector4[,] uvs;
        }

        public static void Bake(SkeletonDataAsset skeletonDataAsset, ExposedList<Skin> skins, int fps, bool flipX, bool flipY , string outputPath = "")
        {
            if (skeletonDataAsset == null || skeletonDataAsset.GetSkeletonData(true) == null)
            {
                Debug.LogError("Could not export Spine Skeleton because SkeletonData Asset is null or invalid!");
                return;
            }

            if (outputPath == "")
            {
                outputPath = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(skeletonDataAsset)).Replace('\\', '/') + "/Baked";
                System.IO.Directory.CreateDirectory(outputPath);
            }
            SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(true);
            var skeletonName = skeletonDataAsset.skeletonJSON.name;
            foreach (Skin skin in skins)
            {
                Skeleton skeleton = new Skeleton(skeletonData){ScaleX = flipX ? -1:1,ScaleY = flipY ? -1:1 };
                skeleton.SetSkin(skin);

                var fileName = skeletonName + "." + skin.Name;
                var skinOutputPath = outputPath + "/" + fileName;

                var needBakeVertexColor = NeedBakeVertexColor(skeletonData);
                var needBakeUV = NeedBakeUV(skeletonData);
                var meshAsset = BakeMesh(out var bindPoses, skeleton, skin, needBakeVertexColor, needBakeUV, skinOutputPath + ".mesh");
                var matAsset = BakeMaterial(skin, skinOutputPath + ".mat");
                var animationDataAsset = BakeAnimation(skeleton, skin, bindPoses, fps, meshAsset.vertexCount, needBakeVertexColor, needBakeUV, skinOutputPath + "_AnimData.bytes");
                var bonesData = BakeBoneData(skeleton); ;
                var instancingDataAsset = ScriptableObject.CreateInstance<SkeletonInstancingDataAsset>();
                instancingDataAsset.sharedMesh = meshAsset;
                instancingDataAsset.sharedMaterial = matAsset;
                instancingDataAsset.animationDataAsset = animationDataAsset;
                instancingDataAsset.bonesData = bonesData;
                AssetDatabase.CreateAsset(instancingDataAsset, skinOutputPath + "_InstancingData.Asset");
                AssetDatabase.Refresh();

            }
        }

        static Material BakeMaterial(Skin skin, string outputPath)
        {
            Material instancingMat = new Material(Shader.Find("Spine/Skeleton-Instancing"));
            instancingMat.enableInstancing = true;
            foreach (var skinEntry in skin.Attachments)
            {
                if (skinEntry.Attachment is IHasTextureRegion)
                {
                    var attchment = skinEntry.Attachment as IHasTextureRegion;
                    if (attchment.Region == null)
                    {
                        continue;
                    }
                    var mat = (Material)((AtlasRegion)attchment.Region).page.rendererObject;
                    instancingMat.mainTexture = mat.mainTexture;
                    break;
                }
            }
            AssetDatabase.CreateAsset(instancingMat, outputPath);
            AssetDatabase.ImportAsset(outputPath);
            AssetDatabase.Refresh();
            var matAsset = AssetDatabase.LoadAssetAtPath<Material>(outputPath);
            return matAsset;
        }

        static BoneData[] BakeBoneData(Skeleton skeleton)
        {
            var result = new BoneData[skeleton.Bones.Count];
            skeleton.SetToSetupPose();
            skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                var bone = skeleton.Bones.Items[i];
                Matrix4x4 localToWorld = new Matrix4x4();
                localToWorld.SetColumn(0, new Vector4(bone.A, bone.B, 0, bone.WorldX));
                localToWorld.SetColumn(1, new Vector4(bone.C, bone.D, 0, bone.WorldY));
                localToWorld.SetColumn(2, new Vector4(0, 0, 1, 0));
                localToWorld.SetColumn(3, new Vector4(0, 0, 0, 1));

                result[i] = new BoneData()
                {
                    name = bone.Data.Name,
                    index = i,
                    bindPose = localToWorld,
                    length = bone.Data.Length,
                };
            }
            return result;
        }

        static Mesh BakeMesh(out Matrix4x4[] bindPoses, Skeleton skeleton, Skin skin, bool bakeColor, bool bakeSquence, string outputPath)
        {
            skeleton.SetToSetupPose();
            skeleton.UpdateWorldTransform(Skeleton.Physics.Update);

            var boneCount = skeleton.Bones.Count;
            bindPoses = new Matrix4x4[boneCount];
            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                var bone = skeleton.Bones.Items[i];
                Matrix4x4 localToWorld = new Matrix4x4();
                localToWorld.SetColumn(0, new Vector4(bone.A, bone.B, 0, bone.WorldX));
                localToWorld.SetColumn(1, new Vector4(bone.C, bone.D, 0, bone.WorldY));
                localToWorld.SetColumn(2, new Vector4(0, 0, 1, 0));
                localToWorld.SetColumn(3, new Vector4(0, 0, 0, 1));

                bindPoses[i] = localToWorld.inverse;
            }

            s_instanceMeshGenerator.Begin();
            s_instanceMeshGenerator.BuildMeshWithArrays(skeleton, skin, bakeColor, bakeSquence);

            Mesh bakeMesh = new Mesh();
            s_instanceMeshGenerator.FillMeshData(bakeMesh);
            AssetDatabase.CreateAsset(bakeMesh, outputPath);
            AssetDatabase.ImportAsset(outputPath);
            AssetDatabase.Refresh();

            var meshAsset = AssetDatabase.LoadAssetAtPath<Mesh>(outputPath);
            return meshAsset;
        }

        static TextAsset BakeAnimation(Skeleton skeleton, Skin skin, Matrix4x4[] bindPoses, int fps, int rawVertexCount, bool bakeVertexColor, bool bakeUV, string outputPath)
        {
            SkeletonAnimationBakeData[] bakeDatas = new SkeletonAnimationBakeData[skeleton.Data.Animations.Count];
            for (int i = 0; i < skeleton.Data.Animations.Count; i++)
            {
                var bakeData = BakeAnimation(skeleton, skin, skeleton.Data.Animations.Items[i], bindPoses, fps, rawVertexCount, bakeVertexColor, bakeUV);
                bakeDatas[i] = bakeData;
            }

            var boneAnimTex = BuildBoneAnimationTexture(bakeDatas, skeleton.Bones.Count);
            FileStream file = File.Open(outputPath, FileMode.Create);
            BinaryWriter writer = new BinaryWriter(file);
            writer.Write(bakeDatas.Length);
            foreach (var bakeData in bakeDatas)
            {
                writer.Write(bakeData.animationName);
                writer.Write(bakeData.frameOffset);
                writer.Write(bakeData.frameCount);
                writer.Write(bakeData.fps);
            }
            byte[] bytes = boneAnimTex.GetRawTextureData();
            writer.Write(boneAnimTex.width);
            writer.Write(boneAnimTex.height);
            writer.Write(bytes.Length);
            writer.Write(bytes);

            writer.Write(bakeVertexColor);

            if (bakeVertexColor)
            {
                var vertexColorAnimTex = BuildVertexColorAnimationTexture(bakeDatas);
                bytes = vertexColorAnimTex.GetRawTextureData();
                writer.Write(vertexColorAnimTex.width);
                writer.Write(vertexColorAnimTex.height);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }

            writer.Write(bakeUV);
            if (bakeUV)
            {
                var uvTex = BuildUVAnimationTexture(bakeDatas);
                bytes = uvTex.GetRawTextureData();
                writer.Write(uvTex.width);
                writer.Write(uvTex.height);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
            file.Close();
            AssetDatabase.ImportAsset(outputPath);
            AssetDatabase.Refresh();
            var animTextAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(outputPath);
            return animTextAsset;
        }

        static SkeletonAnimationBakeData BakeAnimation(Skeleton skeleton, Skin skin,
            Spine.Animation animation, Matrix4x4[] bindPoses, int fps, int rawVertexCount,
                bool bakeVertexColor = false, bool bakeUV = false)
        {
            animation.Apply(skeleton, 0, 0, false, null, 1f, MixBlend.Setup, MixDirection.In);
            skeleton.SetToSetupPose();
            float duration = animation.Duration;
            float bakeInscrement = 1.0f / fps;
            int steps = Mathf.CeilToInt(duration * fps);
            float currentTime = 0;
            int boneCount = skeleton.Bones.Count;
            SkeletonAnimationBakeData bakeData = new SkeletonAnimationBakeData();
            bakeData.fps = fps;
            bakeData.animationName = animation.Name;
            bakeData.frameCount = steps + 1;
            bakeData.boneMatrices = new Matrix4x4[boneCount, steps + 1];
            for (int i = 0; i <= steps; i++)
            {
                if (i > 0)
                {
                    currentTime += bakeInscrement;
                }
                if (i == steps)
                {
                    currentTime = duration;
                }
                animation.Apply(skeleton, 0, currentTime, false, null, 1f, MixBlend.Setup, MixDirection.In);
                skeleton.UpdateWorldTransform(Skeleton.Physics.Update);
                for (int j = 0; j < boneCount; j++)
                {
                    var bone = skeleton.Bones.Items[j];
                    Matrix4x4 transform = new Matrix4x4();
                    transform.SetColumn(0, new Vector4(bone.A, bone.B, 0, bone.WorldX));
                    transform.SetColumn(1, new Vector4(bone.C, bone.D, 0, bone.WorldY));
                    transform.SetColumn(2, new Vector4(0, 0, 1, 0));
                    transform.SetColumn(3, new Vector4(0, 0, 0, 1));
                    bakeData.boneMatrices[j, i] = bindPoses[j] * transform;
                }

                s_instanceMeshGenerator.Begin();
                s_instanceMeshGenerator.BuildMeshWithArrays(skeleton, skin, bakeVertexColor, bakeUV);

                if (bakeVertexColor)
                {
                    if (bakeData.vertexColors == null)
                    {
                        var attachmentCount = s_instanceMeshGenerator.GetBakedAttachmentCount();
                        bakeData.attachmentCount = attachmentCount;
                        bakeData.vertexColors = new Color32[attachmentCount, bakeData.frameCount];
                    }

                    var uv2Buffer = s_instanceMeshGenerator.GetUV2Buffer();
                    var colorBuffer = s_instanceMeshGenerator.GetVertextColorBuffer();

                    for (int v = 0; v < colorBuffer.Length; v++)
                    {
                        var vertexColorFetchIndex = (int)uv2Buffer[v].x;
                        bakeData.vertexColors[vertexColorFetchIndex, i] = colorBuffer[v];
                    }
                }

                if (bakeUV)
                {
                    if (bakeData.uvs == null)
                    {
                        var sequenceVertexCount = s_instanceMeshGenerator.GetBakedSqeuenceVertexCount();
                        bakeData.sequenceVertexCount = sequenceVertexCount;
                        bakeData.uvs = new Vector4[sequenceVertexCount, bakeData.frameCount];
                    }
                    var uv2Buffer = s_instanceMeshGenerator.GetUV2Buffer();
                    var uvBuffer = s_instanceMeshGenerator.GetUVBuffer();
                    var vertexBuffer = s_instanceMeshGenerator.GetVertexBuffer();
                    for (int v = 0; v < uvBuffer.Length; v++)
                    {
                        var sequenceIndex = (int)uv2Buffer[v].y;
                        if (sequenceIndex > 0)
                        {
                            ref var bakeUVData = ref bakeData.uvs[sequenceIndex, i];
                            var uv = uvBuffer[v];
                            var vertex = vertexBuffer[v];
                            bakeUVData.x = uv.x;
                            bakeUVData.y = uv.y;
                            //sequece will cause offset change
                            bakeUVData.z = vertex.x;
                            bakeUVData.w = vertex.y;
                        }
                    }
                }
            }
            return bakeData;
        }

        static Texture2D BuildBoneAnimationTexture(SkeletonAnimationBakeData[] bakeDatas, int bonesCount)
        {
            var boneTexHeight = bonesCount;
            var boneTexWidth = 0;
            for (int i = 0; i < bakeDatas.Length; i++)
            {
                var bakeData = bakeDatas[i];
                boneTexWidth += bakeData.frameCount * 2;
            }
            Texture2D boneTexture = new Texture2D(boneTexWidth, boneTexHeight, TextureFormat.RGBAHalf, false);
            Color[,] textureData = new Color[boneTexHeight, boneTexWidth];
            int frameOffset = 0;
            for (int i = 0; i < bakeDatas.Length; i++)
            {
                ref var bakeData = ref bakeDatas[i];
                bakeData.frameOffset += frameOffset;
                for (int j = 0; j < boneTexHeight; j++)
                {
                    for (int k = 0, frameDataOffset = frameOffset * 2; k < bakeData.frameCount; k++)
                    {
                        var boneMatrix = bakeData.boneMatrices[j, k];
                        textureData[j, frameDataOffset++] = boneMatrix.GetColumn(0);
                        textureData[j, frameDataOffset++] = boneMatrix.GetColumn(1);
                    }
                }
                frameOffset += bakeData.frameCount;
            }
            for (int i = 0; i < boneTexWidth; i++)
            {
                for (int j = 0; j < boneTexHeight; j++)
                {
                    boneTexture.SetPixel(i, j, textureData[j, i]);
                }
            }
            boneTexture.Apply();
            return boneTexture;
        }


        static Texture2D BuildVertexColorAnimationTexture(SkeletonAnimationBakeData[] bakeDatas)
        {
            var texHeight = 0;
            var texWidth = 0;
            for (int i = 0; i < bakeDatas.Length; i++)
            {
                var bakeData = bakeDatas[i];
                texWidth += bakeData.frameCount;
                texHeight = bakeData.attachmentCount;
            }
            Texture2D colorTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBA32, false);
            Color32[,] textureData = new Color32[texWidth, texHeight];
            int frameOffset = 0;
            for (int i = 0; i < bakeDatas.Length; i++)
            {
                var bakeData = bakeDatas[i];
                for (int j = 0; j < texHeight; j++)
                {
                    for (int k = 0, offset = frameOffset; k < bakeData.frameCount; k++, offset++)
                    {
                        try
                        {
                            textureData[offset, j] = bakeData.vertexColors[j, k];  // j = 23  k = 0
                        }
                        catch (System.Exception e)
                        {
                            Debug.Log($"texWidth:{texWidth} texHeight:{texHeight} offset:{offset} j:{j} k:{k}");
                            throw e;
                        }
                    }
                }
                frameOffset += bakeData.frameCount;
            };
            for (int w = 0; w < texWidth; w++)
            {
                for (int h = 0; h < texHeight; h++)
                {
                    colorTexture.SetPixel(w, h, textureData[w, h]);
                }
            }
            colorTexture.Apply();
            EditorUtility.CompressTexture(colorTexture, TextureFormat.ASTC_6x6, TextureCompressionQuality.Best);
            return colorTexture;
        }

        static Texture2D BuildUVAnimationTexture(SkeletonAnimationBakeData[] bakeDatas)
        {
            var texHeight = 0;
            var texWidth = 0;
            for (int i = 0; i < bakeDatas.Length; i++)
            {
                var bakeData = bakeDatas[i];
                texWidth += bakeData.frameCount;
                texHeight = bakeData.sequenceVertexCount;
            }
            Texture2D uvTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGBAHalf, false);
            Color[,] textureData = new Color[texWidth, texHeight];
            int frameOffset = 0;
            for (int i = 0; i < bakeDatas.Length; i++)
            {
                var bakeData = bakeDatas[i];
                for (int j = 0; j < texHeight; j++)
                {
                    for (int k = 0, offset = frameOffset; k < bakeData.frameCount; k++, offset++)
                    {
                        textureData[offset, j] = bakeData.uvs[j, k];
                    }
                }
                frameOffset += bakeData.frameCount;
            };
            for (int w = 0; w < texWidth; w++)
            {
                for (int h = 0; h < texHeight; h++)
                {
                    uvTexture.SetPixel(w, h, textureData[w, h]);
                }
            }
            uvTexture.Apply();
            return uvTexture;
        }

        static bool NeedBakeVertexColor(SkeletonData skeletonData)
        {
            foreach (var animation in skeletonData.Animations)
            {
                foreach (var timeline in animation.Timelines)
                {
                    if (timeline is AttachmentTimeline || timeline is RGBTimeline ||
                        timeline is RGBATimeline || timeline is RGB2Timeline || timeline is RGBA2Timeline ||
                        timeline is AlphaTimeline)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        static bool NeedBakeUV(SkeletonData skeletonData)
        {
            foreach (var animation in skeletonData.Animations)
            {
                foreach (var timeline in animation.Timelines)
                {
                    if (timeline is SequenceTimeline)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
