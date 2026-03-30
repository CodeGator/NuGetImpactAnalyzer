using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;

namespace NuGetImpactAnalyzer.Infrastructure;

/// <summary>
/// Multi-monitor–aware window positioning (avoids <see cref="WindowStartupLocation.CenterScreen"/>, which uses the primary display only).
/// </summary>
internal static class WindowPlacement
{
    /// <summary>
    /// Centers <paramref name="window"/> in the working area of the monitor nearest the mouse cursor.
    /// Call from a <see cref="FrameworkElement.Loaded"/> handler so <see cref="Window.ActualWidth"/> / <see cref="Window.ActualHeight"/> are valid.
    /// </summary>
    public static void CenterOnMonitorContainingCursor(Window window)
    {
        if (!GetCursorPos(out var pt))
        {
            return;
        }

        var hMonitor = MonitorFromPoint(pt, MONITOR_DEFAULTTONEAREST);
        var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
        if (hMonitor == IntPtr.Zero || !GetMonitorInfo(hMonitor, ref mi))
        {
            return;
        }

        var work = mi.rcWork;
        var source = PresentationSource.FromVisual(window);
        if (source?.CompositionTarget != null)
        {
            var m = source.CompositionTarget.TransformFromDevice;
            var topLeft = m.Transform(new System.Windows.Point(work.Left, work.Top));
            var bottomRight = m.Transform(new System.Windows.Point(work.Right, work.Bottom));
            var workW = bottomRight.X - topLeft.X;
            var workH = bottomRight.Y - topLeft.Y;
            window.Left = topLeft.X + (workW - window.ActualWidth) / 2;
            window.Top = topLeft.Y + (workH - window.ActualHeight) / 2;
        }
        else
        {
            var w = work.Right - work.Left;
            var h = work.Bottom - work.Top;
            window.Left = work.Left + (w - window.ActualWidth) / 2;
            window.Top = work.Top + (h - window.ActualHeight) / 2;
        }
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
