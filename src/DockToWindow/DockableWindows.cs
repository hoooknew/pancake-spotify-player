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
    internal class DockableWindows
    {
        public enum Side { Left, Right, Top, Bottom }
        private record DockedPosition(Point position, Side side);
        private record WindowEdges(double left, double top, double right, double bottom)
        {
            public static WindowEdges From(Window w)
                => new WindowEdges(w.Left, w.Top, w.Left + w.ActualWidth, w.Top + w.ActualHeight);
        }


        private Window _main;
        private Dictionary<Window, DockedPosition> _dockedWindows;

        public DockableWindows(Window main)
        {
            _main = main;
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

        private void PositionDockedWindows()
        {
            foreach (var docked in _dockedWindows.Keys)
            {
                var position = _dockedWindows[docked];
                docked.Top = _main.Top + position.position.Y;
                docked.Left = _main.Left + position.position.X;
            }
        }


        void WindowChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window dockable)
            {
                var x = GetHandle(dockable);
                HwndSource source = HwndSource.FromHwnd(x);

                source.RemoveHook(new HwndSourceHook(Dockable_WndProc));

                if (e.OldValue is Window oldMain)
                    oldMain.LocationChanged -= MainWindow_LocationChanged;

                if (e.NewValue is Window newMain)
                {
                    source.AddHook(new HwndSourceHook(Dockable_WndProc));

                    newMain.LocationChanged += MainWindow_LocationChanged;
                }
            }
        }

        private static IntPtr Dockable_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_EXITSIZEMOVE)
            {
                Debug.WriteLine("Dockable_WndProc");
            }

            return IntPtr.Zero;
        }

        private bool __inside_DockableWindow_LocationChanged = false;
        private void DockableWindow_LocationChanged(object? sender, EventArgs e)
        {
            const double SNAP_DISTANCE = 20;

            if (!__inside_DockableWindow_LocationChanged)
            {
                __inside_DockableWindow_LocationChanged = true;
                try
                {
                    if (sender is Window dockable)
                    {
                        var position = GetDockedPosition(dockable);

                        var me = WindowEdges.From(_main);
                        var de = WindowEdges.From(dockable);
                        //if dockable window bottom is close to the main window top
                        if (Math.Abs(me.top - de.bottom) < SNAP_DISTANCE &&
                            (
                                (de.left >= me.left && de.left <= me.right) ||
                                (de.right >= me.left && de.right <= me.right)
                            ))
                        {
                            var newTop = dockable.Top + me.top - de.bottom;
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

                        //if dockable window top is close to the main window bottom

                        //if dockable window left is close to the main window right

                        //if dockable window right is close to the main window left
                    }
                }
                finally
                {
                    __inside_DockableWindow_LocationChanged = false;
                }
            }
        }

        private void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            PositionDockedWindows();
        }
    }
}
