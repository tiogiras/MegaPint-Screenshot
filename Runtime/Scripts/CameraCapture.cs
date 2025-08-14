#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#if USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MegaPint
{

/// <summary> Holds settings and information to render the image of the attached <see cref="Camera" /> component </summary>
[AddComponentMenu("MegaPint/Camera Capture")]
[RequireComponent(typeof(Camera))]
public class CameraCapture : MonoBehaviour
{
    [HideInInspector]
    public int width = 1920;

    [HideInInspector]
    public int height = 1080;

    [HideInInspector]
    public int depth = 32;

    [HideInInspector]
    public BackgroundType backgroundType;

    [HideInInspector]
    public string lastPath = "Assets";

    [HideInInspector]
    public Color backgroundColor;

    [HideInInspector]
    public Sprite backgroundImage;

    [HideInInspector]
    public string imageType = "Simple";

    [HideInInspector]
    public float pixelPerUnit = 1;

    [HideInInspector]
    public bool listenToShortcut;

#if USING_HDRP
    [HideInInspector]
    public int exposureTime = 250;
#endif

    #region Public Methods

    /// <summary> Render the camera's image </summary>
    /// <returns> Rendered image </returns>
#pragma warning disable CS1998
    public async Task <Texture2D> Render()
#pragma warning restore CS1998
    {
        var cam = GetComponent <Camera>();

        PrepareCamera(
            cam,
            out Color bgColor,
            out CameraClearFlags flags,
            out List <GameObject> destroy);

#if USING_URP

        var resetAlphaToFalse = false;
        
        var renderPipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        
        if (backgroundType != BackgroundType.None)
        {
            if (!renderPipeline?.allowPostProcessAlphaOutput ?? true)
            {
                var alphaOutputField = typeof(UniversalRenderPipelineAsset)
                    .GetField("m_AllowPostProcessAlphaOutput", BindingFlags.NonPublic | BindingFlags.Instance);
                    
                alphaOutputField?.SetValue(renderPipeline, true);

                resetAlphaToFalse = true;
            }
        }
#endif
        
#if USING_HDRP
        var camDataHdrp = GetComponent <HDAdditionalCameraData>();

        PrepareCameraData(
            camDataHdrp,
            out HDAdditionalCameraData.ClearColorMode colorMode,
            out Color bgColorHDR,
            out var colorBuffer);
#endif

        // ReSharper disable once RedundantAssignment
        Texture2D render = ScreenshotUtility.RenderCamera(cam, width, height, depth);

#if USING_HDRP
        await Task.Delay(exposureTime);

        render = ScreenshotUtility.RenderCamera(cam, width, height, depth);
#endif

        ResetCamera(cam, bgColor, flags, destroy);
        
#if USING_URP
        if (resetAlphaToFalse)
        {
            var alphaOutputField = typeof(UniversalRenderPipelineAsset)
                .GetField("m_AllowPostProcessAlphaOutput", BindingFlags.NonPublic | BindingFlags.Instance);
            
            alphaOutputField?.SetValue(renderPipeline, false);
        }
#endif
        
#if USING_HDRP
        ResetCameraData(camDataHdrp, colorMode, bgColorHDR, colorBuffer);
#endif

        return render;
    }

    /// <summary> Render the camera and save </summary>
    /// <param name="path"> Export path </param>
    public async void RenderAndSave(string path)
    {
        Save(await Render(), path);
    }

    /// <summary> Save the rendered image </summary>
    /// <param name="texture"> Texture to save </param>
    /// <param name="path"> Export path </param>
    public void Save(Texture2D texture, string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        lastPath = path[..path.LastIndexOf("/", StringComparison.Ordinal)];
        ScreenshotUtility.SaveTexture(texture, path);

#if UNITY_EDITOR
        if (!path.StartsWith("Assets/"))
            return;

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.SaveAndReimport();
#endif
    }

    #endregion

    #region Private Methods

    /// <summary> Reset the camera </summary>
    /// <param name="cam"> Targeted camera </param>
    /// <param name="bgColor"> Background color </param>
    /// <param name="flags"> ClearFlags </param>
    /// <param name="destroy"> Objects to destroy </param>
    private static void ResetCamera(
        Camera cam,
        Color bgColor,
        CameraClearFlags flags,
        IReadOnlyList <GameObject> destroy)
    {
        cam.backgroundColor = bgColor;
        cam.clearFlags = flags;

        for (var i = destroy.Count - 1; i >= 0; i--)
        {
            GameObject obj = destroy[i];
            DestroyImmediate(obj);
        }
    }

    /// <summary> Prepare the camera to render </summary>
    /// <param name="cam"> Targeted camera </param>
    /// <param name="bgColor"> Background color </param>
    /// <param name="flags"> ClearFlags </param>
    /// <param name="destroy"> Objects to destroy </param>
    /// <exception cref="ArgumentOutOfRangeException"> Background not found </exception>
    private void PrepareCamera(
        Camera cam,
        out Color bgColor,
        out CameraClearFlags flags,
        out List <GameObject> destroy)
    {
        bgColor = cam.backgroundColor;
        flags = cam.clearFlags;
        destroy = new List <GameObject>();

        switch (backgroundType)
        {
            case BackgroundType.None:
                cam.clearFlags = CameraClearFlags.Skybox;

                break;

            case BackgroundType.SolidColor:
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = backgroundColor;

                break;

            case BackgroundType.Transparent:
                cam.clearFlags = CameraClearFlags.Depth;

                break;

            case BackgroundType.Image:
                cam.clearFlags = CameraClearFlags.Depth;

                var canvas = new GameObject("RenderCanvas").AddComponent <Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;

                var image = new GameObject("bgImage").AddComponent <Image>();
                image.sprite = backgroundImage;
                image.type = imageType.Equals("Simple") ? Image.Type.Simple : Image.Type.Tiled;
                image.pixelsPerUnitMultiplier = pixelPerUnit;

                RectTransform rect = image.rectTransform;
                Transform parent = canvas.transform;

                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(width, height);
                rect.transform.SetParent(parent);

                rect.localPosition = Vector3.zero;
                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                destroy.Add(canvas.gameObject);
                destroy.Add(image.gameObject);

                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
#if USING_HDRP
    /// <summary> Prepare the camera data for rendering </summary>
    /// <param name="camData"> Targeted camera data </param>
    /// <param name="colorMode"> Color Mode </param>
    /// <param name="bgColor"> Background color </param>
    /// <param name="colorBuffer"> Color buffer </param>
    /// <exception cref="ArgumentOutOfRangeException"> Background not found </exception>
    private void PrepareCameraData(
        HDAdditionalCameraData camData,
        out HDAdditionalCameraData.ClearColorMode colorMode,
        out Color bgColor,
        out string colorBuffer)
    {
        colorMode = camData.clearColorMode;
        bgColor = camData.backgroundColorHDR;
        colorBuffer = "";

        switch (backgroundType)
        {

            case BackgroundType.None:
                camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Sky;

                break;

            case BackgroundType.SolidColor:
                camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                camData.backgroundColorHDR =
 new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a);

                break;

            case BackgroundType.Transparent:
                camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                camData.backgroundColorHDR = new Color(0, 0, 0, 0);

                break;

            case BackgroundType.Image:
                camData.clearColorMode = HDAdditionalCameraData.ClearColorMode.Color;
                camData.backgroundColorHDR = new Color(0, 0, 0, 0);
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        if (backgroundType is BackgroundType.None)
            return;

        ScreenshotUtility.WriteColorBufferFormat("    colorBufferFormat: 48", out colorBuffer);
    }
    
    private void ResetCameraData(
        HDAdditionalCameraData camData,
        HDAdditionalCameraData.ClearColorMode colorMode,
        Color bgColor,
        string colorBuffer)
    {
        camData.clearColorMode = colorMode;
        camData.backgroundColorHDR = bgColor;

        if (backgroundType is BackgroundType.None)
            return;

        ScreenshotUtility.WriteColorBufferFormat(colorBuffer, out var _);
    }
#endif

    #endregion
}

}
