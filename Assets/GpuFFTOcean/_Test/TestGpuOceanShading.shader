Shader "WaterAllInOne/GpuFFTOcean/GpuOcean"
{
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float3 normal : TEXCOORD2;
				float4 vertex : SV_POSITION;
			};

			uniform sampler2D _Heightmap;
			uniform sampler2D _Slopemap;
			uniform sampler2D _Displacementmap;
			uniform float _MeshSize;

			v2f vert (appdata v)
			{
				v2f o;

				int2 vertCoor = int2(floor(v.uv * _MeshSize));
				int sign = ((vertCoor.x + vertCoor.y) & 1) * 2 - 1;
				float2 uuvv = v.uv * _MeshSize / (_MeshSize - 1);
				if (uuvv.x > 1.0) uuvv.x = 0.0;
				if (uuvv.y > 1.0) uuvv.y = 0.0;
				float3 offset = 0.0;
				offset.y = tex2Dlod(_Heightmap, float4(uuvv, 0, 0)).x * sign;
				offset.xz = tex2Dlod(_Displacementmap, float4(uuvv, 0, 0)).xz * sign;
				v.vertex.xz -= offset.xz;
				v.vertex.y += offset.y;

				float4 normalData = tex2Dlod(_Slopemap, float4(uuvv, 0, 0));
				o.normal = float3(-normalData.x*sign, 1.0, -normalData.z*sign);
				o.normal = UnityObjectToWorldNormal(o.normal);
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = 1.0;

				col.xyz *= dot(normalize(i.normal), _WorldSpaceLightPos0.xyz);

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
