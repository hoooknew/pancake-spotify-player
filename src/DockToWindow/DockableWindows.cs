using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        public enum Side { Left, Right, Top, Bottom }
        private record DockedPosition(Point position, Side side);        

        private Window _main;
        private List<Window> _dockable;
        private Dictionary<Window, DockedPosition> _dockedWindows;

        public DockableWindows(Window main)
        {
            _main = main;
            _main.LocationChanged += MainWindow_LocationChanged;

            _dockable = new List<Window>();
            _dockedWindows = new Dictionary<Window, DockedPosition>();
        }

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


        public void AddDockable(Window w)
        {
            if (!_dockable.Contains(w))
            {
                _dockable.Add(w);
                AddHook(w, Dockable_WndProc);
            }
        }

        public void RemoveDockable(Window w)
        {
            if (_dockable.Contains(w))
            {
                RemoveHook(w, Dockable_WndProc);
                _dockable.Remove(w);
            }
        }

        private DockedPosition? GetDockedPosition(Window dockable)
        {
            return _dockedWindows?.GetValueOrDefault(dockable) ?? null;
        }

        private Window? GetDockableWithHandle(IntPtr h)
            => _dockable.FirstOrDefault(w => GetHandle(w) == h);

        private void SetDockedPosition(Window dockable, DockedPosition? position)
        {
            if (position != null)
                _dockedWindows[dockable] = position;
            else
                _dockedWindows.Remove(dockable);
        }

        private void PositionDockedWindows()
        {
            foreach (var docked in _dockedWindows.Keys)
            {
                var position = _dockedWindows[docked];
                docked.Top = _main.Top + position.position.Y;
                docked.Left = _main.Left + position.position.X;
            }
        }

        private IntPtr Dockable_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_EXITSIZEMOVE)
            {
                Window? dockable = GetDockableWithHandle(hwnd);

                if (dockable != null)
                {
                    const double SNAP_DISTANCE = 20;

                    var position = GetDockedPosition(dockable);

                    var me = NativeMethods.GetWindowRectangle(GetHandle(_main));
                    var de = NativeMethods.GetWindowRectangle(GetHandle(dockable));
                    //if dockable window bottom is close to the main window top
                    if (Math.Abs(me.Top - de.Bottom) < SNAP_DISTANCE &&
                        (
                            (de.Left >= me.Left && de.Left <= me.Right) ||
                            (de.Right >= me.Left && de.Right <= me.Right)
                        ))
                    {
                        var newTop = dockable.Top + me.Top - de.Bottom;
                        SetDockedPosition(dockable, new DockedPosition(new Point(dockable.Left - _main.Left, newTop - _main.Top), Side.Top));

                        if (position == null || position.side != Side.Top)
                        {
                            dockable.Top = newTop;
                            Debug.WriteLine("docked on top");
                        }
                    }
                    else
                    {
                        if (position != null)
                        {
                            SetDockedPosition(dockable, null);
                            Debug.WriteLine("undocked");
                        }
                    }
                }
            }

            return IntPtr.Zero;
        }

        private void AddHook(Window w, HwndSourceHook callback)
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

        private void RemoveHook(Window w, HwndSourceHook callback)
        {
            var h = GetHandle(w);
            HwndSource source = HwndSource.FromHwnd(h);
            source.RemoveHook(callback);
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            PositionDockedWindows();
        }

        public void Dispose()
        {
            _main.LocationChanged -= MainWindow_LocationChanged;
        }
    }
}
