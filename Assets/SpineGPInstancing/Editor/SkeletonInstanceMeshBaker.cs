#define SPINE_TRIANGLECHECK

using UnityEngine;
using Spine.Unity;
using Spine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Spine.Instancing
{
    public class InstacingMeshGenerator
    {
        [System.Serializable]
        public struct Settings
        {
            //public bool useClipping;
            [Space]
            [Range(-0.1f, 0f)] public float zSpacing;
            [Space]
            [Header("Vertex Data")]
            public bool pmaVertexColors;
            public bool tintBlack;
            [Tooltip("Enable when using Additive blend mode at SkeletonGraphic under a CanvasGroup. " +
                "When enabled, Additive alpha value is stored at uv2.g instead of color.a to capture CanvasGroup modifying color.a.")]
            public bool canvasGroupTintBlack;
            public bool calculateTangents;
            public bool addNormals;
            public bool immutableTriangles;

            static public Settings Default
            {
                get
                {
                    return new Settings
                    {
                        pmaVertexColors = true,
                        zSpacing = 0f,

                        tintBlack = false,
                        calculateTangents = false,
                        //renderMeshes = true,
                        addNormals = false,
                        immutableTriangles = false
                    };
                }
            }
        }


        const float BoundsMinDefault = float.PositiveInfinity;
        const float BoundsMaxDefault = float.NegativeInfinity;
        public Settings settings = Settings.Default;

        readonly List<Vector3> vertexBuffer = new List<Vector3>(4);
        readonly List<Vector2> uvBuffer = new List<Vector2>(4);
        readonly List<Color32> colorBuffer = new List<Color32>(4);
        readonly List<BoneWeight> boneWeights = new List<BoneWeight>(4);
        readonly List<int> triangelBuffer = new List<int>(6);

        /// <summary>
        /// x:attachmentIndex -> use to read VertextColor
        /// y:sequenceIndex -> use to read Sequence
        /// </summary>
        readonly List<Vector2> uv2Buffer = new List<Vector2>(4);

        Vector2 meshBoundsMin, meshBoundsMax;
        float meshBoundsThickness;

        float[] tempVerts = new float[8];
        BoneWeight[] tempWeights = new BoneWeight[8];

        Vector3[] normals;
        Vector4[] tangents;
        Vector2[] tempTanBuffer;
        ExposedList<Vector2> uv2;
        ExposedList<Vector2> uv3;

        int bakedAttachmentCount = 0;
        int bakedSequenceVertexCount = 0;
        public void Begin()
        {
            vertexBuffer.Clear();
            colorBuffer.Clear();
            uvBuffer.Clear();
            boneWeights.Clear();
            triangelBuffer.Clear();
            uv2Buffer.Clear();
            {
                meshBoundsMin.x = BoundsMinDefault;
                meshBoundsMin.y = BoundsMinDefault;
                meshBoundsMax.x = BoundsMaxDefault;
                meshBoundsMax.y = BoundsMaxDefault;
                meshBoundsThickness = 0f;
            }
            bakedAttachmentCount = 0;
            bakedSequenceVertexCount = 1;
        }

        public void BuildMeshWithArrays(Skeleton skeleton, Skin skin, bool bakeColor = false, bool bakeUV = false)
        {
            Settings settings = this.settings;
            bool canvasGroupTintBlack = settings.tintBlack && settings.canvasGroupTintBlack;

            // Populate Verts
            Color32 color = default(Color32);

            int vertexIndex = 0;
            float[] tempVerts = this.tempVerts;
            Vector2 bmin = this.meshBoundsMin;
            Vector2 bmax = this.meshBoundsMax;

            float a = skeleton.A, r = skeleton.R, g = skeleton.G, b = skeleton.B;

            for (int slotIndex = 0; slotIndex < skeleton.Slots.Count; slotIndex++)
            {
                Slot slot = skeleton.Slots.Items[slotIndex];

                List<Skin.SkinEntry> skinEntries = new List<Skin.SkinEntry>();
                skin.GetAttachments(slotIndex, skinEntries);
                if (skeleton.Data.DefaultSkin != null && skin != skeleton.Data.DefaultSkin)
                    skeleton.Data.DefaultSkin.GetAttachments(slotIndex, skinEntries);

                Sequence sequence = null;
                if (skinEntries.Count > 0 && skinEntries[0].Attachment is IHasTextureRegion)
                {
                    sequence = ((IHasTextureRegion)skinEntries[0].Attachment).Sequence;
                }

                bool hasSequence = sequence != null;
                var entriesCount = hasSequence? 1 : skinEntries.Count;

                for (int entryIndex = 0; entryIndex < entriesCount; entryIndex++)
                {
                    Attachment attachment = skinEntries[entryIndex].Attachment;

                    float z = slotIndex * settings.zSpacing;
                    RegionAttachment regionAttachment = attachment as RegionAttachment;
                    if (regionAttachment != null)
                    {
                        regionAttachment.ComputeWorldVertices(slot, tempVerts, 0);

                        float x1 = tempVerts[RegionAttachment.BLX], y1 = tempVerts[RegionAttachment.BLY];
                        float x2 = tempVerts[RegionAttachment.ULX], y2 = tempVerts[RegionAttachment.ULY];
                        float x3 = tempVerts[RegionAttachment.URX], y3 = tempVerts[RegionAttachment.URY];
                        float x4 = tempVerts[RegionAttachment.BRX], y4 = tempVerts[RegionAttachment.BRY];

                        vertexBuffer.Add(new Vector3(x1, y1, z));
                        vertexBuffer.Add(new Vector3(x4, y4, z));
                        vertexBuffer.Add(new Vector3(x2, y2, z));
                        vertexBuffer.Add(new Vector3(x3, y3, z));


                        //weight
                        for (int i = 0; i < 4; i++)
                        {
                            var bw = new BoneWeight();
                            bw.boneIndex0 = slot.Bone.Data.Index;
                            bw.weight0 = 1;
                            boneWeights.Add(bw);
                        }

                        if (settings.pmaVertexColors)
                        {
                            color.a = (byte)(a * slot.A * regionAttachment.A * 255);
                            color.r = (byte)(r * slot.R * regionAttachment.R * color.a);
                            color.g = (byte)(g * slot.G * regionAttachment.G * color.a);
                            color.b = (byte)(b * slot.B * regionAttachment.B * color.a);
                            if (slot.Data.BlendMode == BlendMode.Additive && !canvasGroupTintBlack) color.a = 0;
                        }
                        else
                        {
                            color.a = (byte)(a * slot.A * regionAttachment.A * 255);
                            color.r = (byte)(r * slot.R * regionAttachment.R * 255);
                            color.g = (byte)(g * slot.G * regionAttachment.G * 255);
                            color.b = (byte)(b * slot.B * regionAttachment.B * 255);
                        }

                        if (slot.Attachment != attachment)
                        {
                            color = Color.clear;
                        }

                        for (int i = 0; i < 4; i++)
                        {
                            colorBuffer.Add(color);
                        }

                        float[] regionUVs = regionAttachment.UVs;
                        Vector2 v1;
                        v1.x = regionUVs[RegionAttachment.BLX]; v1.y = regionUVs[RegionAttachment.BLY];
                        Vector2 v2;
                        v2.x = regionUVs[RegionAttachment.BRX]; v2.y = regionUVs[RegionAttachment.BRY];
                        Vector2 v3;
                        v3.x = regionUVs[RegionAttachment.ULX]; v3.y = regionUVs[RegionAttachment.ULY];
                        Vector2 v4;
                        v4.x = regionUVs[RegionAttachment.URX]; v4.y = regionUVs[RegionAttachment.URY];
                        uvBuffer.Add(v1);
                        uvBuffer.Add(v2);
                        uvBuffer.Add(v3);
                        uvBuffer.Add(v4);

                        //triangel
                        var attachmentFirstVertex = vertexBuffer.Count - 4;
                        triangelBuffer.Add(attachmentFirstVertex);
                        triangelBuffer.Add(attachmentFirstVertex + 2);
                        triangelBuffer.Add(attachmentFirstVertex + 1);
                        triangelBuffer.Add(attachmentFirstVertex + 2);
                        triangelBuffer.Add(attachmentFirstVertex + 3);
                        triangelBuffer.Add(attachmentFirstVertex + 1);


                        //UV2Buffer
                        if (bakeColor || bakeUV)
                        {
                            var vertextColorFetchIndex = bakedAttachmentCount;
                            uv2Buffer.Add(new Vector2(vertextColorFetchIndex, hasSequence ? bakedSequenceVertexCount++ : 0));
                            uv2Buffer.Add(new Vector2(vertextColorFetchIndex, hasSequence ? bakedSequenceVertexCount++ : 0));
                            uv2Buffer.Add(new Vector2(vertextColorFetchIndex, hasSequence ? bakedSequenceVertexCount++ : 0));
                            uv2Buffer.Add(new Vector2(vertextColorFetchIndex, hasSequence ? bakedSequenceVertexCount++ : 0));
                        }


                        if (x1 < bmin.x) bmin.x = x1; // Potential first attachment bounds initialization. Initial min should not block initial max. Same for Y below.
                        if (x1 > bmax.x) bmax.x = x1;
                        if (x2 < bmin.x) bmin.x = x2;
                        else if (x2 > bmax.x) bmax.x = x2;
                        if (x3 < bmin.x) bmin.x = x3;
                        else if (x3 > bmax.x) bmax.x = x3;
                        if (x4 < bmin.x) bmin.x = x4;
                        else if (x4 > bmax.x) bmax.x = x4;

                        if (y1 < bmin.y) bmin.y = y1;
                        if (y1 > bmax.y) bmax.y = y1;
                        if (y2 < bmin.y) bmin.y = y2;
                        else if (y2 > bmax.y) bmax.y = y2;
                        if (y3 < bmin.y) bmin.y = y3;
                        else if (y3 > bmax.y) bmax.y = y3;
                        if (y4 < bmin.y) bmin.y = y4;
                        else if (y4 > bmax.y) bmax.y = y4;

                        bakedAttachmentCount++;

                    }
                    else
                    { //if (settings.renderMeshes) {
                        MeshAttachment meshAttachment = attachment as MeshAttachment;
                        if (meshAttachment != null)
                        {
                            int verticesArrayLength = meshAttachment.WorldVerticesLength;
                            if (tempVerts.Length < verticesArrayLength) this.tempVerts = tempVerts = new float[verticesArrayLength];

                            meshAttachment.ComputeWorldVertices(slot, tempVerts);

                            if (settings.pmaVertexColors)
                            {
                                color.a = (byte)(a * slot.A * meshAttachment.A * 255);
                                color.r = (byte)(r * slot.R * meshAttachment.R * color.a);
                                color.g = (byte)(g * slot.G * meshAttachment.G * color.a);
                                color.b = (byte)(b * slot.B * meshAttachment.B * color.a);
                                if (slot.Data.BlendMode == BlendMode.Additive && !canvasGroupTintBlack) color.a = 0;
                            }
                            else
                            {
                                color.a = (byte)(a * slot.A * meshAttachment.A * 255);
                                color.r = (byte)(r * slot.R * meshAttachment.R * 255);
                                color.g = (byte)(g * slot.G * meshAttachment.G * 255);
                                color.b = (byte)(b * slot.B * meshAttachment.B * 255);
                            }

                            if (slot.Attachment != attachment)
                            {
                                color = Color.clear;
                            }

                            float[] attachmentUVs = meshAttachment.UVs;

                            // Potential first attachment bounds initialization. See conditions in RegionAttachment logic.
                            if (vertexIndex == 0)
                            {
                                // Initial min should not block initial max.
                                // vi == vertexIndex does not always mean the bounds are fresh. It could be a submesh. Do not nuke old values by omitting the check.
                                // Should know that this is the first attachment in the submesh. slotIndex == startSlot could be an empty slot.
                                float fx = tempVerts[0], fy = tempVerts[1];
                                if (fx < bmin.x) bmin.x = fx;
                                if (fx > bmax.x) bmax.x = fx;
                                if (fy < bmin.y) bmin.y = fy;
                                if (fy > bmax.y) bmax.y = fy;
                            }

                            var weights = BuildWeights(meshAttachment, slot);

                            for (int iii = 0; iii < verticesArrayLength; iii += 2)
                            {
                                float x = tempVerts[iii], y = tempVerts[iii + 1];
                                vertexBuffer.Add(new Vector3(x, y, z));
                                colorBuffer.Add(color);
                                uvBuffer.Add(new Vector2(attachmentUVs[iii], attachmentUVs[iii + 1]));
                                boneWeights.Add(weights[iii / 2]);

                                if (x < bmin.x) bmin.x = x;
                                else if (x > bmax.x) bmax.x = x;

                                if (y < bmin.y) bmin.y = y;
                                else if (y > bmax.y) bmax.y = y;
                            }

                            int attachmentFirstVertex = vertexBuffer.Count - verticesArrayLength / 2;
                            int[] attachmentTriangles = meshAttachment.Triangles;
                            for (int ii = 0, nn = attachmentTriangles.Length; ii < nn; ii++)
                                triangelBuffer.Add(attachmentFirstVertex + attachmentTriangles[ii]);

                            //UV2Buffer
                            if (bakeColor || bakeUV)
                            {
                                var vertextColorFetchIndex = bakedAttachmentCount;

                                for (int i = 0; i < verticesArrayLength; i += 2)
                                {
                                    uv2Buffer.Add(new Vector2(vertextColorFetchIndex, hasSequence ? bakedSequenceVertexCount++ : 0));
                                }
                            }

                            bakedAttachmentCount++;
                        }
                    }

                }
            }

            this.meshBoundsMin = bmin;
            this.meshBoundsMax = bmax;
        }

        private List<Vector2> weightCalCache = new List<Vector2>(8);
        BoneWeight[] BuildWeights(MeshAttachment meshAttachment, Slot slot)
        {
            int verticesArrayLength = meshAttachment.WorldVerticesLength;
            var tempWeights = this.tempWeights;
            if (tempWeights.Length < verticesArrayLength) this.tempWeights = tempWeights = new BoneWeight[verticesArrayLength];
            if (meshAttachment.Bones == null)
            {
                for (int i = 0; i < verticesArrayLength; i++)
                {
                    tempWeights[i] = new BoneWeight()
                    {
                        boneIndex0 = slot.Bone.Data.Index,
                        weight0 = 1,
                    };
                }
            }
            else
            {
                int[] bones = meshAttachment.Bones;
                float[] weights = meshAttachment.Vertices;
                for (int vIndex = 0, v = 0, b = 0, n = bones.Length; v < n; vIndex++)
                {
                    int bonesCount = bones[v++];
                    int nn = bonesCount + v;
                    var bw = new BoneWeight();
                    weightCalCache.Clear();
                    for (; v < nn; v++, b += 3)
                    {
                        weightCalCache.Add(new Vector2(bones[v], weights[b + 2]));
                    }
                    if (weightCalCache.Count > 4)
                    {
                        weightCalCache.Sort((a, b) => { return b.y.CompareTo(a.y); });
                    }
                    for (int i = 0; i < weightCalCache.Count; i++)
                    {
                        if (i > 3)
                        {
                            Debug.LogWarning("顶点受骨骼影响数超过4，unity最大支持4，超过部分忽略");
                        }
                        int index = (int)weightCalCache[i].x;
                        float weight = weightCalCache[i].y;
                        switch (i)
                        {
                            case 0:
                                bw.boneIndex0 = index;
                                bw.weight0 = weight;
                                break;
                            case 1:
                                bw.boneIndex1 = index;
                                bw.weight1 = weight;
                                break;
                            case 2:
                                bw.boneIndex2 = index;
                                bw.weight2 = weight;
                                break;
                            case 3:
                                bw.boneIndex3 = index;
                                bw.weight3 = weight;
                                break;
                        }
                    }

                    tempWeights[vIndex] = bw;
                }
            }
            return tempWeights;
        }

        public Bounds GetMeshBounds()
        {
            if (float.IsInfinity(meshBoundsMin.x))
            { // meshBoundsMin.x == BoundsMinDefault // == doesn't work on float Infinity constants.
                return new Bounds();
            }
            else
            {
                //mesh.bounds = ArraysMeshGenerator.ToBounds(meshBoundsMin, meshBoundsMax);
                float halfWidth = (meshBoundsMax.x - meshBoundsMin.x) * 0.5f;
                float halfHeight = (meshBoundsMax.y - meshBoundsMin.y) * 0.5f;
                return new Bounds
                {
                    center = new Vector3(meshBoundsMin.x + halfWidth, meshBoundsMin.y + halfHeight),
                    extents = new Vector3(halfWidth, halfHeight, meshBoundsThickness * 0.5f)
                };
            }
        }

        public void FillMeshData(Mesh mesh)
        {
            Vector3[] vbi = vertexBuffer.ToArray();
            Vector2[] ubi = uvBuffer.ToArray();
            Color32[] cbi = colorBuffer.ToArray();
            BoneWeight[] bwi = boneWeights.ToArray();
            Vector2[] ubi2 = uv2Buffer.ToArray();
            int vbiLength = vbi.Length;
            // Zero the extra.
            {
                int listCount = vertexBuffer.Count;
                Vector3 vector3zero = Vector3.zero;
                for (int i = listCount; i < vbiLength; i++)
                    vbi[i] = vector3zero;
            }

            // Set the vertex buffer.
            {
                mesh.vertices = vbi;
                mesh.uv = ubi;
                mesh.uv2 = ubi2;
                mesh.colors32 = cbi;
                mesh.boneWeights = bwi;
                mesh.bounds = GetMeshBounds();
            }

            {
                if (settings.addNormals)
                {
                    int oldLength = 0;

                    if (normals == null)
                        normals = new Vector3[vbiLength];
                    else
                        oldLength = normals.Length;

                    if (oldLength != vbiLength)
                    {
                        Array.Resize(ref this.normals, vbiLength);
                        Vector3[] localNormals = this.normals;
                        for (int i = oldLength; i < vbiLength; i++) localNormals[i] = Vector3.back;
                    }
                    mesh.normals = this.normals;
                }

                if (settings.tintBlack)
                {
                    if (uv2 != null)
                    {
                        // Sometimes, the vertex buffer becomes smaller. We need to trim the size of the tint black buffers to match.
                        if (vbiLength != uv2.Items.Length)
                        {
                            Array.Resize(ref uv2.Items, vbiLength);
                            Array.Resize(ref uv3.Items, vbiLength);
                            uv2.Count = uv3.Count = vbiLength;
                        }
                        mesh.uv2 = this.uv2.Items;
                        mesh.uv3 = this.uv3.Items;
                    }
                }
            }

            mesh.SetTriangles(triangelBuffer.ToArray(), 0);
        }

        public Color32[] GetVertextColorBuffer()
        {
            return colorBuffer.ToArray();
        }

        public Vector2[] GetUV2Buffer()
        {
            return uv2Buffer.ToArray();
        }

        public Vector2[] GetUVBuffer()
        {
            return uvBuffer.ToArray();
        }

        public Vector3[] GetVertexBuffer()
        {
            return vertexBuffer.ToArray();
        }

        public int GetBakedAttachmentCount()
        {
            return bakedAttachmentCount;
        }

        public int GetBakedSqeuenceVertexCount()
        {
            return bakedSequenceVertexCount;
        }
    }
}
