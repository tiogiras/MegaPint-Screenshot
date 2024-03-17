using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

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

    #region Public Methods

    public Texture2D Render()
    {
        var cam = GetComponent <Camera>();

        PrepareCamera(cam, out Color bgColor, out CameraClearFlags flags, out List <GameObject> destroy);

        Texture2D render = ScreenshotUtility.RenderCamera(cam, width, height, depth);

        ResetCamera(cam, bgColor, flags, destroy);

        return render;
    }

    public void RenderAndSave(string path)
    {
        Save(Render(), path);
    }

    public void Save(Texture2D texture, string path)
    {
        if (string.IsNullOrEmpty(path))
            return;

        lastPath = path[..path.LastIndexOf("/", StringComparison.Ordinal)];
        ScreenshotUtility.SaveTexture(texture, path);

        if (backgroundType == BackgroundType.Transparent)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }
    }

    #endregion

    #region Private Methods

    private void PrepareCamera(Camera cam, out Color bgColor, out CameraClearFlags flags, out List <GameObject> destroy)
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
               
/*#if USING_URP
                if (cam.GetUniversalAdditionalCameraData().renderPostProcessing)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0, 0, 0, 0);
                }
                else
                    cam.clearFlags = CameraClearFlags.Depth;
#else*/
                cam.clearFlags = CameraClearFlags.Depth;      
/*#endif*/
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

    private static void ResetCamera(Camera cam, Color bgColor, CameraClearFlags flags, IReadOnlyList <GameObject> destroy)
    {
        cam.backgroundColor = bgColor;
        cam.clearFlags = flags;

        for (var i = destroy.Count - 1; i >= 0; i--)
        {
            GameObject obj = destroy[i];
            DestroyImmediate(obj);
        }
    }

    #endregion
}
