using System.Runtime.InteropServices;

namespace PlexSleepGuard.Setup;

internal static class UserNotification
{
    private const uint InformationIcon = 0x40;

    public static void ShowInformation(string message)
    {
        if (OperatingSystem.IsWindows())
        {
            _ = MessageBox(IntPtr.Zero, message, "PlexSleepGuard", InformationIcon);
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);
}
