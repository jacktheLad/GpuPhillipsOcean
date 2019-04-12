Shader "WaterAllInOne/GpuFFTOcean/FFT"
{
	CGINCLUDE
	#include "UnityCG.cginc"
	#include "GpuFFTCommon.cginc"

	uniform sampler2D _ButterFlyLookUp;
	uniform sampler2D _ReadBuffer;
	uniform float _V;

	struct appdata {
		float4 vertex : POSITION;
		float2 uv : TEXCOORD0;
	};

	struct v2f {
		float2 uv : TEXCOORD0;
		float4 vertex : SV_POSITION;
	};

	float2 Combine(float2 w, float2 input1, float2 input2) {
		return input1 + ComplexMul(w, input2);
	}

	v2f vert(appdata v) {
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv.xy;
		return o;
	}

	fixed4 frag_h(v2f i) : SV_Target
	{
		float4 data = tex2D(_ButterFlyLookUp, float2(i.uv.y, _V));
		
		float2 uv1 = float2(i.uv.x, data.r);
		float2 uv2 = float2(i.uv.x, data.g);

		float4 c1 = tex2D(_ReadBuffer, uv1);
		float4 c2 = tex2D(_ReadBuffer, uv2);
		float2 w = data.ba;

		float4 o;
		o.xy = Combine(w, c1.xy, c2.xy);
		o.zw = Combine(w, c1.zw, c2.zw);
		return o;
	}

	fixed4 frag_v(v2f i) : SV_Target
	{
		float4 data = tex2D(_ButterFlyLookUp, float2(i.uv.x, _V));

		float2 uv1 = float2(data.r, i.uv.y);
		float2 uv2 = float2(data.g, i.uv.y);

		float4 c1 = tex2D(_ReadBuffer, uv1);
		float4 c2 = tex2D(_ReadBuffer, uv2);
		float2 w = data.ba;

		float4 o;
		o.xy = Combine(w, c1.xy, c2.xy);
		o.zw = Combine(w, c1.zw, c2.zw);
		return o;
	}
	ENDCG
	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_h
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off
			Fog { Mode off }
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_v
			ENDCG
		}
	}
}
