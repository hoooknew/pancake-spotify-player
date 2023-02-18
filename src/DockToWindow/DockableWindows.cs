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
            var mainSize = _main.GetWindowSize();            

            foreach (var docked in _dockedWindows.Keys)
            {
                var dockedSize = docked.GetWindowSize();

                var position = _dockedWindows[docked];
                double top;
                double left;

                if (position.sides.HasFlag(Side.Top_Out))
                    top = mainSize.Top - dockedSize.Height();
                else if (position.sides.HasFlag(Side.Bottom_Out))
                    top = mainSize.Top + mainSize.Height();
                else
                    top = _main.Top + position.offset.Y;

                if (position.sides.HasFlag(Side.Left_In))
                    left = _main.Left;
                else if (position.sides.HasFlag(Side.Right_In))
                    left = _main.Left + (mainSize.Width() - dockedSize.Width());
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
                    var mainSize = _main.GetWindowSize();
                    var dockableSize = dockable.GetWindowSize();

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
                    else if (dockableSize.LeftCloseToRight(mainSize, SnapDistance) &&
                        dockableSize.HasVerticalOverlap(mainSize))
                        sides = Side.Left_Out;

                    if (sides.HasFlag(Side.Top_Out) || sides.HasFlag(Side.Bottom_Out))
                    {
                        if (dockableSize.LeftCloseToLeft(mainSize, SnapDistance))
                            sides |= Side.Left_In;
                        else if (dockableSize.RightCloseToRight(mainSize, SnapDistance))
                            sides |= Side.Right_In;
                    }
                    else if (sides.HasFlag(Side.Top_Out) || sides.HasFlag(Side.Bottom_Out))
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
                        SetDockedPosition(dockable, new DockedPosition(new Point(dockableSize.Left - mainSize.Left, dockableSize.Bottom - dockable.Top), sides));
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

    internal static class DockableWindows_Extensions
    {
        public static bool BottomCloseToTop(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
            => Math.Abs(other_r.Top - r.Bottom) < closeDist;

        public static bool TopCloseToBottom(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
            => Math.Abs(other_r.Bottom - r.Top) < closeDist;

        public static bool LeftCloseToRight(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
         => Math.Abs(other_r.Right - r.Left) < closeDist;

        public static bool RightCloseToLeft(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
            => Math.Abs(other_r.Left - r.Right) < closeDist;

        public static bool RightCloseToRight(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
            => Math.Abs(other_r.Right - r.Right) < closeDist;

        public static bool LeftCloseToLeft(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
        => Math.Abs(other_r.Left - r.Left) < closeDist;

        public static bool BottomCloseToBottom(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
            => Math.Abs(other_r.Bottom - r.Bottom) < closeDist;

        public static bool TopCloseToTop(this NativeMethods.RECT r, NativeMethods.RECT other_r, double closeDist)
            => Math.Abs(other_r.Top - r.Top) < closeDist;


        public static double Width(this NativeMethods.RECT r) => r.Right- r.Left;
        public static double Height(this NativeMethods.RECT r) => r.Bottom - r.Top;


        public static bool HasHorizontalOverlap(this NativeMethods.RECT r, NativeMethods.RECT other_r)
            => (r.Left >= other_r.Left && r.Left <= other_r.Right) ||
               (r.Right >= other_r.Left && r.Right <= other_r.Right);

        public static bool HasVerticalOverlap(this NativeMethods.RECT r, NativeMethods.RECT other_r)
           => (r.Top >= other_r.Top && r.Top <= other_r.Bottom) ||
              (r.Bottom >= other_r.Top && r.Bottom <= other_r.Bottom);


        public static NativeMethods.RECT GetWindowSize(this Window w)
            => NativeMethods.GetExtendedFrameBounds(DockableWindows.GetHandle(w));

    }
}
