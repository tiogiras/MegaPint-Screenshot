#if UNITY_EDITOR
using MegaPint.Editor.Scripts.Settings;

namespace MegaPint.Editor.Scripts
{

/// <summary> Partial class storing saveData values (Screenshot) </summary>
internal static partial class SaveValues
{
    public static class Screenshot
    {
        private static CacheValue <string> s_lastEditorWindowPath = new() {defaultValue = "Assets"};
        private static CacheValue <bool> s_externalExport = new() {defaultValue = false};

        private static CacheValue <bool> s_applyPSShortcutWindow = new() {defaultValue = false};
        private static CacheValue <bool> s_applyPSWindowCapture = new() {defaultValue = false};

        private static SettingsBase s_settings;

        public static string LastEditorWindowPath
        {
            get => ValueProperty.Get("lastEditorWindowPath", ref s_lastEditorWindowPath, _Settings);
            set => ValueProperty.Set("lastEditorWindowPath", value, ref s_lastEditorWindowPath, _Settings);
        }

        public static bool ExternalExport
        {
            get => ValueProperty.Get("externalExport", ref s_externalExport, _Settings);
            set => ValueProperty.Set("externalExport", value, ref s_externalExport, _Settings);
        }

        public static bool ApplyPSShortcutWindow
        {
            get => ValueProperty.Get("applyPS_ShortCutWindow", ref s_applyPSShortcutWindow, _Settings);
            set => ValueProperty.Set("applyPS_ShortCutWindow", value, ref s_applyPSShortcutWindow, _Settings);
        }

        public static bool ApplyPSWindowCapture
        {
            get => ValueProperty.Get("applyPS_WindowCapture", ref s_applyPSWindowCapture, _Settings);
            set => ValueProperty.Set("applyPS_WindowCapture", value, ref s_applyPSWindowCapture, _Settings);
        }

        private static SettingsBase _Settings
        {
            get
            {
                if (MegaPintMainSettings.Exists())
                    return s_settings ??= MegaPintMainSettings.instance.GetSetting("Screenshot");

                return null;
            }
        }
    }
}

}
#endif
