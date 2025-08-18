#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MegaPint.Editor.Scripts.PackageManager.Packages;
using MegaPint.Editor.Scripts.Windows;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MegaPint.Editor.Scripts
{

/// <summary> Partial class used to store MenuItems </summary>
internal static partial class ContextMenu
{
    public static class Screenshot
    {
        public static Action <int> onCaptureNow;

        private static readonly MenuItemSignature s_captureNowSignature = new()
        {
            package = PackageKey.Screenshot, signature = "Capture Now"
        };

        private static readonly MenuItemSignature s_shortcutCaptureSignature = new()
        {
            package = PackageKey.Screenshot, signature = "Shortcut Capture"
        };

        private static readonly MenuItemSignature s_windowCaptureSignature = new()
        {
            package = PackageKey.Screenshot, signature = "Window Capture"
        };

        #region Private Methods

        [MenuItem(MenuItemPackages + "/Screenshot/Capture Now _F12", false, 100)]
        private static void CaptureNow()
        {
#if USING_URP && USING_HDRP
        Debug.LogWarning("Cannot render CameraCaptures while more than one renderPipeline is installed.");
        onCaptureNow?.Invoke(0);

        return;
#endif

            List <CameraCapture> cams = Object.FindObjectsByType <CameraCapture>(FindObjectsSortMode.InstanceID).ToList();

            if (cams.Count == 0)
            {
                onCaptureNow?.Invoke(0);

                return;
            }

            List <CameraCapture> activeCams = cams.Where(cam => cam.listenToShortcut).ToList();

            if (activeCams is not {Count: > 0})
            {
                EditorUtility.DisplayDialog(
                    "Missing Cameras",
                    "No CameraCapture components selected for rendering.",
                    "Ok");

                onCaptureNow?.Invoke(0);

                return;
            }

            foreach (CameraCapture cam in activeCams)
            {
                var timestamp = $"{DateTime.Now:yy-MM-dd})({DateTime.Now:HH-mm-ss}";
                var camName = $"/{cam.gameObject.name}[{cam.GetHashCode()}]({timestamp}).png";
                var path = $"{cam.lastPath}{camName}";
                
                cam.RenderAndSave(path);
            }

            Debug.Log($"{activeCams.Count} CameraCapture components rendered.");

            onCaptureNow?.Invoke(activeCams.Count);
            onMenuItemInvoked?.Invoke(s_captureNowSignature);
        }

        [MenuItem(MenuItemPackages + "/Screenshot/Shortcut Capture", false, 121)]
        private static void OpenShortcutCapture()
        {
            TryOpen <ShortcutCapture>(false, s_shortcutCaptureSignature);
        }

        [MenuItem(MenuItemPackages + "/Screenshot/Window Capture", false, 120)]
        private static void OpenWindowCapture()
        {
            TryOpen <WindowCapture>(false, s_windowCaptureSignature);
        }

        #endregion
    }
}

}
#endif
