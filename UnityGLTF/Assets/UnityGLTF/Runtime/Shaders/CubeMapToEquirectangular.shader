// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/CubemapToEquirectangular" {
  Properties {
		_MainTex ("Cubemap (RGB)", CUBE) = "" {}
	}

	Subshader {
		Pass {
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }

			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma fragmentoption ARB_precision_hint_fastest
				#include "UnityCG.cginc"

				struct v2f {
					float4 pos : POSITION;
					float2 uv : TEXCOORD0;
				};

				samplerCUBE _MainTex;

				#define PI 3.141592653589793
				#define HALFPI 1.57079632679

				v2f vert( appdata_img v )
				{
					v2f o;
					o.pos = UnityObjectToClipPos(v.vertex);
					float2 uv = v.texcoord.xy * 2 - 1;
					uv *= float2(PI, HALFPI);
					o.uv = uv;
					return o;
				}

				fixed4 frag(v2f i) : COLOR
				{
					float cosy = cos(i.uv.y);
					float3 normal = float3(0,0,0);
					normal.x = cos(i.uv.x) * cosy;
					normal.y = i.uv.y;
					normal.z = cos(i.uv.x - HALFPI) * cosy;
					return texCUBE(_MainTex, normal);
				}
			ENDCG
		}
	}
	Fallback Off
}
