#if USING_HDRP
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
#endif

using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[assembly: InternalsVisibleTo("tiogiras.megapint.editor")]

namespace MegaPint
{

/// <summary> Utility class for the screenshot package </summary>
internal static class ScreenshotUtility
{
    #region Public Methods

    /// <summary> Get the Gcd </summary>
    /// <param name="a"> First value </param>
    /// <param name="b"> Second value </param>
    /// <returns> Gcd of a and b </returns>
    public static int Gcd(ulong a, ulong b)
    {
        while (a != 0 && b != 0)
        {
            if (a > b)
                a %= b;
            else
                b %= a;
        }

        return Convert.ToInt32(a | b);
    }

    /// <summary> Render a camera </summary>
    /// <param name="camera"> Targeted camera </param>
    /// <param name="width"> Width of the rendered texture </param>
    /// <param name="height"> Height of the rendered texture </param>
    /// <param name="depth"> Depth of the rendered texture </param>
    /// <returns> Rendered image </returns>
    public static Texture2D RenderCamera(Camera camera, int width, int height, int depth)
    {
        RenderTexture cameraTarget = camera.targetTexture;

        var myRenderTarget = new RenderTexture(width, height, depth);

        camera.targetTexture = myRenderTarget;

        RenderTexture activeRenderTexture = RenderTexture.active;
        RenderTexture.active = myRenderTarget;

        camera.Render();

        var image = new Texture2D(myRenderTarget.width, myRenderTarget.height);
        image.ReadPixels(new Rect(0, 0, myRenderTarget.width, myRenderTarget.height), 0, 0);
        image.Apply();

        RenderTexture.active = activeRenderTexture;
        camera.targetTexture = cameraTarget;

        return image;
    }

    /// <summary> Save a texture </summary>
    /// <param name="texture"> Targeted texture </param>
    /// <param name="filePath"> Export path </param>
    public static void SaveTexture(Texture2D texture, string filePath)
    {
        var bytes = texture.EncodeToPNG();
        var stream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
        var writer = new BinaryWriter(stream);

        foreach (var t in bytes)
            writer.Write(t);

        writer.Close();
        stream.Close();

#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }

    /// <summary> Write the color buffer format </summary>
    /// <param name="newColorBuffer"> New format </param>
    /// <param name="oldColorBuffer"> Output of the old format </param>
    public static void WriteColorBufferFormat(string newColorBuffer, out string oldColorBuffer)
    {
        oldColorBuffer = "";

#if USING_HDRP
        RenderPipelineAsset pipelineAsset = QualitySettings.renderPipeline;

        if (pipelineAsset is not HDRenderPipelineAsset)
            return;

#if UNITY_EDITOR
        var pipelineAssetPath = AssetDatabase.GetAssetPath(pipelineAsset);

        AssetDatabase.SaveAssetIfDirty(pipelineAsset);

        var lines = File.ReadAllLines(pipelineAssetPath);

        for (var i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("colorBufferFormat:"))
                continue;

            oldColorBuffer = lines[i];
            lines[i] = newColorBuffer;

            break;
        }

        File.WriteAllLines(pipelineAssetPath, lines);

        AssetDatabase.SaveAssetIfDirty(pipelineAsset);
        AssetDatabase.Refresh();
#endif
#endif
    }

    #endregion
}

}
