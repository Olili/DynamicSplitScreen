// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/StencilRenderer" {
Properties {
}
SubShader {
	Pass {
	Tags
	{
		"RenderType" = "Opaque"
		"Queue" = "Geometry"
	}
	Stencil{
		Ref 0
		Comp equal
		Pass Keep
		}
	//ZTest Always Cull Off ZWrite Off Fog{ Mode off } //Parametrage du shader pour éviter de lire, écrire dans le zbuffer, désactiver le culling et le brouillard sur le polygone
		ZTest Always Cull Off  Fog { Mode off } //Parametrage du shader pour éviter de lire, écrire dans le zbuffer, désactiver le culling et le brouillard sur le polygone

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			sampler2D MyCameraRd;
			//sampler2D _MainTex;

			struct Prog2Vertex {
	            float4 vertex : POSITION; 	//Les "registres" précisés après chaque variable servent
	            float4 tangent : TANGENT; 	//A savoir ce qu'on est censé attendre de la carte graphique.
	            float3 normal : NORMAL;		//(ce n'est pas basé sur le nom des variables).
	            float4 texcoord : TEXCOORD0;  
	            float4 texcoord1 : TEXCOORD1; 
	            fixed4 color : COLOR; 
        	 };
			 
			//Structure servant a transporter des données du vertex shader au pixel shader.
			//C'est au vertex shader de remplir a la main les informations de cette structure.
			struct Vertex2Pixel
			 {
           	 float4 pos : SV_POSITION;
           	 float4 uv : TEXCOORD0;

			 };  	 

			Vertex2Pixel vert (Prog2Vertex i)
			{
				Vertex2Pixel o;
				o.pos = i.vertex; //Projection du modèle 3D, cette ligne est obligatoire
				o.pos.xy *= 2;
		        o.uv=i.texcoord ; //UV de la texture
				o.uv.y = -o.uv.y + 1;
		      	
		      	return o;
			}

			float4 frag(Vertex2Pixel i) : COLOR
			{
				//return tex2D(MyCameraRd,i.uv.xy);
				return tex2D(MyCameraRd,i.uv.xy);
				//return float4(i.uv.x,1, i.uv.y, 1);
				//return float4(0,0,0,1);
				//return float4(1,0,0,1);
            }
ENDCG 
	}
}

Fallback off

}