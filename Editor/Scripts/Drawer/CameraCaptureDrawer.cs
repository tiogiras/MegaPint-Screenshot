#if UNITY_EDITOR
using System;
using MegaPint.Editor.Scripts.GUI;
using MegaPint.Editor.Scripts.GUI.Factories.Structure;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using GUIUtility = MegaPint.Editor.Scripts.GUI.Utility.GUIUtility;
#if USING_URP
using UnityEngine.Rendering.Universal;
#endif

#if UNITY_EDITOR
namespace MegaPint.Editor.Scripts.Drawer
{

[CustomEditor(typeof(CameraCapture))]
internal class CameraCaptureDrawer : UnityEditor.Editor
{
    public static Action <string> onCameraCaptureRendered;
    public static Action <string> onCameraCaptureExported;

    private readonly string _basePath = Constants.Screenshot.UserInterface.CameraCapture;

#if USING_URP && USING_HDRP
    private readonly string _pathError = Constants.Screenshot.UserInterface.MultiplePipelines;
#endif

    private ColorField _backgroundColor;
    private ObjectField _backgroundImage;

    private EnumField _backgroundType;

    private Button _btnExportPath;
    private Button _btnRender;
    private Button _btnSave;

#if USING_URP
    private UniversalAdditionalCameraData _camData;
#endif

    private DropdownField _depth;
    private IntegerField _height;
    private Label _imageResolution;

    private DropdownField _imageType;
    private Label _path;
    private FloatField _pixelPerUnit;
    private AspectRatioPanel _preview;

    private Texture2D _render;

    private CameraCapture _target;

    private VisualElement _transparencyHint;
    private VisualElement _transparencyHintHdrp;

    private VisualElement _rootTarget;

#if USING_HDRP
    private IntegerField _exposureTime;
#endif

    private IntegerField _width;

    #region Public Methods

    public override VisualElement CreateInspectorGUI()
    {
#if USING_URP && USING_HDRP
        return Resources.Load <VisualTreeAsset>(_pathError).Instantiate();
#endif
        var template = Resources.Load <VisualTreeAsset>(_basePath);
        VisualElement root = GUIUtility.Instantiate(template);

        _rootTarget = root;

        root.style.flexGrow = 1f;
        root.style.flexShrink = 1f;

        _preview = root.Q <AspectRatioPanel>("Preview");
        _btnRender = root.Q <Button>("BTN_Render");
        _btnSave = root.Q <Button>("BTN_Save");

        _btnExportPath = root.Q <Button>("BTN_ExportPath");
        _path = root.Q <Label>("Path");

        _width = root.Q <IntegerField>("Width");
        _height = root.Q <IntegerField>("Height");
        _depth = root.Q <DropdownField>("Depth");

        _backgroundType = root.Q <EnumField>("BackgroundType");
        _backgroundColor = root.Q <ColorField>("BackgroundColor");
        _backgroundImage = root.Q <ObjectField>("BackgroundImage");
        _imageResolution = root.Q <Label>("ImageResolution");

        _imageType = root.Q <DropdownField>("ImageType");
        _pixelPerUnit = root.Q <FloatField>("PixelPerUnit");

        _transparencyHint = root.Q <VisualElement>("TransparencyHint");

        _transparencyHintHdrp = root.Q <VisualElement>("TransparencyHintHDRP");

        _target = (CameraCapture)target;

#if USING_URP
        _camData = _target.GetComponent <Camera>().GetUniversalAdditionalCameraData();
#endif

#if USING_HDRP
        _exposureTime = root.Q <IntegerField>("ExposureTime");
        _backgroundColor.hdr = true;
#endif

        _width.value = _target.width;
        _height.value = _target.height;
        _depth.value = _target.depth.ToString();
        _backgroundType.value = _target.backgroundType;
        _backgroundColor.value = _target.backgroundColor;
        _backgroundImage.value = _target.backgroundImage;
        _imageType.value = _target.imageType;
        _pixelPerUnit.value = _target.pixelPerUnit;

#if USING_HDRP
        _exposureTime.value = _target.exposureTime;
#endif

        _btnSave.style.display = DisplayStyle.None;

        UpdateTransparencyHint();
        UpdateTransparencyHintHdrp();

        UpdatePath();
        UpdateBackgroundColor();
        UpdateBackgroundImage();

        RegisterCallbacks();

        root.schedule.Execute(
            () =>
            {
                root.parent.styleSheets.Add(StyleSheetValues.BaseStyleSheet);
                root.parent.styleSheets.Add(StyleSheetValues.AttributesStyleSheet);

                GUIUtility.ApplyRootElementTheme(root.parent);

                root.parent.AddToClassList(StyleSheetClasses.Background.Color.Tertiary);
            });

        return root;
    }

