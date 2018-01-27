// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Stencils/StencilDrawer"
{
	Properties
	{
		_StencilMask("Stencil Mask", Int) = 0
	}
		SubShader
	{

		Tags
		{
			"RenderType" = "Opaque"
			"Queue" = "Geometry-100"
		}
		//ColorMask 0
		//ZWrite off
		Cull off
		/*Stencil
		{
			Ref[_StencilMask]
			Comp always
			Pass replace
		}*/

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			float4 color;
			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.pos = v.vertex;
				return o;
			}

			half4 frag(v2f i) : COLOR
			{
				return half4(color);
			}
			ENDCG
		}
	}
}