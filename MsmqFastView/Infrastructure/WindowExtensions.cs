using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MsmqFastView.Infrastructure
{
    public static class WindowExtensions
    {
        public static WindowNativeMethods.WINDOWPLACEMENT GetPlacement(this Window window)
        {
            WindowNativeMethods.WINDOWPLACEMENT windowPlacement = new WindowNativeMethods.WINDOWPLACEMENT();
            windowPlacement.length = (uint)Marshal.SizeOf(typeof(WindowNativeMethods.WINDOWPLACEMENT));
            GCHandle lpwndpl = GCHandle.Alloc(windowPlacement, GCHandleType.Pinned);

            bool success = WindowNativeMethods.GetWindowPlacement(new WindowInteropHelper(window).Handle, ref windowPlacement);

            lpwndpl.Free();

            if (!success)
            {
                throw new InvalidOperationException("Geting window placement failed.");
            }

            return windowPlacement;
        }

        public static void SetPlacement(this Window window, WindowNativeMethods.WINDOWPLACEMENT windowPlacement)
        {
            windowPlacement.length = (uint)Marshal.SizeOf(typeof(WindowNativeMethods.WINDOWPLACEMENT));
            windowPlacement.flags = 0;
            GCHandle lpwndpl = GCHandle.Alloc(windowPlacement, GCHandleType.Pinned);

            bool success = WindowNativeMethods.SetWindowPlacement(new WindowInteropHelper(window).Handle, ref windowPlacement);

            lpwndpl.Free();

            if (!success)
            {
                throw new InvalidOperationException("Setting window placement failed.");
            }
        }
    }
}
