#ifndef SPINE_GPU_SKIN_INCLUDED
#define SPINE_GPU_SKIN_INCLUDED 

struct SkinInput 
{
	float4 vertex : POSITION;
	float2 uv : TEXCOORD0;
	half4 vertexColor : COLOR;
	float4 boneWeights : BLENDWEIGHTS;
 #if SHADER_API_GLES || SHADER_API_GLES3
 	float4 boneIndices : BLENDINDICES;
 #else 
	uint4 boneIndices : BLENDINDICES;
 #endif
};

#define MAX_BONE 60

CBUFFER_START(UnityPerMaterial)		
	float4	_BoneToWorld[MAX_BONE * 2];  
	float4  _WorldToBone[MAX_BONE * 2];   
	half 	_Cutoff;
CBUFFER_END

float2 WorldToBone(uint boneIndex,float3 vertex)
{
	float2 local;
	float4 matrices1 = _WorldToBone[boneIndex * 2];
	float4 matrices2 = _WorldToBone[boneIndex * 2 + 1];
					
	float a = matrices1.x, b = matrices1.y, c = matrices1.z, d = matrices1.w;
    float worldX = matrices2.x,worldY = matrices2.y;
	float det = a * d - b * c;
	float x = vertex.x - worldX, y = vertex.y - worldY;
	local.x = (x * d - y * b) / det;
	local.y = (y * a - x * c) / det;
	return local;
}

float2 BoneToWorld(uint boneIndex,float2 vertexBone)
{
	float2 world;
	
	float4 matrices1 = _BoneToWorld[boneIndex * 2];
	float4 matrices2 = _BoneToWorld[boneIndex * 2 + 1];

	float a = matrices1.x, b = matrices1.y, c = matrices1.z, d = matrices1.w;
	float wsX = matrices2.x,wsY = matrices2.y;
	world.x = vertexBone.x * a + vertexBone.y * b + wsX;
    world.y = vertexBone.x * c + vertexBone.y * d + wsY;
	return world;
}

float2 SkinTransform(float3 vertex,uint boneIndex, float weight)
{
	float2 vertexBone = WorldToBone(boneIndex,vertex);
	return BoneToWorld(boneIndex,vertexBone) * weight;
}


float3 Skin (float3 vertex,uint4 boneIndices,float4 boneWeights)
{	
	float3 ws = float3(0.0f,0.0f,vertex.z);
	ws.xy += SkinTransform(vertex,boneIndices.x,boneWeights.x);
	ws.xy += SkinTransform(vertex,boneIndices.y,boneWeights.y);
	ws.xy += SkinTransform(vertex,boneIndices.z,boneWeights.z);
	ws.xy += SkinTransform(vertex,boneIndices.w,boneWeights.w);
	return ws;
}

#endif