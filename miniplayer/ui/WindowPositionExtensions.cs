using miniplayer.lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using static miniplayer.lib.Settings;
using System.Windows.Media.Media3D;
using static miniplayer.ui.WindowPositionExtensions.NativeMethods;

namespace miniplayer.ui
{
    public static class WindowPositionExtensions
    {        
        public static class NativeMethods
        {
            public const Int32 MONITOR_DEFAULTTOPRIMERTY = 0x00000001;
            public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;


            [DllImport("user32.dll")]
            public static extern IntPtr MonitorFromWindow(IntPtr handle, Int32 flags);


            [DllImport("user32.dll")]
            public static extern Boolean GetMonitorInfo(IntPtr hMonitor, NativeMonitorInfo lpmi);


            [Serializable, StructLayout(LayoutKind.Sequential)]
            public struct NativeRectangle
            {
                public Int32 Left;
                public Int32 Top;
                public Int32 Right;
                public Int32 Bottom;


                public NativeRectangle(Int32 left, Int32 top, Int32 right, Int32 bottom)
                {
                    this.Left = left;
                    this.Top = top;
                    this.Right = right;
                    this.Bottom = bottom;
                }
            }


            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public sealed class NativeMonitorInfo
            {
                public Int32 Size = Marshal.SizeOf(typeof(NativeMonitorInfo));
                public NativeRectangle Monitor;
                public NativeRectangle Work;
                public Int32 Flags;
            }
        }

        

        public static void SaveLocation(this Window w)
        {
            Settings.Instance.WindowPosition = w.GetWindowPosition();
            Settings.Instance.Save();
        }

        public static void RestoreLocation(this Window w) 
        {
            var oldPos = w.GetWindowPosition();
            var newPos = Settings.Instance.WindowPosition;

            if (newPos != null)
            {
                w.SetWindowPosition(newPos);
                w.WindowState = WindowState.Normal;

                if (!IsOnScreen(w))
                    w.SetWindowPosition(oldPos);
            }
        }

        public static bool IsOnScreen(this Window w)
        {
            var hwnd = new WindowInteropHelper(w).EnsureHandle();
            var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);
            System.Windows.Rect? monitor_rect = GetMonitorRect(monitor);

            return 
                monitor_rect != null && 
                (monitor_rect?.Contains(w.RestoreBounds) ?? false);
        }

        private static System.Windows.Rect? GetMonitorRect(IntPtr monitor)
        {
            if (monitor != IntPtr.Zero)
            {
                var monitorInfo = new NativeMonitorInfo();
                NativeMethods.GetMonitorInfo(monitor, monitorInfo);

                return new System.Windows.Rect(
                    monitorInfo.Monitor.Left,
                    monitorInfo.Monitor.Top,
                    monitorInfo.Monitor.Right - monitorInfo.Monitor.Left,
                    monitorInfo.Monitor.Bottom - monitorInfo.Monitor.Top);
            }
            else
                return null;
        }

        public static Settings.Rect GetWindowPosition(this Window w)
                => new Settings.Rect(w.RestoreBounds.Left, w.RestoreBounds.Top, w.RestoreBounds.Width, w.RestoreBounds.Height);

        public static void SetWindowPosition(this Window w, Settings.Rect wp)
        {
            w.Left = wp.Left;
            w.Top = wp.Top;
            w.Width = wp.Width;
            w.Height = wp.Height;
        }
    }
}
