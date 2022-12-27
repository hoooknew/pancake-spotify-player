using pancake.lib;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using static pancake.lib.Settings;
using System.Windows.Media.Media3D;
using static pancake.ui.WindowPosition.NativeMethods;

namespace pancake.ui
{
    public static class WindowPosition
    {
        public static string GetSave(DependencyObject obj)
            => (string)obj.GetValue(SaveProperty);

        public static void SetSave(DependencyObject obj, string value)
            => obj.SetValue(SaveProperty, value);
        
        public static readonly DependencyProperty SaveProperty =
            DependencyProperty.RegisterAttached("Save", typeof(string), typeof(Window), new PropertyMetadata("", new PropertyChangedCallback(SaveChangedCallback)));

        private static void SaveChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window w)
            {
                var newValue = e.NewValue as string;
                if (string.IsNullOrEmpty(newValue))
                {
                    w.Initialized -= LoadPosition;
                    w.Closing -= SavePosition;
                }
                else
                {
                    w.Initialized += LoadPosition;
                    w.Closing += SavePosition;                    
                }
            }
        }

        private static void SavePosition(object? sender, EventArgs e)
        {
            if (sender is Window w && GetSave(w) is string settingName)
            {
                Settings.Instance.SetValue(settingName, w.GetWindowPosition());
                Settings.Instance.Save();
            }
        }

        private static void LoadPosition(object? sender, EventArgs e)
        {
            if (sender is Window w && GetSave(w) is string settingName)
            {
                var newPos = Settings.Instance.GetValue(settingName) as Settings.Rect;
                if (newPos!= null)
                {
                    var oldPos = w.GetWindowPosition();                    

                    if (newPos != null)
                    {
                        w.WindowState = WindowState.Normal;
                        w.SetWindowPosition(newPos);                        

                        if (!IsOnScreen(w))
                            w.SetWindowPosition(oldPos);
                    }
                }
            }
        }

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

        private static bool IsOnScreen(this Window w)
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

        private static Settings.Rect GetWindowPosition(this Window w)
                => new Settings.Rect(w.RestoreBounds.Left, w.RestoreBounds.Top, w.RestoreBounds.Width, w.RestoreBounds.Height);

        private static void SetWindowPosition(this Window w, Settings.Rect wp)
        {
            w.Left = wp.Left;
            w.Top = wp.Top;
            w.Width = wp.Width;
            w.Height = wp.Height;
        }
    }
}
