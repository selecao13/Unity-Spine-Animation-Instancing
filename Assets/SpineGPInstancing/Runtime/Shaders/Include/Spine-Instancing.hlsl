#ifndef SPINE_INSTANCING_INCLUDED
#define SPINE_INSTANCING_INCLUDED 

struct InstancingInput
{
    float4 vertex : POSITION;
	uint vertexID :SV_VertexID;
	float2 uv : TEXCOORD0;
	float2 uv2 : TEXCOORD1;
	half4 vertexColor : COLOR;
	float4 boneWeights : BLENDWEIGHTS;
 #if SHADER_API_GLES || SHADER_API_GLES3
 	float4 boneIndices : BLENDINDICES;
 #else 
	uint4 boneIndices : BLENDINDICES;
 #endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};


half4x4 loadMatFromTexture(uint frameIndex, uint boneIndex)
{   
    int2 uv;
    uv.y = boneIndex;
    uv.x = frameIndex * 2;
    half4 c1 = LOAD_TEXTURE2D(_BoneTex,uv);
    uv.x += 1;
    half4 c2 = LOAD_TEXTURE2D(_BoneTex,uv);
    half4 c3 = half4(0,0,1,0); //LOAD_TEXTURE2D(_BoneTex,uv);
    half4 c4 = half4(0,0,0,1);
    half4x4 m;
	m._11_21_31_41 = c1;
	m._12_22_32_42 = c2;
	m._13_23_33_43 = c3;
	m._14_24_34_44 = c4;
    return m;
}

half4 skinning(inout InstancingInput v)
{
	half4 w = v.boneWeights;
	half4 bone = half4(v.boneIndices.x, v.boneIndices.y, v.boneIndices.z, v.boneIndices.w);

	half curFrame = UNITY_ACCESS_INSTANCED_PROP(Props,FrameIndex);
	half progress = UNITY_ACCESS_INSTANCED_PROP(Props,TransitionProgress);
    half nextFrame = curFrame + 1;
	half4x4 localToWorldMatrixPre = loadMatFromTexture(curFrame, bone.x) * w.x;
	localToWorldMatrixPre += loadMatFromTexture(curFrame, bone.y) * max(0, w.y);
	localToWorldMatrixPre += loadMatFromTexture(curFrame, bone.z) * max(0, w.z);
	localToWorldMatrixPre += loadMatFromTexture(curFrame, bone.w) * max(0, w.w);

	half4x4 localToWorldMatrixNext = loadMatFromTexture(nextFrame, bone.x) * w.x;
	localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.y) * max(0, w.y);
	localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.z) * max(0, w.z);
	localToWorldMatrixNext += loadMatFromTexture(nextFrame, bone.w) * max(0, w.w);

    half4 localPosPre = mul(v.vertex,localToWorldMatrixPre);
    half4 localPosNext = mul(v.vertex,localToWorldMatrixNext);
    half4 localPos =  lerp(localPosPre,localPosNext,progress);
    return localPos;
}

half4 GetUV(uint vertexID)
{
	half curFrame = UNITY_ACCESS_INSTANCED_PROP(Props,FrameIndex);
	int2 texcoord;
	texcoord.x = curFrame;
	texcoord.y = vertexID;
	return LOAD_TEXTURE2D(_UVTex,texcoord);
}	

half4 GetVertColor(uint vertexID)
{
	half curFrame = UNITY_ACCESS_INSTANCED_PROP(Props,FrameIndex);
	int2 texcoord;
	texcoord.x = curFrame;
	texcoord.y = vertexID;
	return LOAD_TEXTURE2D(_VertColorTex,texcoord);
}
#endif