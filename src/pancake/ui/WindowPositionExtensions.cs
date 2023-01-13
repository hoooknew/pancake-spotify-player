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
using System.Reflection.Metadata;
using System.Drawing;
using static pancake.ui.WindowPosition.AltNative;
using System.Threading;

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
                    w.Loaded -= LoadPosition;
                    w.Closing -= SavePosition;
                }
                else
                {
                    w.Loaded += LoadPosition;
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
                if (newPos != null && IsOnScreen(w, newPos.ToRect()))
                {
                    w.WindowState = WindowState.Normal;
                    w.SetWindowPosition(newPos);                        
                }
            }
        }

        private static System.Windows.Rect ToRect(this Settings.Rect rect)
            => new System.Windows.Rect(rect.Left, rect.Top, rect.Width, rect.Height);

        public static class AltNative
        {
            [DllImport("user32.dll")]
            static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
   EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

            // size of a device name string
            private const int CCHDEVICENAME = 32;

            [DllImport("user32.dll")]
            public static extern Boolean GetMonitorInfo(IntPtr hMonitor, MonitorInfoEx lpmi);

            //https://www.pinvoke.net/default.aspx/gdi32.getdevicecaps
            public enum DeviceCap
            {
                /// <summary>
                /// Horizontal width in pixels
                /// </summary>
                HORZRES = 8,
                /// <summary>
                /// Vertical height in pixels
                /// </summary>
                VERTRES = 10,                
                
                /// <summary>
                /// Scaling factor x
                /// </summary>
                SCALINGFACTORX = 114,
                /// <summary>
                /// Scaling factor y
                /// </summary>
                SCALINGFACTORY = 115,

                /// <summary>
                /// Vertical height of entire desktop in pixels
                /// </summary>
                DESKTOPVERTRES = 117,
                /// <summary>
                /// Horizontal width of entire desktop in pixels
                /// </summary>
                DESKTOPHORZRES = 118,

            }

            //https://stackoverflow.com/questions/3553575/doing-pinvoke-and-cant-get-the-right-hdc
            [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
            public static extern int GetDeviceCaps(IntPtr hDC, DeviceCap nIndex);


            [Serializable, StructLayout(LayoutKind.Sequential)]
            public struct RectStruct
            {
                public Int32 Left;
                public Int32 Top;
                public Int32 Right;
                public Int32 Bottom;


                public RectStruct(Int32 left, Int32 top, Int32 right, Int32 bottom)
                {
                    this.Left = left;
                    this.Top = top;
                    this.Right = right;
                    this.Bottom = bottom;
                }
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            public sealed class MonitorInfoEx
            {
                public Int32 Size = Marshal.SizeOf(typeof(MonitorInfoEx));
                public RectStruct Monitor;
                public RectStruct Work;
                public Int32 Flags;
            }

            //http://pinvoke.net/default.aspx/user32/EnumDisplayMonitors.html
            delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData);

            [DllImport("user32.dll", EntryPoint = "GetDC", SetLastError = true)]
            public static extern IntPtr GetDC(IntPtr hWnd);

            /// <summary>
            /// The struct that contains the display information
            /// </summary>
            public class DisplayInfo
            {
                public bool Primary { get; set; }
                public int ScreenHeight { get; set; }
                public int ScreenWidth { get; set; }                
                public RectStruct MonitorArea { get; set; }
                public RectStruct WorkArea { get; set; }
            }

            /// <summary>
            /// Collection of display information
            /// </summary>
            public class DisplayInfoCollection : List<DisplayInfo>
            {
            }

            /// <summary>
            /// Returns the number of Displays using the Win32 functions
            /// </summary>
            /// <returns>collection of Display Info</returns>
            public static DisplayInfoCollection GetDisplays()
            {
                DisplayInfoCollection col = new DisplayInfoCollection();

                EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    delegate (IntPtr hMonitor, IntPtr hdcMonitor, ref RectStruct lprcMonitor, IntPtr dwData)
                    {
                        MonitorInfoEx mi = new MonitorInfoEx();
                        mi.Size = 72;
                        bool success = GetMonitorInfo(hMonitor, mi);
                        if (success)
                        {                            
                            DisplayInfo di = new DisplayInfo();
                            di.ScreenWidth = mi.Monitor.Right - mi.Monitor.Left;
                            di.ScreenHeight = mi.Monitor.Bottom - mi.Monitor.Top;                            
                            di.MonitorArea = mi.Monitor;
                            di.WorkArea = mi.Work;
                            di.Primary = mi.Flags == 1;
                            col.Add(di);
                        }
                        return true;
                    }, IntPtr.Zero);
                return col;
            }
        }

        public static class NativeMethods
        {
            public const Int32 MONITOR_DEFAULTTOPRIMERTY = 0x00000001;
            public const Int32 MONITOR_DEFAULTTONEAREST = 0x00000002;

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetProcessDPIAware();

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

            //[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
            //public static extern int GetDeviceCaps(IntPtr hDC, DeviceCap nIndex);

            //public enum DeviceCap
            //{       
            //    HORZRES = 8,
            //    /// <summary>
            //    /// Vertical height in pixels
            //    /// </summary>
            //    VERTRES = 10,
            //    /// <summary>
            //    /// Number of bits per pixel
            //    /// </summary>
               
            //    /// <summary>
            //    /// Vertical height of entire desktop in pixels
            //    /// </summary>
            //    DESKTOPVERTRES = 117,
            //    /// <summary>
            //    /// Horizontal width of entire desktop in pixels
            //    /// </summary>
            //    DESKTOPHORZRES = 118,

            //}

            //public static double GetWindowsScreenScalingFactor(bool percentage = true)
            //{
            //    //Create Graphics object from the current windows handle
            //    Graphics GraphicsObject = Graphics.FromHwnd(IntPtr.Zero);
            //    //Get Handle to the device context associated with this Graphics object
            //    IntPtr DeviceContextHandle = GraphicsObject.GetHdc();
            //    //Call GetDeviceCaps with the Handle to retrieve the Screen Height
            //    int LogicalScreenHeight = GetDeviceCaps(DeviceContextHandle, (int)DeviceCap.VERTRES);
            //    int PhysicalScreenHeight = GetDeviceCaps(DeviceContextHandle, (int)DeviceCap.DESKTOPVERTRES);
            //    //Divide the Screen Heights to get the scaling factor and round it to two decimals
            //    double ScreenScalingFactor = Math.Round(PhysicalScreenHeight / (double)LogicalScreenHeight, 2);
            //    //If requested as percentage - convert it
            //    if (percentage)
            //    {
            //        ScreenScalingFactor *= 100.0;
            //    }
            //    //Release the Handle and Dispose of the GraphicsObject object
            //    GraphicsObject.ReleaseHdc(DeviceContextHandle);
            //    GraphicsObject.Dispose();
            //    //Return the Scaling Factor
            //    return ScreenScalingFactor;
            //}

        }      

        private static bool IsOnScreen(this Window w, System.Windows.Rect rect)
        {
            SetProcessDPIAware();

            var hwnd = new WindowInteropHelper(w).EnsureHandle();
            var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MONITOR_DEFAULTTONEAREST);

            var hdc = AltNative.GetDC(hwnd);
            var horzRes = AltNative.GetDeviceCaps(hdc, DeviceCap.HORZRES);
            var verRez = AltNative.GetDeviceCaps(hdc, DeviceCap.VERTRES);

            System.Windows.Rect? monitor_rect = GetMonitorRect(monitor);

            var x = AltNative.GetDisplays();

            return 
                monitor_rect != null && 
                (monitor_rect?.Contains(rect) ?? false);
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