    #endregion

    #region Private Methods

    /// <summary> Change the export path </summary>
    private void ChangePath()
    {
        var path = EditorUtility.OpenFolderPanel("Choose Path", _target.lastPath, "");

        if (string.IsNullOrEmpty(path))
            return;

        if (path.IsPathInProject(out var pathInProject))
        {
            _target.lastPath = pathInProject;
            UpdatePath();

            ApplyModifiedProperties();
        }
        else
        {
            if (!SaveValues.Screenshot.ExternalExport)
            {
                EditorUtility.DisplayDialog(
                    "Folder not in project",
                    "The folder must be within the Assets folder!\nYou can enable project external export in the screenshot settings.",
                    "ok");

                return;
            }

            _target.lastPath = path;
            UpdatePath();

            ApplyModifiedProperties();
        }
    }

    /// <summary> Applies all changes to the serializedObject </summary>
    private void ApplyModifiedProperties()
    {
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(serializedObject.targetObject);
    }

    /// <summary> Register all callbacks </summary>
    private void RegisterCallbacks()
    {
        _btnRender.clicked += Render;

        _btnSave.clicked += Save;

        _width.RegisterValueChangedCallback(
            evt =>
            {
                if (evt.newValue < 0)
                {
                    _width.SetValueWithoutNotify(0);
                    _target.width = 0;

                    ApplyModifiedProperties();

                    return;
                }

                _target.width = evt.newValue;

                ApplyModifiedProperties();
            });

        _height.RegisterValueChangedCallback(
            evt =>
            {
                if (evt.newValue < 0)
                {
                    _height.SetValueWithoutNotify(0);
                    _target.height = 0;

                    ApplyModifiedProperties();

                    return;
                }

                _target.height = evt.newValue;

                ApplyModifiedProperties();
            });

#if USING_HDRP
        _exposureTime.RegisterValueChangedCallback(
            evt =>
            {
                if (evt.newValue < 0)
                {
                    _exposureTime.SetValueWithoutNotify(0);
                    _target.exposureTime = 0;

                    ApplyModifiedProperties();

                    return;
                }

                _target.exposureTime = evt.newValue;

                ApplyModifiedProperties();
            });
#endif

        _depth.RegisterValueChangedCallback(
            evt =>
            {
                _target.depth = int.Parse(evt.newValue);
                ApplyModifiedProperties();
            });

        _backgroundType.RegisterValueChangedCallback(
            evt =>
            {
                _target.backgroundType = (BackgroundType)evt.newValue;
                UpdateBackgroundColor();
                UpdateBackgroundImage();
                UpdateTransparencyHint();

                ApplyModifiedProperties();
            });

        _backgroundColor.RegisterValueChangedCallback(
            evt =>
            {
                _target.backgroundColor = evt.newValue;
                ApplyModifiedProperties();
            });

        _imageType.RegisterValueChangedCallback(
            evt =>
            {
                _target.imageType = evt.newValue;
                UpdateBackgroundImage();

                ApplyModifiedProperties();
            });

        _pixelPerUnit.RegisterValueChangedCallback(
            evt =>
            {
                if (evt.newValue < 0)
                    _pixelPerUnit.SetValueWithoutNotify(0);

                _target.pixelPerUnit = evt.newValue;
                ApplyModifiedProperties();
            });

        _backgroundImage.RegisterValueChangedCallback(
            evt =>
            {
                _target.backgroundImage = (Sprite)evt.newValue;
                UpdateBackgroundImage();

                ApplyModifiedProperties();
            });

        _btnExportPath.clicked += ChangePath;
    }

