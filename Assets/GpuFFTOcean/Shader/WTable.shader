Shader "WaterAllInOne/GpuFFTOcean/WTable"
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

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			float Dispersion(float m, float n) {
				float w0 = 2.f * PI / 200.f;
				float2 k = K(m, n);
				float wk = sqrt(G * length(k));
				return floor(wk / w0) * w0;
			}
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv.xy;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float2 mn = round(i.uv * (N + 1));
				return EncodeFloatRGBA(Dispersion(mn.x, mn.y));
			}
			ENDCG
		}
	}
}
