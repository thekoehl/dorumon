Shader "Bunny/Terrain"
{
	Properties 
	{
_Color("_Color", Color) = (1,1,1,1)
_MainTex("_MainTex", 2D) = "black" {}
_RockTiling("_RockTiling", Float) = 0.1
_UpFacing("_UpFacing", 2D) = "white" {}
_GrassTiling("_GrassTiling", Float) = 0.1
_DownFacing("_DownFacing", 2D) = "gray" {}
_UpFacing_Detail("_UpFacing_Detail", 2D) = "black" {}
_Detail("_Detail", 2D) = "black" {}
_DetailTiling("_DetailTiling", Float) = 0.1
_LightColor("_LightColor", Color) = (1,1,1,1)
_Silhouette("_Silhouette", 2D) = "black" {}
_Silhouette_Movement("_Silhouette_Movement", Vector) = (0,0,0,0)

	}
	
	SubShader 
	{
		Tags
		{
"Queue"="Geometry"
"IgnoreProjector"="False"
"RenderType"="Opaque"

		}

		
Cull Off
ZWrite On
ZTest LEqual
ColorMask RGBA
Fog{
}


		CGPROGRAM
#pragma surface surf BlinnPhongEditor  noforwardadd approxview halfasview vertex:vert
#pragma target 3.0


float4 _Color;
sampler2D _MainTex;
float _RockTiling;
sampler2D _UpFacing;
float _GrassTiling;
sampler2D _DownFacing;
sampler2D _UpFacing_Detail;
sampler2D _Detail;
float _DetailTiling;
float4 _LightColor;
sampler2D _Silhouette;
float4 _Silhouette_Movement;

			struct EditorSurfaceOutput {
				half3 Albedo;
				half3 Normal;
				half3 Emission;
				half3 Gloss;
				half Specular;
				half Alpha;
				half4 Custom;
			};
			
			inline half4 LightingBlinnPhongEditor_PrePass (EditorSurfaceOutput s, half4 light)
			{
float4 Multiply0=float4( s.Albedo.x, s.Albedo.y, s.Albedo.z, 1.0 ) * light;
return Multiply0;

			}

			inline half4 LightingBlinnPhongEditor (EditorSurfaceOutput s, half3 lightDir, half3 viewDir, half atten)
			{
				half3 h = normalize (lightDir + viewDir);
				
				half diff = max (0, dot ( lightDir, s.Normal ));
				
				float nh = max (0, dot (s.Normal, h));
				float spec = pow (nh, s.Specular*128.0);
				
				half4 res;
				res.rgb = _LightColor0.rgb * diff;
				res.w = spec * Luminance (_LightColor0.rgb);
				res *= atten * 2.0;

				return LightingBlinnPhongEditor_PrePass( s, res );
			}
			
			struct Input {
				float3 worldPos;
float3 sWorldNormal;
float4 color : COLOR;
float2 uv_Silhouette;

			};

			void vert (inout appdata_full v, out Input o) {
float4 VertexOutputMaster0_0_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_1_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_2_NoInput = float4(0,0,0,0);
float4 VertexOutputMaster0_3_NoInput = float4(0,0,0,0);

o.sWorldNormal = mul((float3x3)_Object2World, SCALED_NORMAL);

			}
			

			void surf (Input IN, inout EditorSurfaceOutput o) {
				o.Normal = float3(0.0,0.0,1.0);
				o.Alpha = 1.0;
				o.Albedo = 0.0;
				o.Emission = 0.0;
				o.Gloss = 0.0;
				o.Specular = 0.0;
				o.Custom = 0.0;
				
float4 Multiply8=float4( IN.worldPos.x, IN.worldPos.y,IN.worldPos.z,1.0 ) * float4( 1,1,1,0);
float4 Multiply12=Multiply8 * _RockTiling.xxxx;
float4 Tex2D7=tex2D(_MainTex,Multiply12.xy);
float4 Swizzle0=float4(Multiply8.z, Multiply8.y, Multiply8.x, Multiply8.w);
float4 Multiply13=Swizzle0 * _RockTiling.xxxx;
float4 Tex2D0=tex2D(_MainTex,Multiply13.xy);
float4 Splat3=float4( IN.sWorldNormal.x, IN.sWorldNormal.y,IN.sWorldNormal.z,1.0 ).x;
float4 Saturate3=saturate(Splat3);
float4 Negative1= -Splat3; 
 float4 Saturate4=saturate(Negative1);
float4 Add5=Saturate3 + Saturate4;
float4 Lerp6=lerp(Tex2D7,Tex2D0,Add5);
float4 Swizzle1=float4(Multiply8.x, Multiply8.z, Multiply8.y, Multiply8.w);
float4 Multiply14=_GrassTiling.xxxx * Swizzle1;
float4 Tex2D1=tex2D(_UpFacing,Multiply14.xy);
float4 Tex2D3=tex2D(_UpFacing_Detail,Multiply14.xy);
float4 Invert1= float4(1.0, 1.0, 1.0, 1.0) - Tex2D1.aaaa;
float4 Splat1=IN.color.z;
float4 Invert0= float4(1.0, 1.0, 1.0, 1.0) - Splat1;
float4 Add2=Invert1 + Invert0;
float4 Saturate2=saturate(Add2);
float4 Multiply2=Saturate2 * Invert0;
float4 Lerp2=lerp(Tex2D1,Tex2D3,Multiply2);
float4 Splat4=float4( IN.sWorldNormal.x, IN.sWorldNormal.y,IN.sWorldNormal.z,1.0 ).y;
float4 Add0=Tex2D1.aaaa + Splat4;
float4 Multiply0=Add0 * Splat4;
float4 Add6=Multiply0 + Multiply0;
float4 Saturate5=saturate(Add6);
float4 Lerp0=lerp(Lerp6,Lerp2,Saturate5);
float4 Tex2D2=tex2D(_DownFacing,Multiply14.xy);
float4 Negative0= -Splat4; 
 float4 Add1=Tex2D2.aaaa + Negative0;
float4 Multiply1=Add1 * Negative0;
float4 Lerp1=lerp(Lerp0,Tex2D2,Multiply1);
float4 SplatAlpha1=_LightColor.w;
float4 Multiply7=SplatAlpha1 * float4( 10,10,10,10 );
float4 Multiply6=_LightColor * Multiply7;
float4 Multiply9=_Silhouette_Movement * _Time;
float4 Multiply10=float4( 1.6,1.6,1.6,1.6 ) * Multiply9;
float4 Add4=(IN.uv_Silhouette.xyxy) + Multiply10;
float4 Tex2D5=tex2D(_Silhouette,Add4.xy);
float4 Multiply11=(IN.uv_Silhouette.xyxy) * float4( -1,1,0,0);
float4 Add3=Multiply9 + Multiply11;
float4 Tex2D6=tex2D(_Silhouette,Add3.xy);
float4 Lerp7=lerp(Tex2D5.aaaa,Tex2D6.aaaa,Tex2D6.aaaa);
float4 Lerp5=lerp(Multiply6,float4( 0.0, 0.0, 0.0, 0.0 ),Lerp7);
float4 Splat2=IN.color.y;
float4 Lerp4=lerp(Lerp1,Lerp5,Splat2);
float4 Multiply17=_DetailTiling.xxxx * Multiply8;
float4 Tex2D4=tex2D(_Detail,Multiply17.xy);
float4 Multiply16=_DetailTiling.xxxx * Swizzle0;
float4 Tex2D8=tex2D(_Detail,Multiply16.xy);
float4 Lerp8=lerp(Tex2D4,Tex2D8,Add5);
float4 Multiply15=_DetailTiling.xxxx * Swizzle1;
float4 Tex2D9=tex2D(_Detail,Multiply15.xy);
float4 Saturate1=saturate(Splat4);
float4 Saturate0=saturate(Negative0);
float4 Add7=Saturate1 + Saturate0;
float4 Saturate6=saturate(Add7);
float4 Lerp9=lerp(Lerp8,Tex2D9,Saturate6);
float4 SplatAlpha0=IN.color.w;
float4 Multiply3=Tex2D9.aaaa * SplatAlpha0;
float4 Lerp3=lerp(Lerp4,Lerp9,Multiply3);
float4 Multiply4=Lerp3 * Lerp3;
float4 Master0_1_NoInput = float4(0,0,1,1);
float4 Master0_2_NoInput = float4(0,0,0,0);
float4 Master0_3_NoInput = float4(0,0,0,0);
float4 Master0_4_NoInput = float4(0,0,0,0);
float4 Master0_5_NoInput = float4(1,1,1,1);
float4 Master0_7_NoInput = float4(0,0,0,0);
float4 Master0_6_NoInput = float4(1,1,1,1);
o.Albedo = Multiply4;

				o.Normal = normalize(o.Normal);
			}
		ENDCG
	}
	Fallback "Diffuse"
}