﻿using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class Utility
{
    #region Public Methods

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

    #endregion
}
