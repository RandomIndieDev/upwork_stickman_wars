// Toony Colors Pro+Mobile 2
// (c) 2014-2021 Jean Moreno

Shader "Toony Colors Pro 2/User/HairTrans"
{
	Properties
	{
		[Enum(Front, 2, Back, 1, Both, 0)] _Cull ("Render Face", Float) = 2.0
		[TCP2ToggleNoKeyword] _ZWrite ("Depth Write", Float) = 1.0
		[HideInInspector] _RenderingMode ("rendering mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("blending source", Float) = 1.0
		[HideInInspector] _DstBlend ("blending destination", Float) = 0.0
		[TCP2Separator]

		[TCP2HeaderHelp(Base)]
		_Color ("Color", Color) = (1,1,1,1)
		[TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
		[TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
		_MainTex ("Albedo", 2D) = "white" {}
		[TCP2Separator]

		[TCP2Header(Ramp Shading)]
		_RampThreshold ("Threshold", Range(0.01,1)) = 0.5
		_RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
		_LightWrapFactor ("Light Wrap Factor", Range(0,2)) = 0.5
		[TCP2Separator]
		
		[TCP2HeaderHelp(Specular)]
		[Toggle(TCP2_SPECULAR)] _UseSpecular ("Enable Specular", Float) = 0
		_AnisotropicTangentShift ("Tangent Shift Texture", 2D) = "gray" {}
		_AnisotropicShift1 ("Shift 1", Float) = 0.2
		_AnisotropicExponent1 ("Exponent 1", Float) = 4
		[TCP2ColorNoAlpha] _SpecularColor1 ("Specular Color 1", Color) = (0.3,0.3,0.3,1)
		[Space]
		_AnisotropicShift2 ("Shift 2", Float) = 0.5
		_AnisotropicExponent2 ("Exponent 2", Float) = 400
		[TCP2ColorNoAlpha] _SpecularColor2 ("Specular Color 2", Color) = (0.8,0.8,0.8,1)
		[Space]
		_SpecularShadowAttenuation ("Specular Shadow Attenuation", Float) = 0.25
		[TCP2Separator]
		
		[TCP2HeaderHelp(Rim Lighting)]
		[Toggle(TCP2_RIM_LIGHTING)] _UseRim ("Enable Rim Lighting", Float) = 0
		[TCP2ColorNoAlpha] _RimColor ("Rim Color", Color) = (0.8,0.8,0.8,0.5)
		_RimMin ("Rim Min", Range(0,2)) = 0.5
		_RimMax ("Rim Max", Range(0,2)) = 1
		//Rim Direction
		_RimDir ("Rim Direction", Vector) = (0,0,1,1)
		[TCP2Separator]
		[TCP2HeaderHelp(Ambient Lighting)]
		[Toggle(TCP2_AMBIENT)] _UseAmbient ("Enable Ambient/Indirect Diffuse", Float) = 0
		//AMBIENT CUBEMAP
		_AmbientCube ("Ambient Cubemap", Cube) = "_Skybox" {}
		[TCP2Separator]
		
		[Enum(ToonyColorsPro.ShaderGenerator.BlendOperation)] _blendOperation ("Blend Operation", Float) = 0

		// Avoid compile error if the properties are ending with a drawer
		[HideInInspector] __dummy__ ("unused", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
		}

		CGINCLUDE

		#include "UnityCG.cginc"
		#include "UnityLightingCommon.cginc"	// needed for LightColor

		// Texture/Sampler abstraction
		#define TCP2_TEX2D_WITH_SAMPLER(tex)						UNITY_DECLARE_TEX2D(tex)
		#define TCP2_TEX2D_NO_SAMPLER(tex)							UNITY_DECLARE_TEX2D_NOSAMPLER(tex)
		#define TCP2_TEX2D_SAMPLE(tex, samplertex, coord)			UNITY_SAMPLE_TEX2D_SAMPLER(tex, samplertex, coord)
		#define TCP2_TEX2D_SAMPLE_LOD(tex, samplertex, coord, lod)	UNITY_SAMPLE_TEX2D_SAMPLER_LOD(tex, samplertex, coord, lod)

		// Shader Properties
		TCP2_TEX2D_WITH_SAMPLER(_MainTex);
		TCP2_TEX2D_WITH_SAMPLER(_AnisotropicTangentShift);
		
		// Shader Properties
		float4 _MainTex_ST;
		fixed4 _Color;
		float _LightWrapFactor;
		float _RampThreshold;
		float _RampSmoothing;
		fixed4 _HColor;
		fixed4 _SColor;
		float4 _AnisotropicTangentShift_ST;
		float _AnisotropicShift1;
		float _AnisotropicShift2;
		float _AnisotropicExponent1;
		float _AnisotropicExponent2;
		fixed4 _SpecularColor1;
		fixed4 _SpecularColor2;
		float _SpecularShadowAttenuation;
		float4 _RimDir;
		float _RimMin;
		float _RimMax;
		fixed4 _RimColor;

		samplerCUBE _AmbientCube;

		//Specular help functions (from UnityStandardBRDF.cginc)
		inline half3 SpecSafeNormalize(half3 inVec)
		{
			half dp3 = max(0.001f, dot(inVec, inVec));
			return inVec * rsqrt(dp3);
		}
		// Hair Anisotropic Specular
		inline half HairStrandSpecular(float3 tangent, float3 viewDir, float3 lightDir, float exponent)
		{
			half3 halfDir = SpecSafeNormalize(lightDir + viewDir);
			half tdh = dot(tangent, halfDir);
			half sth = sqrt(1.0 - tdh * tdh);
			half dirAtten = smoothstep(-1.0, 0.0, tdh);
			return dirAtten * pow(sth, exponent);
		}
		inline float3 HairShiftTangent(float3 tangent, float3 normal, float shift)
		{
			return normalize(tangent + shift * normal);
		}

		ENDCG

		// Main Surface Shader
		Blend [_SrcBlend] [_DstBlend]
		Cull [_Cull]
		ZWrite [_ZWrite]
		BlendOp [_blendOperation]

		CGPROGRAM

		#pragma surface surf ToonyColorsCustom vertex:vertex_surface exclude_path:deferred exclude_path:prepass keepalpha nolightmap nolppv keepalpha
		#pragma target 3.0

		//================================================================
		// SHADER KEYWORDS

		#pragma shader_feature_local_fragment TCP2_SPECULAR
		#pragma shader_feature_local_fragment TCP2_RIM_LIGHTING
		#pragma shader_feature_local_fragment TCP2_AMBIENT
		#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON

		//================================================================
		// STRUCTS

		// Vertex input
		struct appdata_tcp2
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			float4 texcoord0 : TEXCOORD0;
			float4 texcoord1 : TEXCOORD1;
			float4 texcoord2 : TEXCOORD2;
			half4 tangent : TANGENT;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct Input
		{
			half3 viewDir;
			half3 tangent;
			float3 worldPos;
			half3 worldNormal; INTERNAL_DATA
			float2 texcoord0;
		};

		//================================================================

		// Custom SurfaceOutput
		struct SurfaceOutputCustom
		{
			half atten;
			half3 Albedo;
			half3 Normal;
			half3 worldNormal;
			half3 Emission;
			half Specular;
			half Gloss;
			half Alpha;
			half ndv;
			half ndvRaw;

			Input input;

			// Shader Properties
			float __lightWrapFactor;
			float __rampThreshold;
			float __rampSmoothing;
			float3 __highlightColor;
			float3 __shadowColor;
			float __ambientIntensity;
			float __anisotropicTangentShift;
			float __anisotropicShift1;
			float __anisotropicShift2;
			float __tangentShiftStrength1;
			float __tangentShiftStrength2;
			float __anisotropicExponent1;
			float __anisotropicExponent2;
			float3 __specularColor1;
			float3 __specularColor2;
			float __specularShadowAttenuation;
			float3 __rimDir;
			float __rimMin;
			float __rimMax;
			float3 __rimColor;
			float __rimStrength;
		};

		//================================================================
		// VERTEX FUNCTION

		void vertex_surface(inout appdata_tcp2 v, out Input output)
		{
			UNITY_INITIALIZE_OUTPUT(Input, output);

			// Texture Coordinates
			output.texcoord0.xy = v.texcoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;

			output.tangent = mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0)).xyz;

		}

		//================================================================
		// SURFACE FUNCTION

		void surf(Input input, inout SurfaceOutputCustom output)
		{
			// Shader Properties Sampling
			float4 __albedo = ( TCP2_TEX2D_SAMPLE(_MainTex, _MainTex, input.texcoord0.xy).rgba );
			float4 __mainColor = ( _Color.rgba );
			float __alpha = ( __albedo.a * __mainColor.a );
			output.__lightWrapFactor = ( _LightWrapFactor );
			output.__rampThreshold = ( _RampThreshold );
			output.__rampSmoothing = ( _RampSmoothing );
			output.__highlightColor = ( _HColor.rgb );
			output.__shadowColor = ( _SColor.rgb );
			output.__ambientIntensity = ( 1.0 );
			output.__anisotropicTangentShift = ( TCP2_TEX2D_SAMPLE(_AnisotropicTangentShift, _AnisotropicTangentShift, input.texcoord0.xy * _AnisotropicTangentShift_ST.xy + _AnisotropicTangentShift_ST.zw).r );
			output.__anisotropicShift1 = ( _AnisotropicShift1 );
			output.__anisotropicShift2 = ( _AnisotropicShift2 );
			output.__tangentShiftStrength1 = ( 1.0 );
			output.__tangentShiftStrength2 = ( 1.0 );
			output.__anisotropicExponent1 = ( _AnisotropicExponent1 );
			output.__anisotropicExponent2 = ( _AnisotropicExponent2 );
			output.__specularColor1 = ( _SpecularColor1.rgb );
			output.__specularColor2 = ( _SpecularColor2.rgb );
			output.__specularShadowAttenuation = ( _SpecularShadowAttenuation );
			output.__rimDir = ( _RimDir.xyz );
			output.__rimMin = ( _RimMin );
			output.__rimMax = ( _RimMax );
			output.__rimColor = ( _RimColor.rgb );
			output.__rimStrength = ( 1.0 );

			output.input = input;

			half3 worldNormal = WorldNormalVector(input, output.Normal);
			output.worldNormal = worldNormal;

			half ndv = abs(dot(input.viewDir, normalize(output.Normal.xyz)));
			half ndvRaw = ndv;
			output.ndv = ndv;
			output.ndvRaw = ndvRaw;

			output.Albedo = __albedo.rgb;
			output.Alpha = __alpha;

			output.Albedo *= __mainColor.rgb;

		}

		//================================================================
		// LIGHTING FUNCTION

		inline half4 LightingToonyColorsCustom(inout SurfaceOutputCustom surface, half3 viewDir, UnityGI gi)
		{

			half ndv = surface.ndv;
			half3 lightDir = gi.light.dir;
			#if defined(UNITY_PASS_FORWARDBASE)
				half3 lightColor = _LightColor0.rgb;
				half atten = surface.atten;
			#else
				// extract attenuation from point/spot lights
				half3 lightColor = _LightColor0.rgb;
				half atten = max(gi.light.color.r, max(gi.light.color.g, gi.light.color.b)) / max(_LightColor0.r, max(_LightColor0.g, _LightColor0.b));
			#endif

			half3 normal = normalize(surface.Normal);
			half ndl = dot(normal, lightDir);
			half3 ramp;
			
			// Wrapped Lighting
			half lightWrap = surface.__lightWrapFactor;
			ndl = (ndl + lightWrap) / (1 + lightWrap);
			
			#define		RAMP_THRESHOLD	surface.__rampThreshold
			#define		RAMP_SMOOTH		surface.__rampSmoothing
			ndl = saturate(ndl);
			ramp = smoothstep(RAMP_THRESHOLD - RAMP_SMOOTH*0.5, RAMP_THRESHOLD + RAMP_SMOOTH*0.5, ndl);

			// Apply attenuation (shadowmaps & point/spot lights attenuation)
			ramp *= atten;

			// Highlight/Shadow Colors
			#if !defined(UNITY_PASS_FORWARDBASE)
				ramp = lerp(half3(0,0,0), surface.__highlightColor, ramp);
			#else
				ramp = lerp(surface.__shadowColor, surface.__highlightColor, ramp);
			#endif

			// Output color
			half4 color;
			color.rgb = surface.Albedo * lightColor.rgb * ramp;
			color.a = surface.Alpha;

			// Apply indirect lighting (ambient)
			half occlusion = 1;
			#ifdef UNITY_LIGHT_FUNCTION_APPLY_INDIRECT
			#if defined(TCP2_AMBIENT)
				half3 ambient = gi.indirect.diffuse;
				
				//Ambient Cubemap
				ambient.rgb += texCUBE(_AmbientCube, normal);
				ambient *= surface.Albedo * occlusion * surface.__ambientIntensity;

				color.rgb += ambient;
			#endif
			#endif

			// Premultiply blending
			#if defined(_ALPHAPREMULTIPLY_ON)
				color.rgb *= color.a;
			#endif

			#if defined(TCP2_SPECULAR)
			// Hair Anisotropic Specular
			half tangentShift = surface.__anisotropicTangentShift;
			half shift1 = surface.__anisotropicShift1;
			half shift2 = surface.__anisotropicShift2;
			float3 anisoTangent1 = HairShiftTangent(surface.input.tangent, normal, shift1 + tangentShift * surface.__tangentShiftStrength1);
			float3 anisoTangent2 = HairShiftTangent(surface.input.tangent, normal, shift2 + tangentShift * surface.__tangentShiftStrength2);
			half exp1 = surface.__anisotropicExponent1;
			half exp2 = surface.__anisotropicExponent2;
			half spec1 = HairStrandSpecular(anisoTangent1, viewDir, lightDir, exp1);
			half spec2 = HairStrandSpecular(anisoTangent2, viewDir, lightDir, exp2);
			half3 specColor1 = spec1 * surface.__specularColor1;
			half3 specColor2 = spec2 * surface.__specularColor2;
			specColor1 *= ndl;
			specColor2 *= ndl;
			half specShadowAtten = saturate(atten * ndl + surface.__specularShadowAttenuation);
			specColor1 *= specShadowAtten;
			specColor2 *= specShadowAtten;
			
			//Apply specular
			color.rgb += specColor1 + specColor2 * lightColor.rgb;
			#endif
			// Rim Lighting
			#if defined(TCP2_RIM_LIGHTING)
			half3 rViewDir = viewDir;
			half3 rimDir = surface.__rimDir;
			rViewDir = normalize(UNITY_MATRIX_V[0].xyz * rimDir.x + UNITY_MATRIX_V[1].xyz * rimDir.y + UNITY_MATRIX_V[2].xyz * rimDir.z);
			half rim = 1.0f - saturate(dot(rViewDir, normal));
			rim = ( rim );
			half rimMin = surface.__rimMin;
			half rimMax = surface.__rimMax;
			rim = smoothstep(rimMin, rimMax, rim);
			half3 rimColor = surface.__rimColor;
			half rimStrength = surface.__rimStrength;
			//Rim light mask
			color.rgb += ndl * atten * rim * rimColor * rimStrength;
			#endif

			// Apply alpha to Forward Add passes
			#if defined(_ALPHABLEND_ON) && defined(UNITY_PASS_FORWARDADD)
				color.rgb *= color.a;
			#endif

			return color;
		}

		void LightingToonyColorsCustom_GI(inout SurfaceOutputCustom surface, UnityGIInput data, inout UnityGI gi)
		{
			half3 normal = surface.Normal;

			// GI without reflection probes
			gi = UnityGlobalIllumination(data, 1.0, normal); // occlusion is applied in the lighting function, if necessary

			surface.atten = data.atten; // transfer attenuation to lighting function
			gi.light.color = _LightColor0.rgb; // remove attenuation

		}

		ENDCG

	}

	Fallback "Diffuse"
	CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}

/* TCP_DATA u config(unity:"2020.3.7f1";ver:"2.8.1";tmplt:"SG2_Template_Default";features:list["UNITY_5_4","UNITY_5_5","UNITY_5_6","UNITY_2017_1","UNITY_2018_1","UNITY_2018_2","UNITY_2018_3","UNITY_2019_1","UNITY_2019_2","UNITY_2019_3","UNITY_2019_4","UNITY_2020_1","WRAPPED_LIGHTING_CUSTOM","SPECULAR","SPECULAR_NO_ATTEN","SPECULAR_SHADER_FEATURE","RIM","RIM_DIR","RIM_LIGHTMASK","RIM_SHADER_FEATURE","SS_SHADER_FEATURE","SUBSURFACE_AMB_COLOR","CUBE_AMBIENT","AMBIENT_SHADER_FEATURE","VERTICAL_FOG_ALPHA","ENABLE_FOG","VERTICAL_FOG_SHADER_FEATURE","SPECULAR_ANISOTROPIC_HAIR","AUTO_TRANSPARENT_BLENDING","BLEND_OP"];flags:list[];flags_extra:dict[];keywords:dict[RENDER_TYPE="Opaque",RampTextureDrawer="[TCP2Gradient]",RampTextureLabel="Ramp Texture",SHADER_TARGET="3.0",RIM_LABEL="Rim Lighting"];shaderProperties:list[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,sp(name:"Blend Operation";imps:list[imp_enum(value_type:1;value:0;enum_type:"ToonyColorsPro.ShaderGenerator.BlendOperation";guid:"8b6354fd-def4-42cc-ac25-e6f64df1ce50";op:Multiply;lbl:"Blend Operation";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False)];customTextures:list[];codeInjection:codeInjection(injectedFiles:list[];mark:False);matLayers:list[]) */
/* TCP_HASH 4e7ec9f1af68ddbd87e0af3fb841ecc2 */
