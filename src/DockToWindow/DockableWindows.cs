using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace DockToWindow
{
    internal class DockableWindows : IDisposable
    {
        #region NativeMethods
        private class NativeMethods
        {
            #region Constants
            public static readonly int WM_EXITSIZEMOVE = 0x232;
            #endregion

            [DllImport("dwmapi.dll")]
            public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;

                public bool BottomCloseToTop(NativeMethods.RECT other_r, double closeDist)
               => Math.Abs(other_r.Top - this.Bottom) < closeDist;

                public bool TopCloseToBottom(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Bottom - this.Top) < closeDist;

                public bool LeftCloseToRight(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Right - this.Left) < closeDist;

                public bool RightCloseToLeft(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Left - this.Right) < closeDist;

                public bool RightCloseToRight(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Right - this.Right) < closeDist;

                public bool LeftCloseToLeft(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Left - this.Left) < closeDist;

                public bool BottomCloseToBottom(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Bottom - this.Bottom) < closeDist;

                public bool TopCloseToTop(NativeMethods.RECT other_r, double closeDist)
                    => Math.Abs(other_r.Top - this.Top) < closeDist;


                public double Width
                    => this.Right - this.Left;
                public double Height
                    => this.Bottom - this.Top;


                public bool HasHorizontalOverlap(NativeMethods.RECT other_r)
                    => (this.Left >= other_r.Left && this.Left <= other_r.Right) ||
                       (this.Right >= other_r.Left && this.Right <= other_r.Right);

                public bool HasVerticalOverlap(NativeMethods.RECT other_r)
                   => (this.Top >= other_r.Top && this.Top <= other_r.Bottom) ||
                      (this.Bottom >= other_r.Top && this.Bottom <= other_r.Bottom);
            }

            [Flags]
            private enum DwmWindowAttribute : uint
            {
                DWMWA_NCRENDERING_ENABLED = 1,
                DWMWA_NCRENDERING_POLICY,
                DWMWA_TRANSITIONS_FORCEDISABLED,
                DWMWA_ALLOW_NCPAINT,
                DWMWA_CAPTION_BUTTON_BOUNDS,
                DWMWA_NONCLIENT_RTL_LAYOUT,
                DWMWA_FORCE_ICONIC_REPRESENTATION,
                DWMWA_FLIP3D_POLICY,
                DWMWA_EXTENDED_FRAME_BOUNDS,
                DWMWA_HAS_ICONIC_BITMAP,
                DWMWA_DISALLOW_PEEK,
                DWMWA_EXCLUDED_FROM_PEEK,
                DWMWA_CLOAK,
                DWMWA_CLOAKED,
                DWMWA_FREEZE_REPRESENTATION,
                DWMWA_LAST
            }

            public static NativeMethods.RECT GetExtendedFrameBounds(Window w)
                => NativeMethods.GetExtendedFrameBounds(DockableWindows.GetHandle(w));

            // The wpf sizes includes transparent space around the window. This function returns a
            // rectangle that describes the visual area painted much better.
            public static RECT GetExtendedFrameBounds(IntPtr hWnd)
            {
                int size = Marshal.SizeOf(typeof(RECT));
                DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, size);

                return rect;
            }

        }
        #endregion

        #region Handle Attached Property
        public static IntPtr GetHandle(DependencyObject obj)
        {
            var handle = (IntPtr)obj.GetValue(HandleProperty);
            if (handle == IntPtr.Zero && obj is Window w)
            {
                handle = new WindowInteropHelper(w).Handle;
                SetHandle(obj, handle);
            }

            return handle;
        }
        public static void SetHandle(DependencyObject obj, IntPtr value)
            => obj.SetValue(HandleProperty, value);
        public static readonly DependencyProperty HandleProperty =
            DependencyProperty.RegisterAttached("Handle", typeof(IntPtr), typeof(DockableWindows), new PropertyMetadata(IntPtr.Zero));
        #endregion

        [Flags]
        public enum Side
        {
            None = 0,
            Top_Out = 1,
            Bottom_Out = 2,
            Left_Out = 4,
            Right_Out = 8,
            Left_In = 16,
            Right_In = 32,
            Top_In = 64,
            Bottom_In = 128
        }

        const double DEFAULT_SNAP_DISTANCE = 20;

        private record DockedPosition(Point offset, Side sides);

        private Window _main;
        readonly double _snapDistance;
        private List<Window> _dockable;
        private Dictionary<Window, DockedPosition> _dockedWindows;

        public double SnapDistance => _snapDistance;

        public DockableWindows(Window main, double snapDistance = DEFAULT_SNAP_DISTANCE)
        {
            _main = main;
            _main.LocationChanged += MainWindow_SizeOrLocationChanged;
            _main.SizeChanged += MainWindow_SizeOrLocationChanged;

            _snapDistance = snapDistance;

            _dockable = new List<Window>();
            _dockedWindows = new Dictionary<Window, DockedPosition>();
        }

        public void AddDockable(Window w)
        {
            void AddHook(Window w, HwndSourceHook callback)
            {
                if (w.IsLoaded)
                {
                    var h = GetHandle(w);
                    HwndSource source = HwndSource.FromHwnd(h);
                    source.AddHook(callback);
                }
                else
                    w.Loaded += (s, e) => AddHook(w, callback);

            }

            if (!_dockable.Contains(w))
            {
                _dockable.Add(w);
                w.Closed += (s, e) => RemoveDockable((s as Window)!);
                AddHook(w, Dockable_WndProc);
            }
        }
        public void RemoveDockable(Window w)
        {
            void RemoveHook(Window w, HwndSourceHook callback)
            {
                var h = GetHandle(w);
                HwndSource source = HwndSource.FromHwnd(h);
                source.RemoveHook(callback);
            }

            if (_dockable.Contains(w))
            {
                RemoveHook(w, Dockable_WndProc);
                _dockable.Remove(w);
            }

            if (_dockedWindows.ContainsKey(w))
                _dockedWindows.Remove(w);
        }


        private DockedPosition? GetDockedPosition(Window dockable)
        {
            return _dockedWindows?.GetValueOrDefault(dockable) ?? null;
        }
        private void SetDockedPosition(Window dockable, DockedPosition? position)
        {
            if (position != null)
                _dockedWindows[dockable] = position;
            else
                _dockedWindows.Remove(dockable);
        }

        private Window? GetDockableWithHandle(IntPtr h)
            => _dockable.FirstOrDefault(w => GetHandle(w) == h);

        private void PositionDockedWindows()
        {
            var mainSize = NativeMethods.GetExtendedFrameBounds(_main);

            foreach (var docked in _dockedWindows.Keys)
            {
                var dockedSize = NativeMethods.GetExtendedFrameBounds(docked);

                var position = _dockedWindows[docked];
                double top;
                double left;

                if (position.sides.HasFlag(Side.Top_Out))
                    top = mainSize.Top - dockedSize.Height;
                else if (position.sides.HasFlag(Side.Bottom_Out))
                    top = mainSize.Top + mainSize.Height;
                else if (position.sides.HasFlag(Side.Top_In))
                    top = _main.Top;
                else if (position.sides.HasFlag(Side.Bottom_In))
                    top = _main.Top + (mainSize.Height - dockedSize.Height);
                else
                    top = _main.Top + position.offset.Y;


                if (position.sides.HasFlag(Side.Left_Out))
                    left = _main.Left - dockedSize.Width;
                else if (position.sides.HasFlag(Side.Right_Out))
                    left = _main.Left + mainSize.Width;
                else if (position.sides.HasFlag(Side.Left_In))
                    left = _main.Left;
                else if (position.sides.HasFlag(Side.Right_In))
                    left = _main.Left + (mainSize.Width - dockedSize.Width);
                else
                    left = _main.Left + position.offset.X;

                docked.Top = top;
                docked.Left = left;
            }
        }

        private IntPtr Dockable_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_EXITSIZEMOVE)
            {
                Window? dockable = GetDockableWithHandle(hwnd);

                if (dockable != null)
                {
                    var mainSize = NativeMethods.GetExtendedFrameBounds(_main);
                    var dockableSize = NativeMethods.GetExtendedFrameBounds(dockable);

                    var sides = Side.None;

                    if (dockableSize.BottomCloseToTop(mainSize, SnapDistance) &&
                        dockableSize.HasHorizontalOverlap(mainSize))
                        sides = Side.Top_Out;
                    else if (dockableSize.TopCloseToBottom(mainSize, SnapDistance) &&
                        dockableSize.HasHorizontalOverlap(mainSize))
                        sides = Side.Bottom_Out;
                    else if (dockableSize.LeftCloseToRight(mainSize, SnapDistance) &&
                        dockableSize.HasVerticalOverlap(mainSize))
                        sides = Side.Right_Out;
                    else if (dockableSize.RightCloseToLeft(mainSize, SnapDistance) &&
                        dockableSize.HasVerticalOverlap(mainSize))
                        sides = Side.Left_Out;

                    if (sides.HasFlag(Side.Top_Out) || sides.HasFlag(Side.Bottom_Out))
                    {
                        if (dockableSize.LeftCloseToLeft(mainSize, SnapDistance))
                            sides |= Side.Left_In;
                        else if (dockableSize.RightCloseToRight(mainSize, SnapDistance))
                            sides |= Side.Right_In;
                    }
                    else if (sides.HasFlag(Side.Left_Out) || sides.HasFlag(Side.Right_Out))
                    {
                        if (dockableSize.TopCloseToTop(mainSize, SnapDistance))
                            sides |= Side.Top_In;
                        else if (dockableSize.BottomCloseToBottom(mainSize, SnapDistance))
                            sides |= Side.Bottom_In;
                    }

                    if (sides == Side.None)
                    {
                        var position = GetDockedPosition(dockable);

                        if (position != null)
                        {
                            SetDockedPosition(dockable, null);
                            Debug.WriteLine("undocked");
                        }
                    }
                    else
                    {
                        SetDockedPosition(dockable, new DockedPosition(new Point(dockableSize.Left - mainSize.Left, dockableSize.Top - mainSize.Top), sides));
                        PositionDockedWindows();
                    }
                }
            }

            return IntPtr.Zero;
        }

        private void MainWindow_SizeOrLocationChanged(object? sender, EventArgs e)
        {
            PositionDockedWindows();
        }

        public void Dispose()
        {
            _main.LocationChanged -= MainWindow_SizeOrLocationChanged;
            _main.SizeChanged -= MainWindow_SizeOrLocationChanged;

            foreach (var dw in _dockable.ToList())
                RemoveDockable(dw);
        }
    }
}
