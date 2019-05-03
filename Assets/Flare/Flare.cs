using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Aerobox.Rendering.PostProcessing
{

    [Serializable]
    [PostProcess(typeof(FlareRenderer), PostProcessEvent.BeforeStack, "Aerobox/Flare")]
    public sealed class Flare : PostProcessEffectSettings
    {
        const int MAX_DOWNSAMPLE = 1;
        public TextureParameter spectralLut = new TextureParameter();
    }

    public sealed class FlareRenderer : PostProcessEffectRenderer<Flare>
    {
        const int MAX_DOWNSAMPLE = 1;
        RenderTexture cSource;
        RenderTexture[] renderTextures, renderTextures1;
        RenderTexture downsampled, radialWarped, ghosts, aberration;
        public override void Init()
        {
            cSource = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGBHalf);
            cSource.Create();

            renderTextures = new RenderTexture[MAX_DOWNSAMPLE];
            for (int i = 0; i < MAX_DOWNSAMPLE; i++)
            {
                renderTextures[i] = new RenderTexture(Mathf.Max(Screen.width >> i, 1), Mathf.Max(Screen.height >> i, 1), 0, RenderTextureFormat.ARGBHalf);
                renderTextures[i].Create();
            }
            renderTextures1 = new RenderTexture[MAX_DOWNSAMPLE];
            for (int i = 0; i < MAX_DOWNSAMPLE; i++)
            {
                renderTextures1[i] = new RenderTexture(Mathf.Max(Screen.width >> i, 1), Mathf.Max(Screen.height >> i, 1), 0, RenderTextureFormat.ARGBHalf);
                renderTextures1[i].Create();
            }
            downsampled = new RenderTexture(Mathf.Max(Screen.width >> MAX_DOWNSAMPLE, 1),
                Mathf.Max(Screen.height >> MAX_DOWNSAMPLE, 1), 0, RenderTextureFormat.ARGBHalf);
            downsampled.Create();
            radialWarped = new RenderTexture(Mathf.Max(Screen.width >> MAX_DOWNSAMPLE, 1),
                Mathf.Max(Screen.height >> MAX_DOWNSAMPLE, 1), 0, RenderTextureFormat.ARGBHalf);
            radialWarped.Create();
            ghosts = new RenderTexture(Mathf.Max(Screen.width >> MAX_DOWNSAMPLE, 1),
                Mathf.Max(Screen.height >> MAX_DOWNSAMPLE, 1), 0, RenderTextureFormat.ARGBHalf);
            ghosts.Create();
            aberration = new RenderTexture(Mathf.Max(Screen.width >> MAX_DOWNSAMPLE, 1),
                Mathf.Max(Screen.height >> MAX_DOWNSAMPLE, 1), 0, RenderTextureFormat.ARGBHalf);
            aberration.Create();
        }

        public override void Release()
        {
            if (cSource)
                cSource.Release();
            if (renderTextures != null)
                for (int i = 0; i < MAX_DOWNSAMPLE; i++)
                {
                    if (renderTextures[i])
                        renderTextures[i].Release();
                }
            if (renderTextures1 != null)
                for (int i = 0; i < MAX_DOWNSAMPLE; i++)
                {
                    if (renderTextures1[i])
                        renderTextures1[i].Release();
                }
            if (downsampled != null) downsampled.Release();
            if (radialWarped != null) radialWarped.Release();
            if (ghosts != null) ghosts.Release();
            if (aberration != null) aberration.Release();
        }

        private const int BOX_UP_PASS = 0;
        private const int H_BLUR_PASS = 1;
        private const int V_BLUR_PASS = 2;
        private const int LERP_PASS = 3;
        private const int RADIAL_WARP_PASS = 4;
        private const int GHOST_PASS = 5;
        private const int ADD_PASS = 6;
        private const int ABERRATION_PASS = 7;

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Aerobox/Flare"));
            sheet.properties.SetFloat("_DirtIntensity", Mathf.Pow(MAX_DOWNSAMPLE, 3));

            int rtWidth = context.width;
            int rtHeight = context.height;

            context.command.BlitFullscreenTriangle(context.source, cSource);
            context.command.BlitFullscreenTriangle(cSource, renderTextures[0]);

            sheet.properties.SetFloat("_Delta", 1);
            for (int i = 1; i < MAX_DOWNSAMPLE; i++)
            {
                context.command.BlitFullscreenTriangle(renderTextures[i - 1], renderTextures1[i - 1], sheet, H_BLUR_PASS);
                context.command.BlitFullscreenTriangle(renderTextures1[i - 1], renderTextures[i], sheet, V_BLUR_PASS);
            }
            context.command.BlitFullscreenTriangle(renderTextures[MAX_DOWNSAMPLE - 1], downsampled);

            context.command.BlitFullscreenTriangle(downsampled, radialWarped, sheet, RADIAL_WARP_PASS);
            context.command.BlitFullscreenTriangle(downsampled, ghosts, sheet, GHOST_PASS);

            sheet.properties.SetTexture("_AddTexture", radialWarped);
            sheet.properties.SetFloat("_Add", 4.0f);
            context.command.BlitFullscreenTriangle(ghosts, aberration, sheet, ADD_PASS);
            
            if ((Texture)settings.spectralLut)
                sheet.properties.SetTexture("_ChromaticAberration_Spectrum", settings.spectralLut);
            context.command.BlitFullscreenTriangle(aberration, renderTextures[MAX_DOWNSAMPLE - 1], sheet, ABERRATION_PASS);
            context.command.BlitFullscreenTriangle(renderTextures[MAX_DOWNSAMPLE - 1], renderTextures1[MAX_DOWNSAMPLE - 1], sheet, H_BLUR_PASS);
            context.command.BlitFullscreenTriangle(renderTextures1[MAX_DOWNSAMPLE - 1], renderTextures[MAX_DOWNSAMPLE - 1], sheet, V_BLUR_PASS);

            sheet.properties.SetFloat("_Delta", 0.5f);
            for (int i = MAX_DOWNSAMPLE - 1; i > 0; i--)
            {
                context.command.BlitFullscreenTriangle(renderTextures[i], renderTextures[i - 1], sheet, BOX_UP_PASS);
            }
            context.command.BlitFullscreenTriangle(renderTextures[0], renderTextures1[0], sheet, BOX_UP_PASS);
            // context.command.BlitFullscreenTriangle(renderTextures[0], context.destination);return;
            context.command.BlitFullscreenTriangle(renderTextures1[0], renderTextures[0], sheet, V_BLUR_PASS);
            context.command.BlitFullscreenTriangle(renderTextures[0], renderTextures1[0], sheet, H_BLUR_PASS);
            
            sheet.properties.SetTexture("_AddTexture", renderTextures1[0]);
            sheet.properties.SetFloat("_Add", 1.0f);
            context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, ADD_PASS);
        }
    }
}
