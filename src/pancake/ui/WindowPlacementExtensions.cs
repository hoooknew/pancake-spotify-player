using Newtonsoft.Json;
using pancake.lib;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace pancake.ui
{
    /// <summary>
    /// Thank you to danielchalmers/DesktopClock for helping me find GetWindowPlacement() and SetWindowPlacement()
    /// </summary>
    /// <see cref="https://github.com/danielchalmers/WpfWindowPlacement/blob/master/WpfWindowPlacement/NativeMethods.cs"/>
    /// <seealso cref="https://github.com/danielchalmers/DesktopClock"/>
    public static class WindowPlacement
    {        
        private record WindowPlacementData(NativeMethods.WINDOWPLACEMENT location, DockableWindows.DockedPosition? docking);

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
                    w.IsVisibleChanged -= LoadPosition;
                    w.Closing -= SavePosition;
                }
                else
                {
                    w.IsVisibleChanged += LoadPosition;
                    w.Closing += SavePosition;                    
                }
            }
        }

        private static void SavePosition(object? sender, EventArgs e)
        {
            if (sender is Window w && GetSave(w) is string settingName)
            {
                string data = GetWindowPlacement(w);

                Settings.Instance.SetValue(settingName, data);
                Settings.Instance.Save();
            }
        }  

        private static void LoadPosition(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Window w && GetSave(w) is string settingName)
            {
                var data = Settings.Instance.GetValue(settingName) as string;

                SetWindowPlacement(w, data);
            }
        }

        private static string GetWindowPlacement(Window w)
        {
            var hwnd = new WindowInteropHelper(w).EnsureHandle();

            var location = new NativeMethods.WINDOWPLACEMENT();
            NativeMethods.GetWindowPlacement(hwnd, ref location);

            var data = new WindowPlacementData(location, DockableWindows.GetDockedPosition(w));

            return JsonConvert.SerializeObject(data);
        }

        private static bool SetWindowPlacement(Window w, string? data)
        {
            if (data != null)
            {
                var wp = JsonConvert.DeserializeObject<WindowPlacementData>(data)!;

                var hwnd = new WindowInteropHelper(w).EnsureHandle();

                var location = wp.location;

                if (NativeMethods.SetWindowPlacement(hwnd, ref location))
                {
                    DockableWindows.SetDockedPosition(w, wp.docking);
                    return true;
                }
            }

            return false;
        }

        #region NativeMethods
        private static class NativeMethods
        {
            //http://www.pinvoke.net/default.aspx/user32/ShowState.html
            public enum ShowState : int
            {
                SW_HIDE = 0,
                SW_SHOWNORMAL = 1,
                SW_NORMAL = 1,
                SW_SHOWMINIMIZED = 2,
                SW_SHOWMAXIMIZED = 3,
                SW_MAXIMIZE = 3,
                SW_SHOWNOACTIVATE = 4,
                SW_SHOW = 5,
                SW_MINIMIZE = 6,
                SW_SHOWMINNOACTIVE = 7,
                SW_SHOWNA = 8,
                SW_RESTORE = 9,
                SW_SHOWDEFAULT = 10,
                SW_FORCEMINIMIZE = 11,
                SW_MAX = 11
            }

            /// <summary>
            /// Contains information about the placement of a window on the screen.
            /// </summary>
            /// <see cref="http://www.pinvoke.net/default.aspx/Structures/WINDOWPLACEMENT.html"/>
            [Serializable]
            [StructLayout(LayoutKind.Sequential)]
            internal struct WINDOWPLACEMENT
            {
                /// <summary>
                /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
                /// <para>
                /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
                /// </para>
                /// </summary>
                public int Length;

                /// <summary>
                /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
                /// </summary>
                public int Flags;

                /// <summary>
                /// The current show state of the window.
                /// </summary>
                public ShowState ShowState;

                /// <summary>
                /// The coordinates of the window's upper-left corner when the window is minimized.
                /// </summary>
                public POINT MinPosition;

                /// <summary>
                /// The coordinates of the window's upper-left corner when the window is maximized.
                /// </summary>
                public POINT MaxPosition;

                /// <summary>
                /// The window's coordinates when the window is in the restored position.
                /// </summary>
                public RECT NormalPosition;

                /// <summary>
                /// Gets the default (empty) value.
                /// </summary>
                public static WINDOWPLACEMENT Default
                {
                    get
                    {
                        WINDOWPLACEMENT result = new WINDOWPLACEMENT();
                        result.Length = Marshal.SizeOf(result);
                        return result;
                    }
                }
            }

            //http://www.pinvoke.net/default.aspx/Structures/RECT.html
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left, Top, Right, Bottom;

                public RECT(int left, int top, int right, int bottom)
                {
                    Left = left;
                    Top = top;
                    Right = right;
                    Bottom = bottom;
                }

                public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

                public int X
                {
                    get { return Left; }
                    set { Right -= (Left - value); Left = value; }
                }

                public int Y
                {
                    get { return Top; }
                    set { Bottom -= (Top - value); Top = value; }
                }

                public int Height
                {
                    get { return Bottom - Top; }
                    set { Bottom = value + Top; }
                }

                public int Width
                {
                    get { return Right - Left; }
                    set { Right = value + Left; }
                }

                public System.Drawing.Point Location
                {
                    get { return new System.Drawing.Point(Left, Top); }
                    set { X = value.X; Y = value.Y; }
                }

                public System.Drawing.Size Size
                {
                    get { return new System.Drawing.Size(Width, Height); }
                    set { Width = value.Width; Height = value.Height; }
                }

                public static implicit operator System.Drawing.Rectangle(RECT r)
                {
                    return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
                }

                public static implicit operator RECT(System.Drawing.Rectangle r)
                {
                    return new RECT(r);
                }

                public static bool operator ==(RECT r1, RECT r2)
                {
                    return r1.Equals(r2);
                }

                public static bool operator !=(RECT r1, RECT r2)
                {
                    return !r1.Equals(r2);
                }

                public bool Equals(RECT r)
                {
                    return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
                }

                public override bool Equals(object? obj)
                {
                    if (obj is RECT)
                        return Equals((RECT)obj);
                    else if (obj is System.Drawing.Rectangle)
                        return Equals(new RECT((System.Drawing.Rectangle)obj));
                    return false;
                }

                public override int GetHashCode()
                {
                    return ((System.Drawing.Rectangle)this).GetHashCode();
                }

                public override string ToString()
                {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
                }
            }

            //http://www.pinvoke.net/default.aspx/Structures/POINT.html
            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;

                public POINT(int x, int y)
                {
                    this.X = x;
                    this.Y = y;
                }

                public static implicit operator System.Drawing.Point(POINT p)
                {
                    return new System.Drawing.Point(p.X, p.Y);
                }

                public static implicit operator POINT(System.Drawing.Point p)
                {
                    return new POINT(p.X, p.Y);
                }

                public override string ToString()
                {
                    return $"X: {X}, Y: {Y}";
                }
            }

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool SetWindowPlacement(IntPtr hWnd, [In] ref WINDOWPLACEMENT lpwndpl);
        }
        #endregion
    }
}
