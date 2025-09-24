// Toony Colors Pro+Mobile 2
// (c) 2014-2021 Jean Moreno

Shader "Toony Colors Pro 2/User/NoTileFogSpecEtc"
{
	Properties
	{
		[TCP2HeaderHelp(Base)]
		_Color ("Color", Color) = (1,1,1,1)
		[TCP2ColorNoAlpha] _HColor ("Highlight Color", Color) = (0.75,0.75,0.75,1)
		[TCP2ColorNoAlpha] _SColor ("Shadow Color", Color) = (0.2,0.2,0.2,1)
		_MainTex ("Albedo", 2D) = "white" {}
		[TCP2Separator]

		[TCP2Header(Ramp Shading)]
		_RampThreshold ("Threshold", Range(0.01,1)) = 0.5
		_RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
		[TCP2Separator]
		
		[TCP2HeaderHelp(Specular)]
		[Toggle(TCP2_SPECULAR)] _UseSpecular ("Enable Specular", Float) = 0
		[TCP2ColorNoAlpha] _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
		_SpecularToonSize ("Toon Size", Range(0,1)) = 0.25
		_SpecularToonSmoothness ("Toon Smoothness", Range(0.001,0.5)) = 0.05
		[TCP2Separator]
		
		[TCP2HeaderHelp(Rim Lighting)]
		[Toggle(TCP2_RIM_LIGHTING)] _UseRim ("Enable Rim Lighting", Float) = 0
		[TCP2ColorNoAlpha] _RimColor ("Rim Color", Color) = (0.8,0.8,0.8,0.5)
		_RimMin ("Rim Min", Range(0,2)) = 0.5
		_RimMax ("Rim Max", Range(0,2)) = 1
		//Rim Direction
		_RimDir ("Rim Direction", Vector) = (0,0,1,1)
		[TCP2Separator]

		[TCP2HeaderHelp(Reflections)]
		[Toggle(TCP2_REFLECTIONS)] _UseReflections ("Enable Reflections", Float) = 0
		
		[NoScaleOffset] _Cube ("Reflection Cubemap", Cube) = "black" {}
		[TCP2ColorNoAlpha] _ReflectionCubemapColor ("Color", Color) = (1,1,1,1)
		_ReflectionCubemapRoughness ("Cubemap Roughness", Range(0,1)) = 0.5
		_FresnelMin ("Fresnel Min", Range(0,2)) = 0
		_FresnelMax ("Fresnel Max", Range(0,2)) = 1.5
		[TCP2Separator]
		
		[TCP2HeaderHelp(MatCap)]
		[Toggle(TCP2_MATCAP)] _UseMatCap ("Enable MatCap", Float) = 0
		[NoScaleOffset] _MatCapTex ("MatCap (RGB)", 2D) = "black" {}
		[TCP2ColorNoAlpha] _MatCapColor ("MatCap Color", Color) = (1,1,1,1)
		[TCP2Separator]
		[TCP2HeaderHelp(Ambient Lighting)]
		[Toggle(TCP2_AMBIENT)] _UseAmbient ("Enable Ambient/Indirect Diffuse", Float) = 0
		//AMBIENT CUBEMAP
		_AmbientCube ("Ambient Cubemap", Cube) = "_Skybox" {}
		[TCP2Separator]
		
		[TCP2HeaderHelp(Normal Mapping)]
		[Toggle(_NORMALMAP)] _UseNormalMap ("Enable Normal Mapping", Float) = 0
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Scale", Float) = 1
		[TCP2Separator]
		
		[TCP2TextureSingleLine] _NoTileNoiseTex ("Non-repeating Tiling Noise Texture", 2D) = "black" {}
		[TCP2Vector4Floats(Contrast X,Contrast Y,Contrast Z,Smoothing,1,16,1,16,1,16,0.05,10)] _TriplanarSamplingStrength ("Triplanar Sampling Parameters", Vector) = (8,8,8,0.5)

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
		sampler2D _BumpMap;
		sampler2D _MainTex;
		
		// Shader Properties
		float _BumpScale;
		float4 _MainTex_ST;
		fixed4 _Color;
		fixed4 _MatCapColor;
		float _RampThreshold;
		float _RampSmoothing;
		fixed4 _HColor;
		fixed4 _SColor;
		float _SpecularToonSize;
		float _SpecularToonSmoothness;
		fixed4 _SpecularColor;
		float4 _RimDir;
		float _RimMin;
		float _RimMax;
		fixed4 _RimColor;
		float _ReflectionCubemapRoughness;
		fixed4 _ReflectionCubemapColor;
		float _FresnelMin;
		float _FresnelMax;

		// Non-repeating tiling
		sampler2D _NoTileNoiseTex;
		float4 _NoTileNoiseTex_TexelSize;
		
		float4 _TriplanarSamplingStrength;
		samplerCUBE _Cube;
		sampler2D _MatCapTex;
		samplerCUBE _AmbientCube;

		// Non-repeating tiling texture fetch function
		// Adapted from: http://www.iquilezles.org/www/articles/texturerepetition/texturerepetition.htm (c) 2017 - Inigo Quilez - MIT License
		float4 tex2D_noTile(sampler2D samp, in float2 uv)
		{
			// sample variation pattern
			float k = tex2D(_NoTileNoiseTex, (1/_NoTileNoiseTex_TexelSize.zw) * uv).a; // cheap (cache friendly) lookup
		
			// compute index
			float index = k*8.0;
			float i = floor(index);
			float f = frac(index);
		
			// offsets for the different virtual patterns
			float2 offa = sin(float2(3.0,7.0)*(i+0.0)); // can replace with any other hash
			float2 offb = sin(float2(3.0,7.0)*(i+1.0)); // can replace with any other hash
		
			// compute derivatives for mip-mapping
			float2 dx = ddx(uv);
			float2 dy = ddy(uv);
		
			// sample the two closest virtual patterns
			float4 cola = tex2Dgrad(samp, uv + offa, dx, dy);
			float4 colb = tex2Dgrad(samp, uv + offb, dx, dy);
		
			// interpolate between the two virtual patterns
			return lerp(cola, colb, smoothstep(0.2,0.8,f-0.1*dot(cola-colb, 1)));
		}
		
		// Texture sampling with triplanar UVs
		float4 tex2D_triplanar(sampler2D samp, float4 tiling_offset, float3 worldPos, float3 worldNormal)
		{
			half4 sample_y = ( tex2D(samp, worldPos.xz * tiling_offset.xy + tiling_offset.zw).rgba );
			fixed4 sample_x = ( tex2D(samp, worldPos.zy * tiling_offset.xy + tiling_offset.zw).rgba );
			fixed4 sample_z = ( tex2D(samp, worldPos.xy * tiling_offset.xy + tiling_offset.zw).rgba );
			
			// blending
			half3 blendWeights = pow(abs(worldNormal), _TriplanarSamplingStrength.xyz / _TriplanarSamplingStrength.w);
			blendWeights = blendWeights / (blendWeights.x + abs(blendWeights.y) + blendWeights.z);
			half4 triplanar = sample_x * blendWeights.x + sample_y * blendWeights.y + sample_z * blendWeights.z;
			
			return triplanar;
		}
			
		half4 tex2D_triplanar_noTile(sampler2D samp, float4 tiling_offset, float3 worldPos, float3 worldNormal)
		{
			half4 sample_y = ( tex2D_noTile(samp, worldPos.xz * tiling_offset.xy + tiling_offset.zw).rgba );
			fixed4 sample_x = ( tex2D_noTile(samp, worldPos.zy * tiling_offset.xy + tiling_offset.zw).rgba );
			fixed4 sample_z = ( tex2D_noTile(samp, worldPos.xy * tiling_offset.xy + tiling_offset.zw).rgba );
			
			// blending
			half3 blendWeights = pow(abs(worldNormal), _TriplanarSamplingStrength.xyz / _TriplanarSamplingStrength.w);
			blendWeights = blendWeights / (blendWeights.x + abs(blendWeights.y) + blendWeights.z);
			half4 triplanar = sample_x * blendWeights.x + sample_y * blendWeights.y + sample_z * blendWeights.z;
			
			return triplanar;
		}
		
		ENDCG

		// Main Surface Shader

		CGPROGRAM

		#pragma surface surf ToonyColorsCustom vertex:vertex_surface exclude_path:deferred exclude_path:prepass keepalpha nolightmap nolppv
		#pragma target 3.0

		//================================================================
		// SHADER KEYWORDS

		#pragma shader_feature_local_fragment TCP2_SPECULAR
		#pragma shader_feature_local_fragment TCP2_RIM_LIGHTING
		#pragma shader_feature_local_fragment TCP2_REFLECTIONS
		#pragma shader_feature_local_fragment TCP2_AMBIENT
		#pragma shader_feature_local TCP2_MATCAP
		#pragma shader_feature_local _NORMALMAP

		//================================================================
		// STRUCTS

		// Vertex input
		struct appdata_tcp2
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
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
			half2 matcap;
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
			float3 normalTS;

			Input input;

			// Shader Properties
			float __rampThreshold;
			float __rampSmoothing;
			float3 __highlightColor;
			float3 __shadowColor;
			float __ambientIntensity;
			float __specularToonSize;
			float __specularToonSmoothness;
			float3 __specularColor;
			float3 __rimDir;
			float __rimMin;
			float __rimMax;
			float3 __rimColor;
			float __rimStrength;
			float __reflectionCubemapRoughness;
			float3 __reflectionCubemapColor;
			float __fresnelMin;
			float __fresnelMax;
		};

		//================================================================
		// VERTEX FUNCTION

		void vertex_surface(inout appdata_tcp2 v, out Input output)
		{
			UNITY_INITIALIZE_OUTPUT(Input, output);

			output.tangent = mul(unity_ObjectToWorld, float4(v.tangent.xyz, 0)).xyz;
			#if defined(TCP2_MATCAP)
			//MatCap
			float3 worldNorm = normalize(unity_WorldToObject[0].xyz * v.normal.x + unity_WorldToObject[1].xyz * v.normal.y + unity_WorldToObject[2].xyz * v.normal.z);
			worldNorm = mul((float3x3)UNITY_MATRIX_V, worldNorm);
			output.matcap = worldNorm.xy * 0.5 + 0.5;
			#endif

		}

		//================================================================
		// SURFACE FUNCTION

		void surf(Input input, inout SurfaceOutputCustom output)
		{

			input.worldNormal = WorldNormalVector(input, output.Normal);

			// Shader Properties Sampling
			float4 __normalMap = ( tex2D_triplanar_noTile(_BumpMap, float4(1, 1, 0, 0), input.worldPos, input.worldNormal) );
			float __bumpScale = ( _BumpScale );
			float4 __albedo = ( tex2D_triplanar_noTile(_MainTex, float4(1, 1, 1, 1) * _MainTex_ST, input.worldPos, input.worldNormal) );
			float4 __mainColor = ( _Color.rgba );
			float __alpha = ( __albedo.a * __mainColor.a );
			float3 __matcapColor = ( _MatCapColor.rgb );
			output.__rampThreshold = ( _RampThreshold );
			output.__rampSmoothing = ( _RampSmoothing );
			output.__highlightColor = ( _HColor.rgb );
			output.__shadowColor = ( _SColor.rgb );
			output.__ambientIntensity = ( 1.0 );
			output.__specularToonSize = ( _SpecularToonSize );
			output.__specularToonSmoothness = ( _SpecularToonSmoothness );
			output.__specularColor = ( _SpecularColor.rgb );
			output.__rimDir = ( _RimDir.xyz );
			output.__rimMin = ( _RimMin );
			output.__rimMax = ( _RimMax );
			output.__rimColor = ( _RimColor.rgb );
			output.__rimStrength = ( 1.0 );
			output.__reflectionCubemapRoughness = ( _ReflectionCubemapRoughness );
			output.__reflectionCubemapColor = ( _ReflectionCubemapColor.rgb );
			output.__fresnelMin = ( _FresnelMin );
			output.__fresnelMax = ( _FresnelMax );

			output.input = input;

			#if defined(_NORMALMAP)
			half4 normalMap = __normalMap;
			output.Normal = UnpackScaleNormal(normalMap, __bumpScale);
			output.normalTS = output.Normal;

			#endif

			half3 worldNormal = WorldNormalVector(input, output.Normal);
			output.worldNormal = worldNormal;

			half ndv = abs(dot(input.viewDir, normalize(output.Normal.xyz)));
			half ndvRaw = ndv;
			output.ndv = ndv;
			output.ndvRaw = ndvRaw;

			output.Albedo = __albedo.rgb;
			output.Alpha = __alpha;

			output.Albedo *= __mainColor.rgb;
			
			//MatCap
			#if defined(TCP2_MATCAP)
			fixed3 matcap = tex2D(_MatCapTex, input.matcap).rgb * __matcapColor;
			output.Emission += matcap;
			#endif

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

			#if defined(TCP2_SPECULAR)
			//Blinn-Phong Specular
			half3 h = normalize(lightDir + viewDir);
			float ndh = max(0, dot (normal, h));
			float spec = smoothstep(surface.__specularToonSize + surface.__specularToonSmoothness, surface.__specularToonSize - surface.__specularToonSmoothness,1 - (ndh / (1+surface.__specularToonSmoothness)));
			spec *= ndl;
			spec *= atten;
			
			//Apply specular
			color.rgb += spec * lightColor.rgb * surface.__specularColor;
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

			half3 reflections = half3(0, 0, 0);
			#if defined(TCP2_REFLECTIONS)
			// Reflection cubemap
			half3 reflectVector = reflect(-viewDir, surface.worldNormal);
			reflections.rgb += texCUBElod(_Cube, half4(reflectVector.xyz, surface.__reflectionCubemapRoughness * 10.0)).rgb;
			reflections.rgb *= surface.__reflectionCubemapColor;
			#endif
			half fresnelMin = surface.__fresnelMin;
			half fresnelMax = surface.__fresnelMax;
			half fresnelTerm = smoothstep(fresnelMin, fresnelMax, 1 - surface.ndvRaw);
			reflections *= fresnelTerm;
			color.rgb += reflections;

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

/* TCP_DATA u config(unity:"2020.3.7f1";ver:"2.8.1";tmplt:"SG2_Template_Default";features:list["UNITY_5_4","UNITY_5_5","UNITY_5_6","UNITY_2017_1","UNITY_2018_1","UNITY_2018_2","UNITY_2018_3","UNITY_2019_1","UNITY_2019_2","UNITY_2019_3","UNITY_2019_4","UNITY_2020_1","ENABLE_FOG","SPECULAR","SPEC_LEGACY","SPECULAR_TOON","SPECULAR_SHADER_FEATURE","RIM","RIM_DIR","RIM_LIGHTMASK","RIM_SHADER_FEATURE","REFLECTION_CUBEMAP","REFLECTION_FRESNEL","REFLECTION_SHADER_FEATURE","REFL_ROUGH","MATCAP_ADD","MATCAP","MATCAP_SHADER_FEATURE","CUBE_AMBIENT","AMBIENT_SHADER_FEATURE","BUMP","BUMP_SCALE","BUMP_SHADER_FEATURE"];flags:list[];flags_extra:dict[];keywords:dict[RENDER_TYPE="Opaque",RampTextureDrawer="[TCP2Gradient]",RampTextureLabel="Ramp Texture",SHADER_TARGET="3.0",RIM_LABEL="Rim Lighting"];shaderProperties:list[sp(name:"Albedo";imps:list[imp_mp_texture(uto:True;tov:"";tov_lbl:"";gto:True;sbt:False;scr:False;scv:"";scv_lbl:"";gsc:False;roff:False;goff:False;sin_anm:False;sin_anmv:"";sin_anmv_lbl:"";gsin:False;notile:True;triplanar_local:False;def:"white";locked_uv:False;uv:6;cc:4;chan:"RGBA";mip:-1;mipprop:False;ssuv_vert:False;ssuv_obj:False;uv_type:Triplanar;uv_chan:"XZ";tpln_scale:1;uv_shaderproperty:__NULL__;uv_cmp:__NULL__;sep_sampler:__NULL__;prop:"_MainTex";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"4b1f0dce-fa0d-48cb-8c5c-5ddcee94946a";op:Multiply;lbl:"Albedo";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False),,,,,,,,,,,,,,,,,,,,,,sp(name:"Normal Map";imps:list[imp_mp_texture(uto:False;tov:"";tov_lbl:"";gto:False;sbt:False;scr:False;scv:"";scv_lbl:"";gsc:False;roff:False;goff:False;sin_anm:False;sin_anmv:"";sin_anmv_lbl:"";gsin:False;notile:True;triplanar_local:False;def:"bump";locked_uv:False;uv:6;cc:4;chan:"RGBA";mip:-1;mipprop:False;ssuv_vert:False;ssuv_obj:False;uv_type:Triplanar;uv_chan:"XZ";tpln_scale:1;uv_shaderproperty:__NULL__;uv_cmp:__NULL__;sep_sampler:__NULL__;prop:"_BumpMap";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"aaf0be0e-3c58-4b7f-bb6b-23ec52d3242f";op:Multiply;lbl:"Normal Map";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False)];customTextures:list[];codeInjection:codeInjection(injectedFiles:list[];mark:False);matLayers:list[]) */
/* TCP_HASH ab88a46db825c77ccffa3f3095c4529d */
