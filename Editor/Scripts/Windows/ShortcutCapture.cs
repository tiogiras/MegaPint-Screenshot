#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using MegaPint.Editor.Scripts.GUI.Utility;
using UnityEngine;
using UnityEngine.UIElements;
using GUIUtility = MegaPint.Editor.Scripts.GUI.Utility.GUIUtility;

namespace MegaPint.Editor.Scripts.Windows
{

/// <summary>
///     Window based on the <see cref="EditorWindowBase" /> to display and handle the shortcut capture
///     feature
/// </summary>
internal class ShortcutCapture : EditorWindowBase
{
    public static Action onOpen;
    public static Action onClose;

    public static Action onRefresh;
    public static Action <string, bool> onChangeState;

    private VisualTreeAsset _baseWindow;

    private Button _btnAll;
    private Button _btnNone;
    private Button _btnRefresh;

    private ListView _cameras;

    private List <CameraCapture> _cams = new();
    private VisualTreeAsset _listItem;
    private Label _placeholder;

    #region Public Methods

    public override EditorWindowBase ShowWindow()
    {
        titleContent.text = "Shortcut Capture";

        minSize = new Vector2(250, 300);

        onOpen?.Invoke();

        if (!SaveValues.Screenshot.ApplyPSShortcutWindow)
            return this;

        this.CenterOnMainWin(400, 500);
        SaveValues.Screenshot.ApplyPSShortcutWindow = false;

        return this;
    }

    #endregion

    #region Protected Methods

    protected override string BasePath()
    {
        return Constants.Screenshot.UserInterface.ShortcutCapture;
    }

    protected override void CreateGUI()
    {
        base.CreateGUI();

        VisualElement root = rootVisualElement;

        VisualElement content = GUIUtility.Instantiate(_baseWindow, root);

        content.style.flexGrow = 1;
        content.style.flexShrink = 1;

        _cameras = content.Q <ListView>("Cameras");
        _placeholder = content.Q <Label>("Placeholder");

        _btnAll = content.Q <Button>("BTN_All");
        _btnNone = content.Q <Button>("BTN_None");
        _btnRefresh = content.Q <Button>("BTN_Refresh");

        RegisterCallbacks();

        _cameras.makeItem += () => GUIUtility.Instantiate(_listItem);

        _cameras.bindItem += (element, i) =>
        {
            CameraCapture camera = _cams[i];
            element.Q <Label>("Name").text = camera.gameObject.name;

            var on = camera.listenToShortcut;
            var onButton = element.Q <Button>("BTN_On");
            var offButton = element.Q <Button>("BTN_Off");

            UpdateListItem(onButton, offButton, on);

            onButton.clickable = new Clickable(
                () =>
                {
                    camera.listenToShortcut = true;
                    onChangeState?.Invoke(camera.gameObject.name, true);

                    UpdateListItem(onButton, offButton, true);
                });

            offButton.clickable = new Clickable(
                () =>
                {
                    camera.listenToShortcut = false;
                    onChangeState?.Invoke(camera.gameObject.name, false);

                    UpdateListItem(onButton, offButton, false);
                });
        };

        RefreshCameras();
    }

    protected override bool LoadResources()
    {
        _baseWindow = Resources.Load <VisualTreeAsset>(BasePath());
        _listItem = Resources.Load <VisualTreeAsset>(Constants.Screenshot.UserInterface.ShortcutCaptureItem);

        return _baseWindow != null && _listItem != null;
    }

    protected override void RegisterCallbacks()
    {
        _btnRefresh.clicked += RefreshCameras;

        _btnAll.clicked += SelectAll;
        _btnNone.clicked += SelectNone;
    }

    protected override void UnRegisterCallbacks()
    {
        _btnRefresh.clicked -= RefreshCameras;

        _btnAll.clicked -= SelectAll;
        _btnNone.clicked -= SelectNone;

        onClose?.Invoke();
    }

    #endregion

    #region Private Methods

    /// <summary> Update the list item </summary>
    /// <param name="onButton"> On button </param>
    /// <param name="offButton"> Off button </param>
    /// <param name="on"> State of the toggle </param>
    private static void UpdateListItem(VisualElement onButton, VisualElement offButton, bool on)
    {
        GUIUtility.ToggleActiveButtonInGroup(on ? 0 : 1, onButton, offButton);
    }

    /// <summary> Refresh listed cameras </summary>
    private void RefreshCameras()
    {
        onRefresh?.Invoke();

        List <CameraCapture> cams = FindObjectsByType <CameraCapture>(FindObjectsSortMode.InstanceID).ToList();

        var hasCams = cams is {Count: > 0};

        if (hasCams)
        {
            _cams = cams;
            _cameras.itemsSource = _cams;
        }

        _cameras.style.display = hasCams ? DisplayStyle.Flex : DisplayStyle.None;
        _btnAll.style.display = hasCams ? DisplayStyle.Flex : DisplayStyle.None;
        _btnNone.style.display = hasCams ? DisplayStyle.Flex : DisplayStyle.None;

        _placeholder.style.display = hasCams ? DisplayStyle.None : DisplayStyle.Flex;
    }

    /// <summary> Select all cameras </summary>
    private void SelectAll()
    {
        SetAllCameraListeners(true);
    }

    /// <summary> Deselect all cameras </summary>
    private void SelectNone()
    {
        SetAllCameraListeners(false);
    }

    /// <summary> Set listening on all cameras </summary>
    /// <param name="listening"> new listening value </param>
    private void SetAllCameraListeners(bool listening)
    {
        if (_cams is not {Count: > 0})
            return;

        foreach (CameraCapture cam in _cams)
        {
            cam.listenToShortcut = listening;
            onChangeState?.Invoke(cam.gameObject.name, listening);
        }

        _cameras.RefreshItems();
    }

    #endregion
}

}
#endif
