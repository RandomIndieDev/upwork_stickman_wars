// Toony Colors Pro+Mobile 2
// (c) 2014-2021 Jean Moreno

Shader "Toony Colors Pro 2/User/PiggyShaderTestOutline"
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
		[NoScaleOffset] _Alpha ("Alpha Texture", 2D) = "white" {}
		 _Alpha1 ("Alpha Range", Range(0,1)) = 0.5
		[TCP2Separator]

		[TCP2Header(Ramp Shading)]
		_RampThreshold ("Threshold", Range(0.01,1)) = 0.5
		_RampSmoothing ("Smoothing", Range(0.001,1)) = 0.5
		[TCP2Separator]
		
		[TCP2HeaderHelp(Specular)]
		[Toggle(TCP2_SPECULAR)] _UseSpecular ("Enable Specular", Float) = 0
		[TCP2ColorNoAlpha] _SpecularColor ("Specular Color", Color) = (0.5,0.5,0.5,1)
		_SpecularShadowAttenuation ("Specular Shadow Attenuation", Float) = 0.25
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
		
		[TCP2HeaderHelp(MatCap)]
		[Toggle(TCP2_MATCAP)] _UseMatCap ("Enable MatCap", Float) = 0
		[NoScaleOffset] _MatCapTex ("MatCap (RGB)", 2D) = "white" {}
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
		
		[TCP2HeaderHelp(Silhouette Pass)]
		_SilhouetteColor ("Silhouette Color", Color) = (0,0,0,0.33)
		[TCP2Separator]
		
		[TCP2HeaderHelp(Outline)]
		_OutlineWidth ("Width", Range(0.1,4)) = 1
		_OutlineColorVertex ("Color", Color) = (0,0,0,1)
		// Outline Normals
		[TCP2MaterialKeywordEnumNoPrefix(Regular, _, Vertex Colors, TCP2_COLORS_AS_NORMALS, Tangents, TCP2_TANGENT_AS_NORMALS, UV1, TCP2_UV1_AS_NORMALS, UV2, TCP2_UV2_AS_NORMALS, UV3, TCP2_UV3_AS_NORMALS, UV4, TCP2_UV4_AS_NORMALS)]
		_NormalsSource ("Outline Normals Source", Float) = 0
		[TCP2MaterialKeywordEnumNoPrefix(Full XYZ, TCP2_UV_NORMALS_FULL, Compressed XY, _, Compressed ZW, TCP2_UV_NORMALS_ZW)]
		_NormalsUVType ("UV Data Type", Float) = 0
		[TCP2Separator]

		// Avoid compile error if the properties are ending with a drawer
		[HideInInspector] __dummy__ ("unused", Float) = 0
	}

	SubShader
	{
		Tags
		{
			"RenderType"="Opaque"
			"Queue"="Geometry+10" // Make sure that the objects are rendered later to avoid sorting issues with the transparent silhouette
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
		TCP2_TEX2D_WITH_SAMPLER(_BumpMap);
		TCP2_TEX2D_WITH_SAMPLER(_MainTex);
		TCP2_TEX2D_WITH_SAMPLER(_Alpha);
		
		// Shader Properties
		float _OutlineWidth;
		fixed4 _OutlineColorVertex;
		fixed4 _SilhouetteColor;
		float _BumpScale;
		float4 _MainTex_ST;
		float _Alpha1;
		fixed4 _Color;
		fixed4 _MatCapColor;
		float _RampThreshold;
		float _RampSmoothing;
		fixed4 _HColor;
		fixed4 _SColor;
		float _SpecularToonSize;
		float _SpecularToonSmoothness;
		float _SpecularShadowAttenuation;
		fixed4 _SpecularColor;
		float4 _RimDir;
		float _RimMin;
		float _RimMax;
		fixed4 _RimColor;

		sampler2D _MatCapTex;
		samplerCUBE _AmbientCube;

		ENDCG

		// Outline Include
		CGINCLUDE

		struct appdata_outline
		{
			float4 vertex : POSITION;
			float3 normal : NORMAL;
			#if TCP2_UV1_AS_NORMALS
			float4 texcoord0 : TEXCOORD0;
		#elif TCP2_UV2_AS_NORMALS
			float4 texcoord1 : TEXCOORD1;
		#elif TCP2_UV3_AS_NORMALS
			float4 texcoord2 : TEXCOORD2;
		#elif TCP2_UV4_AS_NORMALS
			float4 texcoord3 : TEXCOORD3;
		#endif
		#if TCP2_COLORS_AS_NORMALS
			float4 vertexColor : COLOR;
		#endif
			float4 tangent : TANGENT;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		struct v2f_outline
		{
			float4 vertex : SV_POSITION;
			float4 vcolor : TEXCOORD0;
			float3 pack1 : TEXCOORD1; /* pack1.xyz = worldNormal */
			float3 pack2 : TEXCOORD2; /* pack2.xyz = worldPos */
			UNITY_VERTEX_OUTPUT_STEREO
		};

		v2f_outline vertex_outline (appdata_outline v)
		{
			v2f_outline output;
			UNITY_INITIALIZE_OUTPUT(v2f_outline, output);
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			float3 worldNormalUv = mul(unity_ObjectToWorld, float4(v.normal, 1.0)).xyz;
			// Shader Properties Sampling
			float __outlineWidth = ( _OutlineWidth );
			float4 __outlineColorVertex = ( _OutlineColorVertex.rgba );

			float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
			output.pack2.xyz = worldPos;
			output.pack1.xyz = worldNormalUv;
		
		#ifdef TCP2_COLORS_AS_NORMALS
			//Vertex Color for Normals
			float3 normal = (v.vertexColor.xyz*2) - 1;
		#elif TCP2_TANGENT_AS_NORMALS
			//Tangent for Normals
			float3 normal = v.tangent.xyz;
		#elif TCP2_UV1_AS_NORMALS || TCP2_UV2_AS_NORMALS || TCP2_UV3_AS_NORMALS || TCP2_UV4_AS_NORMALS
			#if TCP2_UV1_AS_NORMALS
				#define uvChannel texcoord0
			#elif TCP2_UV2_AS_NORMALS
				#define uvChannel texcoord1
			#elif TCP2_UV3_AS_NORMALS
				#define uvChannel texcoord2
			#elif TCP2_UV4_AS_NORMALS
				#define uvChannel texcoord3
			#endif
		
			#if TCP2_UV_NORMALS_FULL
			//UV for Normals, full
			float3 normal = v.uvChannel.xyz;
			#else
			//UV for Normals, compressed
			#if TCP2_UV_NORMALS_ZW
				#define ch1 z
				#define ch2 w
			#else
				#define ch1 x
				#define ch2 y
			#endif
			float3 n;
			//unpack uvs
			v.uvChannel.ch1 = v.uvChannel.ch1 * 255.0/16.0;
			n.x = floor(v.uvChannel.ch1) / 15.0;
			n.y = frac(v.uvChannel.ch1) * 16.0 / 15.0;
			//- get z
			n.z = v.uvChannel.ch2;
			//- transform
			n = n*2 - 1;
			float3 normal = n;
			#endif
		#else
			float3 normal = v.normal;
		#endif
		
		#if TCP2_ZSMOOTH_ON
			//Correct Z artefacts
			normal = UnityObjectToViewPos(normal);
			normal.z = -_ZSmooth;
		#endif
			float size = 1;
		
		#if !defined(SHADOWCASTER_PASS)
			output.vertex = UnityObjectToClipPos(v.vertex.xyz + normal * __outlineWidth * size * 0.01);
		#else
			v.vertex = v.vertex + float4(normal,0) * __outlineWidth * size * 0.01;
		#endif
		
			output.vcolor.xyzw = __outlineColorVertex;

			return output;
		}

		float4 fragment_outline (v2f_outline input) : SV_Target
		{

			// Shader Properties Sampling
			float4 __outlineColor = ( float4(1,1,1,1) );

			half4 outlineColor = __outlineColor * input.vcolor.xyzw;

			return outlineColor;
		}

		ENDCG
		// Outline Include End
		// Silhouette Pass
		Pass
		{
			Name "Silhouette"
			Tags
			{
				"LightMode"="ForwardBase"
			}
			Blend SrcAlpha OneMinusSrcAlpha
			ZTest Greater
			ZWrite Off

			Stencil
			{
				Ref 1
				Comp NotEqual
				Pass Replace
				ReadMask 1
				WriteMask 1
			}

			CGPROGRAM
			#pragma vertex vertex_silhouette
			#pragma fragment fragment_silhouette
			#pragma multi_compile_instancing
			#pragma target 3.0

			struct appdata_sil
			{
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f_sil
			{
				float4 vertex : SV_POSITION;
				UNITY_VERTEX_OUTPUT_STEREO
				float3 pack0 : TEXCOORD0; /* pack0.xyz = worldPos */
				float3 pack1 : TEXCOORD1; /* pack1.xyz = worldNormal */
			};

			v2f_sil vertex_silhouette (appdata_sil v)
			{
				v2f_sil output;
				UNITY_INITIALIZE_OUTPUT(v2f_sil, output);
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

				float3 worldNormalUv = mul(unity_ObjectToWorld, float4(v.normal, 1.0)).xyz;

				float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
				output.pack0.xyz = worldPos;
				output.pack1.xyz = worldNormalUv;
				output.vertex = UnityObjectToClipPos(v.vertex);

				return output;
			}

			half4 fragment_silhouette (v2f_sil input) : SV_Target
			{

				// Shader Properties Sampling
				float4 __silhouetteColor = ( _SilhouetteColor.rgba );

				return __silhouetteColor;
			}
			ENDCG
		}

		// Main Surface Shader
		Blend [_SrcBlend] [_DstBlend]
		Cull [_Cull]
		ZWrite [_ZWrite]

		Stencil
		{
			Ref 0
			ReadMask 255
			WriteMask 255
			Comp Always
			Pass Keep
			Fail Keep
			ZFail Keep
		}

		CGPROGRAM

		#pragma surface surf ToonyColorsCustom vertex:vertex_surface exclude_path:deferred exclude_path:prepass keepalpha noforwardadd nolightmap nofog nolppv keepalpha
		#pragma target 3.0

		//================================================================
		// SHADER KEYWORDS

		#pragma shader_feature_local_fragment TCP2_SPECULAR
		#pragma shader_feature_local_fragment TCP2_RIM_LIGHTING
		#pragma shader_feature_local_fragment TCP2_AMBIENT
		#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
		#pragma shader_feature_local TCP2_MATCAP
		#pragma shader_feature_local _NORMALMAP

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
			float __specularShadowAttenuation;
			float3 __specularColor;
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

			input.worldNormal = WorldNormalVector(input, output.Normal);

			// Shader Properties Sampling
			float4 __normalMap = ( TCP2_TEX2D_SAMPLE(_BumpMap, _BumpMap, input.texcoord0.xy).rgba );
			float __bumpScale = ( _BumpScale );
			float4 __albedo = ( TCP2_TEX2D_SAMPLE(_MainTex, _MainTex, input.texcoord0.xy).rgba );
			float __alpha = (  clamp(_Alpha1+TCP2_TEX2D_SAMPLE(_Alpha, _Alpha, input.texcoord0.xy),0,1) );
			float4 __mainColor = ( _Color.rgba );
			float3 __matcapColor = ( _MatCapColor.rgb );
			output.__rampThreshold = ( _RampThreshold );
			output.__rampSmoothing = ( _RampSmoothing );
			output.__highlightColor = ( _HColor.rgb );
			output.__shadowColor = ( _SColor.rgb );
			output.__ambientIntensity = ( 1.0 );
			output.__specularToonSize = ( _SpecularToonSize );
			output.__specularToonSmoothness = ( _SpecularToonSmoothness );
			output.__specularShadowAttenuation = ( _SpecularShadowAttenuation );
			output.__specularColor = ( _SpecularColor.rgb );
			output.__rimDir = ( _RimDir.xyz );
			output.__rimMin = ( _RimMin );
			output.__rimMax = ( _RimMax );
			output.__rimColor = ( _RimColor.rgb );
			output.__rimStrength = ( 1.0 );

			output.input = input;

			#if defined(_NORMALMAP)
			half4 normalMap = __normalMap;
			output.Normal = UnpackScaleNormal(normalMap, __bumpScale);
			output.normalTS = output.Normal;

			#endif

			half3 worldNormal = WorldNormalVector(input, output.Normal);
			output.worldNormal = worldNormal;

			output.Albedo = __albedo.rgb;
			output.Alpha = __alpha;

			output.Albedo *= __mainColor.rgb;
			
			//MatCap
			#if defined(TCP2_MATCAP)
			half2 capCoord = mul((float3x3)UNITY_MATRIX_V, worldNormal).xy * 0.5 + 0.5;
			fixed3 matcap = tex2D(_MatCapTex, capCoord).rgb * __matcapColor;
			output.Albedo *= matcap;
			#endif

		}

		//================================================================
		// LIGHTING FUNCTION

		inline half4 LightingToonyColorsCustom(inout SurfaceOutputCustom surface, half3 viewDir, UnityGI gi)
		{

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
			// Apply attenuation (shadowmaps & point/spot lights attenuation)
			ndl *= atten;
			half3 ramp;
			
			#define		RAMP_THRESHOLD	surface.__rampThreshold
			#define		RAMP_SMOOTH		surface.__rampSmoothing
			ndl = saturate(ndl);
			ramp = smoothstep(RAMP_THRESHOLD - RAMP_SMOOTH*0.5, RAMP_THRESHOLD + RAMP_SMOOTH*0.5, ndl);

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
			//Blinn-Phong Specular
			half3 h = normalize(lightDir + viewDir);
			float ndh = max(0, dot (normal, h));
			float spec = smoothstep(surface.__specularToonSize + surface.__specularToonSmoothness, surface.__specularToonSize - surface.__specularToonSmoothness,1 - (ndh / (1+surface.__specularToonSmoothness)));
			spec *= ndl;
			spec *= saturate(atten * ndl + surface.__specularShadowAttenuation);
			
			//Apply specular
			color.rgb += spec * lightColor.rgb * surface.__specularColor;
			#endif
			// Rim Lighting
			#if defined(TCP2_RIM_LIGHTING)
			#if !defined(UNITY_PASS_FORWARDADD)
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
			color.rgb += rim * rimColor * rimStrength;
			#endif
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

		// Outline
		Pass
		{
			Name "Outline"
			Tags
			{
				"LightMode"="ForwardBase"
			}
			Cull Front

			CGPROGRAM
			#pragma vertex vertex_outline
			#pragma fragment fragment_outline
			#pragma target 3.0
			#pragma multi_compile _ TCP2_COLORS_AS_NORMALS TCP2_TANGENT_AS_NORMALS TCP2_UV1_AS_NORMALS TCP2_UV2_AS_NORMALS TCP2_UV3_AS_NORMALS TCP2_UV4_AS_NORMALS
			#pragma multi_compile _ TCP2_UV_NORMALS_FULL TCP2_UV_NORMALS_ZW
			#pragma multi_compile_instancing
			ENDCG
		}
	}

	Fallback "Diffuse"
	CustomEditor "ToonyColorsPro.ShaderGenerator.MaterialInspector_SG2"
}

/* TCP_DATA u config(unity:"2020.3.7f1";ver:"2.8.1";tmplt:"SG2_Template_Default";features:list["UNITY_5_4","UNITY_5_5","UNITY_5_6","UNITY_2017_1","UNITY_2018_1","UNITY_2018_2","UNITY_2018_3","UNITY_2019_1","UNITY_2019_2","UNITY_2019_3","UNITY_2019_4","UNITY_2020_1","ATTEN_AT_NDL","SPECULAR","SPEC_LEGACY","SPECULAR_TOON","SPECULAR_NO_ATTEN","SPECULAR_SHADER_FEATURE","RIM","RIM_DIR","RIM_SHADER_FEATURE","CUBE_AMBIENT","MATCAP_SHADER_FEATURE","MATCAP_PIXEL","BUMP_SHADER_FEATURE","PASS_SILHOUETTE","ALPHA_TESTING_DITHERING","STENCIL","AMBIENT_SHADER_FEATURE","MATCAP_MULT","MATCAP","BUMP_SCALE","BUMP","DISSOLVE_SHADER_FEATURE","AUTO_TRANSPARENT_BLENDING","DISSOLVE_GRADIENT","SILHOUETTE_STENCIL","OUTLINE"];flags:list["noforwardadd"];flags_extra:dict[pragma_gpu_instancing=list[]];keywords:dict[RENDER_TYPE="Opaque",RampTextureDrawer="[TCP2Gradient]",RampTextureLabel="Ramp Texture",SHADER_TARGET="3.0",RIM_LABEL="Rim Lighting"];shaderProperties:list[,,sp(name:"Alpha";imps:list[imp_customcode(prepend_type:Disabled;prepend_code:"";prepend_file:"";prepend_file_block:"";preprend_params:dict[];code:"clamp({3}+{2},0,1)";guid:"993e65d6-0ca1-4b7a-8735-4b565381bef2";op:Multiply;lbl:"Alpha";gpu_inst:False;locked:False;impl_index:-1),imp_mp_texture(uto:False;tov:"";tov_lbl:"";gto:False;sbt:False;scr:False;scv:"";scv_lbl:"";gsc:False;roff:False;goff:False;sin_anm:False;sin_anmv:"";sin_anmv_lbl:"";gsin:False;notile:False;triplanar_local:False;def:"white";locked_uv:False;uv:0;cc:1;chan:"R";mip:-1;mipprop:False;ssuv_vert:False;ssuv_obj:False;uv_type:Texcoord;uv_chan:"XZ";tpln_scale:1;uv_shaderproperty:__NULL__;uv_cmp:__NULL__;sep_sampler:__NULL__;prop:"_Alpha";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"5a9cdc57-cb4e-496e-b200-8abd168ddde7";op:Multiply;lbl:"Alpha Texture";gpu_inst:False;locked:False;impl_index:-1),imp_mp_range(def:0.5;min:0;max:1;prop:"_Alpha1";md:"";gbv:False;custom:False;refs:"";pnlock:False;guid:"ae13f595-cb5b-43d9-b7a9-7de6053b2170";op:Multiply;lbl:"Alpha Range";gpu_inst:False;locked:False;impl_index:-1)];layers:list[];unlocked:list[];clones:dict[];isClone:False),,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,sp(name:"Blend Operation";imps:list[imp_enum(value_type:1;value:0;enum_type:"ToonyColorsPro.ShaderGenerator.BlendOperation";guid:"9727ff25-0e42-4228-a6a0-5ed0f296799c";op:Multiply;lbl:"Blend Operation";gpu_inst:False;locked:False;impl_index:0)];layers:list[];unlocked:list[];clones:dict[];isClone:False)];customTextures:list[];codeInjection:codeInjection(injectedFiles:list[];mark:False);matLayers:list[]) */
/* TCP_HASH 58d7bd8693bbfe6bcec63e1928d16217 */
