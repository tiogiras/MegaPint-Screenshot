#if UNITY_EDITOR
#if UNITY_INCLUDE_TESTS
using MegaPint.Editor.Scripts.Tests.Utility;
using MegaPint.Editor.Scripts.Windows;
using NUnit.Framework;

namespace MegaPint.Editor.Scripts.Tests
{

/// <summary> Unit tests regarding the menuItems of the package </summary>
internal class MenuItemTests
{
    #region Tests

    [Test]
    public void CaptureNow()
    {
        TestsUtility.ValidateMenuItemLink(Constants.Screenshot.Links.CaptureNow, null);
    }

    [Test]
    public void ShortcutCapture()
    {
        TestsUtility.ValidateMenuItemLink(Constants.Screenshot.Links.ShortcutCapture, typeof(ShortcutCapture));
    }

    [Test]
    public void WindowCapture()
    {
        TestsUtility.ValidateMenuItemLink(Constants.Screenshot.Links.WindowCapture, typeof(WindowCapture));
    }

    #endregion
}

}
#endif
#endif
