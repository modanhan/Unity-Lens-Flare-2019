Shader "Hidden/Aerobox/Flare"
{


	HLSLINCLUDE
        #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float4 _MainTex_TexelSize;

        TEXTURE2D_SAMPLER2D(_WrappedTexture, sampler_WrappedTexture);
        TEXTURE2D_SAMPLER2D(_BlurTexture, sampler_BlurTexture);
        TEXTURE2D_SAMPLER2D(_FlareTexture, sampler_FlareTexture);
        TEXTURE2D_SAMPLER2D(_ChromaticAberration_Spectrum, sampler_ChromaticAberration_Spectrum);
        TEXTURE2D_SAMPLER2D(_AddTexture, sampler_AddTexture);

        float _Intensity;
        float _Delta;
        float _DirtIntensity;
        float _InvIterations;
        float _Add;

        const float HALO_WIDTH = 1.0;

        float4 SampleBox(float2 uv, float delta)
        {
            float4 o = _MainTex_TexelSize.xyxy * float4(-delta, -delta, delta, delta);
            return (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + o.xy)
            + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + o.zy)
            + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + o.xw)
            + SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + o.zw)) * 0.25;
        }

        float4 FragBox(VaryingsDefault i) : SV_Target
		{
			return SampleBox(i.texcoord, _Delta);
		}

		static const float gaussian[7] = { 
            0.00598,	0.060626,	0.241843,	0.383103,	0.241843,	0.060626,	0.00598
        };

		float4 FragHBlur(VaryingsDefault i) : SV_Target
		{
			float4 color;
            float2 o = float2(_MainTex_TexelSize.x, 0);
			for(int idx = -3; idx <= 3; idx++){
				float4 tColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + idx * o);
            	color += tColor * gaussian[idx + 3];
			}
			return color;
		}

        float4 FragVBlur(VaryingsDefault i) : SV_Target
		{
			float4 color;
            float2 o = float2(0, _MainTex_TexelSize.y);
			for(int idx = -3; idx <= 3; idx++){
				float4 tColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + idx * o);
            	color += tColor * gaussian[idx + 3];
			}
			return color;
		}

        float4 FragRadialLerp(VaryingsDefault i) : SV_Target
        {

            return 0;

        }

        float4 FragRadialWarp(VaryingsDefault i) : SV_Target
        {
            float2 ghostVec = i.texcoord - 0.5;
            float2 haloVec = normalize(ghostVec) * -0.5;
            return max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + haloVec) - 0.0, 0)
                * length(ghostVec) * 0.0025;
        }

        static const float ghosts[9] = { 
            0.625,	0.390625,	0.24414,	0.15258,    -0.625,	-0.390625,	-0.24414,	-0.15258,   -0.09536,
        };

        float4 FragGhost(VaryingsDefault i) : SV_Target
        {
            float4 color = float4(0, 0, 0, 0);
            float2 uv = i.texcoord - 0.5;
            for (int i = 0; i < 9; i++) {
                float t_p = ghosts[i];
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv * t_p + 0.5) * (t_p * t_p);
            }
            return color * 0.005;
        }

        float4 FragAdd(VaryingsDefault i) : SV_Target
        {
            return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord)
                + SAMPLE_TEXTURE2D(_AddTexture, sampler_AddTexture, i.texcoord) * _Add;
        }

        float4 FragChromaticAberration(VaryingsDefault i) : SV_Target
        {
			float2 texcoord = i.texcoord.xy * 2 - 1;
            float2 diff_texels = normalize(-texcoord) * pow(dot(texcoord, texcoord), 0.25) * 36;
            float2 diff_sampler = diff_texels * _MainTex_TexelSize.xy;
			float2 pos = i.texcoord.xy;
			int samples = clamp(int(length(diff_texels)), 3, 18);
            float inv_samples = 1.0 / samples;
			float2 delta = diff_sampler * inv_samples;
            pos -= delta * samples * 0.5;
			float4 sum = float4(0, 0, 0, 1), filterSum = float4(0, 0, 0, 1);
            float2 t = float2(0.5 * inv_samples, 0);
			for (int i = 0; i < samples; i++)
			{
				float4 s = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, pos, 0);
				float4 filter = SAMPLE_TEXTURE2D_LOD(_ChromaticAberration_Spectrum, sampler_ChromaticAberration_Spectrum, t, 0);
				sum += s * filter;
                t.x += inv_samples;
				filterSum += filter;
				pos += delta;
			}
			return sum / filterSum;
        }

	ENDHLSL

	    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        
        // 0 Box Up
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragBox

            ENDHLSL
        }

        // 1 HBlur
		Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragHBlur

            ENDHLSL
        }

        // 2 VBlur
		Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragVBlur

            ENDHLSL
        }

        // 3 
        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment FragRadialLerp

            ENDHLSL
        }

        // 4 Radial Warp
        Pass {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragRadialWarp
            ENDHLSL
        }
        
        // 5 Ghost
        Pass {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragGhost
            ENDHLSL
        }

        // 6 Add
        Pass {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragAdd
            ENDHLSL
        }

        // 7 Chromatic Aberration
        Pass {
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragChromaticAberration
            ENDHLSL
        }
    }
}