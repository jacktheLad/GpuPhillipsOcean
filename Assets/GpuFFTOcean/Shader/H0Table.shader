Shader "WaterAllInOne/GpuFFTOcean/H0Table"
{
	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "GpuFFTCommon.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag

			uniform float A;
			uniform float4 WindDir;

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float4  vertex : SV_POSITION;
				float2  uv : TEXCOORD0;
			};

			float Phillips(float m, float n) {
				float2 k = K(m, n);
				float kLength2 = dot(k,k);
				if (kLength2 < 0.0000000000001f)
					return 0;

				k = normalize(k);
				float kLength4 = kLength2 * kLength2;
				float KdotW = dot(k, WindDir.xy);
				float KdotW2 = KdotW * KdotW;

				float L = dot(WindDir.xy, WindDir.xy) / G;
				float L2 = L * L;

				float l2 = L2 * 0.00001f;
				float scale = exp(-kLength2 * l2);

				return A * KdotW2 * exp(-1.0f / (kLength2 * L2)) / kLength4 * scale;
			}

			float2 hash2(float2 p) {
				return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3))))*43758.5453);
			}

			float2 GaussianRandomV2(float2 uv) {
				float2 rng = hash2(uv);
				// log(0)=infinity.耽误了一天时间查找这个错误
				rng.x = max(rng.x, 0.000001);

				float t = sqrt(-2.f * log(rng.x));
				float theta = 2.f * PI * rng.y;
				float z0 = t * cos(theta);
				float z1 = t * sin(theta);

				return float2(z0, z1);
			}

			float2 H0(float m, float n, float2 uv) {
				float2 r = GaussianRandomV2(uv);
				return r * sqrt(Phillips(m, n) * 0.5f);
			}

			v2f vert(appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				return o;
			}

			float4 frag(v2f i) : SV_Target
			{
				float2 uv = i.uv;
				float2 mn = round(uv*(N + 1));

				float2 h0 = H0(mn.x, mn.y, uv);
				float2 h0Conj = H0(-mn.x, -mn.y, uv);

				//return float4(Phillips(mn.x,mn.y), 0, 0, 1);
				//return float4( h0Conj.y,h0,h0Conj.x);
				return float4(h0, h0Conj);
			}

			ENDCG
		}
	}
}
