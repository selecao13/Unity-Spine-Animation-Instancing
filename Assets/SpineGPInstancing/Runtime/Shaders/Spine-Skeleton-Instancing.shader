Shader "Spine/Skeleton-Instancing" {
	Properties{

		_Cutoff("Shadow alpha cutoff", Range(0,1)) = 0.1
		[NoScaleOffset] _MainTex("Main Texture", 2D) = "black" {}
		[Toggle(_STRAIGHT_ALPHA_INPUT)] _StraightAlphaInput("Straight Alpha Texture", Int) = 0
		[HideInInspector] _StencilRef("Stencil Reference", Float) = 1.0
		[HideInInspector][Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Float) = 8 // Set to Always as default

		// Outline properties are drawn via custom editor.
		[HideInInspector] _OutlineWidth("Outline Width", Range(0,8)) = 3.0
		[HideInInspector] _OutlineColor("Outline Color", Color) = (1,1,0,1)
		[HideInInspector] _OutlineReferenceTexWidth("Reference Texture Width", Int) = 1024
		[HideInInspector] _ThresholdEnd("Outline Threshold", Range(0,1)) = 0.25
		[HideInInspector] _OutlineSmoothness("Outline Smoothness", Range(0,1)) = 1.0
		[HideInInspector][MaterialToggle(_USE8NEIGHBOURHOOD_ON)] _Use8Neighbourhood("Sample 8 Neighbours", Float) = 1
		[HideInInspector] _OutlineOpaqueAlpha("Opaque Alpha", Range(0,1)) = 1.0
		[HideInInspector] _OutlineMipLevel("Outline Mip Level", Range(0,3)) = 0

		// 石化效果
		[HideInInspector][Toggle(_USE_GRAY)]_IfGray("Enable Gray",float) = 0
		[HideInInspector][Toggle(_USE_STONE)]_IfStone("Enable Stone",float) = 0
		[HideInInspector]_StoneTex("Stone Texture",2D) = "black"{}
		[HideInInspector]_StoneFactor("Stone Factor",Range(0,1)) = 0
		[HideInInspector]_StoneUVScale("UV Scale",Range(0,1)) = 0.2
		[HideInInspector]_GrayBlend("Gray Blend",Range(0,1)) = 0
		[HideInInspector]_Progress("Progress",Range(0,1)) = 0
		[HideInInspector]_Gray("Gray",Range(0,1)) = 0
	}

		SubShader{
			Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" "PreviewType" = "Plane" }

			Fog { Mode Off }
			Cull Off
			ZWrite Off
			Blend One OneMinusSrcAlpha
			Lighting Off

			Stencil {
				Ref[_StencilRef]
				Comp[_StencilComp]
				Pass Keep
			}

			Pass {
				Name "Normal"

				HLSLPROGRAM
				#define USE_URP

				#pragma multi_compile_instancings
				#pragma shader_feature _ _STRAIGHT_ALPHA_INPUT
				#pragma shader_feature _ _USE_STONE
				#pragma shader_feature _ _USE_GRAY
				#pragma shader_feature _ _UV_ANIM
				#pragma shader_feature _ _VERTEX_COLOR_ANIM 
				#pragma shader_feature _ _ONE_BONE_WEIGHT 
				#pragma vertex vert
				#pragma fragment frag
				#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
				#include "Packages/com.esotericsoftware.spine.spine-unity/Runtime/spine-unity/Shaders/CGIncludes/Spine-Common.cginc"
			
				TEXTURE2D(_MainTex);            
				SAMPLER(sampler_MainTex);
				
				TEXTURE2D(_BoneTex);
				SAMPLER(sampler_BoneTex);

				TEXTURE2D(_UVTex);
				SAMPLER(sampler_UVTex);

				TEXTURE2D(_VertColorTex);
				SAMPLER(sampler_VertColorTex);
			
				//石化
				TEXTURE2D(_StoneTex);
				SAMPLER(sampler_StoneTex);
								
				UNITY_INSTANCING_BUFFER_START(Props)
				UNITY_DEFINE_INSTANCED_PROP(half,StoneFactor)
				UNITY_DEFINE_INSTANCED_PROP(half,StoneUVScale)
				UNITY_DEFINE_INSTANCED_PROP(half,GrayBlend)
				UNITY_DEFINE_INSTANCED_PROP(half,Progress)
				UNITY_DEFINE_INSTANCED_PROP(half,Gray)
				
				UNITY_DEFINE_INSTANCED_PROP(half,FrameIndex)
				UNITY_DEFINE_INSTANCED_PROP(half,TransitionProgress)
				UNITY_INSTANCING_BUFFER_END(Props)

				#include "Include/Spine-Instancing.hlsl"
				
				struct VertexOutput 
				{
					float4 	pos 				: SV_POSITION;			
					float2 	uv 					: TEXCOORD0;
					float3 	worldPosition 		: TEXCOORD1;
					half4  	vertexColor 		: COLOR;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				VertexOutput vert(InstancingInput v)
				{
					VertexOutput o;

 					UNITY_SETUP_INSTANCE_ID(v);
                	UNITY_TRANSFER_INSTANCE_ID(v, o);

					half4 localPos = skinning(v);
			
				#if defined(_UV_ANIM)
					half4 uvData = GetUV(v.uv2.y);
					if(v.uv2.y > 0)
					{
						half4 uvData = GetUV(v.uv2.y);
						o.uv = uvData.xy;
						localPos.xy = uvData.zw;
					}
					else
					{
						o.uv = v.uv;
					}
				#else 
					o.uv = v.uv;
				#endif
					o.pos = TransformObjectToHClip(localPos.xyz);//TransformObjectToHClip(v.vertex);//TransformObjectToHClip(localPos.xyz);//mul(UNITY_MATRIX_VP,float4(ws,1.0f)); 
					o.worldPosition =  TransformObjectToWorld(localPos.xyz);
				#if defined(_VERTEX_COLOR_ANIM) 
					o.vertexColor = PMAGammaToTargetSpace(GetVertColor(v.uv2.x));
					//Culling 
					if(o.vertexColor.r + o.vertexColor.g + o.vertexColor.b + o.vertexColor.a == 0)
					{
						 o.pos = half4(10000, 10000, 10000, 1);
					}
				#else
					o.vertexColor = v.vertexColor;
				#endif
					return o;
				}

				half4 frag(VertexOutput i) : SV_Target 
				{
					UNITY_SETUP_INSTANCE_ID(i);

					half _StoneFactor 	= UNITY_ACCESS_INSTANCED_PROP(Props, StoneFactor);
					half _StoneUVScale 	= UNITY_ACCESS_INSTANCED_PROP(Props, StoneUVScale);
					half _GrayBlend 	= UNITY_ACCESS_INSTANCED_PROP(Props, GrayBlend);
					half _Progress 		= UNITY_ACCESS_INSTANCED_PROP(Props, Progress);
					half _Gray 			= UNITY_ACCESS_INSTANCED_PROP(Props, Gray);

				  	half4 texColor = SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, i.uv);

				#if defined(_USE_GRAY)
					half3 gray = dot(texColor.rgb, half3(0.299, 0.587, 0.114));
					gray = lerp(texColor.rgb, gray, _Gray);//灰化
					texColor.rgb = gray;
				#endif

				#if defined(_USE_STONE)
					//石化
					half4 stoneColor = SAMPLE_TEXTURE2D(_StoneTex, sampler_StoneTex,i.worldPosition.xy * _StoneUVScale * 2);
					half3 grayColor = dot(texColor.rgb, half3(0.299, 0.587, 0.114));
					grayColor = lerp(texColor.rgb, grayColor, _GrayBlend);
					 //渐变
					half dist = distance(half2(0.5, 0.5), i.uv.xy);
					half range = 1 - saturate(dist * (1 - _Progress) * 500);
					range = smoothstep(0, 0.4, range);
					//石化颜色
					stoneColor.rgb = lerp(grayColor, stoneColor.rgb, _StoneFactor);
					texColor.rgb = lerp(texColor.rgb, stoneColor.rgb, range);
				#endif
				#if defined(_STRAIGHT_ALPHA_INPUT)
					texColor.rgb *= texColor.a;
				#endif
					return (texColor * i.vertexColor);
				}
				ENDHLSL
			}			
		}
		CustomEditor "SpineShaderWithOutlineGUI"
}