    /// <summary> Save the rendered image </summary>
    private void Save()
    {
        var path = EditorUtility.SaveFilePanel(
            "Save Screenshot",
            _target.lastPath,
            "",
            "png");

        if (string.IsNullOrEmpty(path))
            return;

        if (path.IsPathInProject(out var pathInProject))
        {
            _target.Save(_render, pathInProject);
            onCameraCaptureExported?.Invoke(_target.gameObject.name);

            return;
        }

        if (!SaveValues.Screenshot.ExternalExport)
        {
            EditorUtility.DisplayDialog(
                "Path not in project",
                "The path must be within the Assets folder!\nYou can enable project external export in the screenshot settings.",
                "ok");

            return;
        }

        onCameraCaptureExported?.Invoke(_target.gameObject.name);
        _target.Save(_render, path);

        UpdatePath();
    }

    /// <summary> Render the camera </summary>
    private async void Render()
    {
        var width = _target.width;
        var height = _target.height;

        var gcd = ScreenshotUtility.Gcd((ulong)width, (ulong)height);
        
        _render = await _target.Render();

        _preview.style.backgroundImage = _render;
        _preview.aspectRatioX = width / gcd;
        _preview.aspectRatioY = height / gcd;
        _preview.FitToParent();

        _btnSave.style.display = DisplayStyle.Flex;

        onCameraCaptureRendered?.Invoke(_target.gameObject.name);
    }

    /// <summary> Update the background color </summary>
    private void UpdateBackgroundColor()
    {
        _backgroundColor.style.display = _target.backgroundType == BackgroundType.SolidColor
            ? DisplayStyle.Flex
            : DisplayStyle.None;
    }

    /// <summary> Update the background image </summary>
    private void UpdateBackgroundImage()
    {
        var isImage = _target.backgroundType == BackgroundType.Image;
        _backgroundImage.style.display = isImage ? DisplayStyle.Flex : DisplayStyle.None;
        _imageType.style.display = isImage ? DisplayStyle.Flex : DisplayStyle.None;

        Sprite image = _target.backgroundImage;

        _imageResolution.style.display =
            isImage && image != null ? DisplayStyle.Flex : DisplayStyle.None;

        _pixelPerUnit.style.display = isImage && _target.imageType.Equals("Tiled")
            ? DisplayStyle.Flex
            : DisplayStyle.None;

        _imageResolution.text = _target.backgroundImage == null
            ? ""
            : $"{image.rect.width} x {image.rect.height}";
    }

    /// <summary> Update the export path </summary>
    private void UpdatePath()
    {
        _path.text = _target.lastPath;
        _path.tooltip = _target.lastPath;
    }

    /// <summary> Update the transparency hint </summary>
    private void UpdateTransparencyHint()
    {
#if USING_URP
        if (_target.backgroundType is not BackgroundType.None)
            _transparencyHint.style.display = _camData.renderPostProcessing ? DisplayStyle.Flex
                : DisplayStyle.None;
        else
            _transparencyHint.style.display = DisplayStyle.None;
#else
        _transparencyHint.style.display = DisplayStyle.None;
#endif
    }

    /// <summary> Update the transparency hint of the hdrp pipeline </summary>
    private void UpdateTransparencyHintHdrp()
    {
#if USING_HDRP
        _transparencyHintHdrp.style.display = DisplayStyle.Flex;
#else
        _transparencyHintHdrp.style.display = DisplayStyle.None;
#endif
    }

    #endregion
}

}
#endif
#endif
