Shader "WaterAllInOne/GpuFFTOcean/HSDTable"
{
	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "GpuFFTCommon.cginc"

			uniform sampler2D _WTable;
	        uniform sampler2D _H0Table;
			uniform float _GTime;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			struct MRTs {
				float4 heightBuffer : COLOR0;
				float4 slopeBuffer : COLOR1;
				float4 displacementBuffer : COLOR2;
			};

			float2 H(float m, float n, float2 uv, float t) {
				float4 h0Table = tex2D(_H0Table, uv);
				float4 wTable = tex2D(_WTable, uv);

				float2 h0 = h0Table.xy;
				float2 h0Conj = h0Table.zw;

				float omegat = DecodeFloatRGBA(wTable) * t;
				float _cos = cos(omegat);
				float _sin = sin(omegat);
				float2 exp0 = float2(_cos, _sin);
				float2 exp1 = float2(_cos, -_sin);

				return ComplexMul(h0, exp0) + ComplexMul(h0Conj, exp1);
			}

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				return o;
			}

			MRTs frag(v2f i)
			{
				float2 mn = round(i.uv * N);
				float2 k = K(mn.x, mn.y);
				float kLength = length(k);
				float2 h = H(mn.x, mn.y, i.uv, _GTime);
				float2 factor = float2(-h.y, h.x);

				float2 xSlope = k.x * factor;
				float2 zSlope = k.y * factor;

				float2 xDisplacement = (kLength < 0.000001f) ? 0.f : (-xSlope / kLength);
				float2 zDisplacement = (kLength < 0.000001f) ? 0.f : (-zSlope / kLength);

				MRTs o;

				o.heightBuffer = float4(h,0,0);
				o.slopeBuffer = float4(xSlope, zSlope);
				o.displacementBuffer = float4(xDisplacement, zDisplacement);

				return o;
			}
			ENDCG
		}
	}
}
