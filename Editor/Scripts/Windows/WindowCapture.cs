#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaPint.Editor.Scripts.GUI.Factories.Structure;
using MegaPint.Editor.Scripts.GUI.Utility;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using GUIUtility = MegaPint.Editor.Scripts.GUI.Utility.GUIUtility;

namespace MegaPint.Editor.Scripts.Windows
{

/// <summary>
///     Window based on the <see cref="EditorWindowBase" /> to display and handle the rendering of editor windows
/// </summary>
internal class WindowCapture : EditorWindowBase
{
    public static Action onOpen;
    public static Action onClose;

    public static Action onRefresh;
    public static Action <string> onRender;
    public static Action onExport;

    private readonly List <EditorWindow> _windowRefs = new();

    private VisualTreeAsset _baseWindow;

    private Button _btnRefresh;
    private Button _btnRender;
    private Button _btnSave;

    private AspectRatioPanel _preview;

    private Texture2D _render;

    private DropdownField _windows;

    #region Public Methods

    public override EditorWindowBase ShowWindow()
    {
        titleContent.text = "Window Capture";

        minSize = new Vector2(450, 350);

        onOpen?.Invoke();

        if (!SaveValues.Screenshot.ApplyPSWindowCapture)
            return this;

        this.CenterOnMainWin(700, 475);
        SaveValues.Screenshot.ApplyPSWindowCapture = false;

        return this;
    }

    #endregion

    #region Protected Methods

    protected override string BasePath()
    {
        return Constants.Screenshot.UserInterface.WindowCapture;
    }

    protected override async void CreateGUI()
    {
        base.CreateGUI();

        VisualElement root = rootVisualElement;

        VisualElement content = GUIUtility.Instantiate(_baseWindow, root);
        content.style.flexGrow = 1;
        content.style.flexShrink = 1;

        _preview = content.Q <AspectRatioPanel>("Preview");
        _windows = content.Q <DropdownField>("Windows");

        _btnRefresh = content.Q <Button>("BTN_Refresh");
        _btnRender = content.Q <Button>("BTN_Render");
        _btnSave = content.Q <Button>("BTN_Save");

        _btnRender.style.display = DisplayStyle.None;
        _btnSave.style.display = DisplayStyle.None;

        RegisterCallbacks();

        await Task.Delay(100);

        RefreshWindows();
    }

    protected override bool LoadResources()
    {
        _baseWindow = Resources.Load <VisualTreeAsset>(BasePath());

        return _baseWindow != null;
    }

    protected override void RegisterCallbacks()
    {
        _btnRefresh.clicked += RefreshWindows;
        _btnRender.clicked += Render;
        _btnSave.clicked += Save;

        _windows.RegisterValueChangedCallback(WindowSelected);
    }

    protected override void UnRegisterCallbacks()
    {
        _btnRefresh.clicked -= RefreshWindows;
        _btnRender.clicked -= Render;
        _btnSave.clicked -= Save;

        _windows.UnregisterValueChangedCallback(WindowSelected);

        onClose?.Invoke();
    }

    #endregion

    #region Private Methods

    /// <summary> Refresh listed windows </summary>
    private void RefreshWindows()
    {
        onRefresh?.Invoke();

        EditorWindow[] windows = Resources.FindObjectsOfTypeAll <EditorWindow>();

        _windowRefs.Clear();
        _windows.choices.Clear();

        foreach (EditorWindow window in windows)
        {
            _windowRefs.Add(window);
            _windows.choices.Add(window.titleContent.ToString());
        }

        _windows.SetValueWithoutNotify("");

        _btnRender.style.display = DisplayStyle.None;
        _btnSave.style.display = DisplayStyle.None;
        _preview.style.backgroundImage = null;
    }

    /// <summary> Render the selected window </summary>
    private async void Render()
    {
        EditorWindow target = _windowRefs[_windows.index];

        if (target == null)
            return;

        onRender?.Invoke(target.titleContent.text);

        if (!target.hasFocus)
        {
            target.Focus();
            await Task.Delay(100);
        }

        Vector2 pos = target.position.position;
        var width = target.position.width;
        var height = target.position.height;

        Color[] colors = InternalEditorUtility.ReadScreenPixel(pos, (int)width, (int)height);

        var result = new Texture2D((int)width, (int)height);
        result.SetPixels(colors);
        result.Apply();

        var gcd = ScreenshotUtility.Gcd((ulong)width, (ulong)height);

        _render = result;

        _preview.style.backgroundImage = _render;
        _preview.AspectRatioX = (int)width / gcd;
        _preview.AspectRatioY = (int)height / gcd;
        _preview.FitToParent();

        _btnSave.style.display = DisplayStyle.Flex;
    }

    /// <summary> Save the rendered image </summary>
    private void Save()
    {
        var path = EditorUtility.SaveFilePanel(
            "Save Screenshot",
            SaveValues.Screenshot.LastEditorWindowPath,
            "",
            "png");

        if (string.IsNullOrEmpty(path))
            return;

        if (!path.IsPathInProject(out var _) && !SaveValues.Screenshot.ExternalExport)
        {
            EditorUtility.DisplayDialog(
                "Path not in project",
                "The path must be within the Assets folder!\nYou can enable project external export in the screenshot settings.",
                "ok");

            return;
        }

        onExport?.Invoke();
        SaveValues.Screenshot.LastEditorWindowPath = path;
        ScreenshotUtility.SaveTexture(_render, path);
    }

    /// <summary> Callback on selecting a window </summary>
    /// <param name="evt"> Callback event </param>
    private void WindowSelected(ChangeEvent <string> evt)
    {
        _btnRender.style.display = DisplayStyle.Flex;
    }

    #endregion
}

}
#endif
