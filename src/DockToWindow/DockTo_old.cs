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
    internal class DockTo_Old
    {
        public enum Side { Left, Right, Top, Bottom }
        private record DockedPosition(Point position, Side side);
        private record WindowEdges(double left, double top, double right, double bottom)
        {
            public static WindowEdges From(Window w)
                => new WindowEdges(w.Left, w.Top, w.Left + w.ActualWidth, w.Top + w.ActualHeight);            
        }

        public static Window GetWindow(DependencyObject obj)
            => (Window)obj.GetValue(WindowProperty);
        public static void SetWindow(DependencyObject obj, Window value)
            => obj.SetValue(WindowProperty, value);        
        public static readonly DependencyProperty WindowProperty =
            DependencyProperty.RegisterAttached("Window", typeof(Window), typeof(DockableWindows), new PropertyMetadata(new PropertyChangedCallback(WindowChangedCallback)));


        private static Dictionary<Window, DockedPosition> GetDockedWindows(DependencyObject obj)
            => (Dictionary<Window, DockedPosition>)obj.GetValue(DockedWindowsProperty);
        private static void SetDockedWindows(DependencyObject obj, Dictionary<Window, DockedPosition> value)
            => obj.SetValue(DockedWindowsProperty, value);
        private static readonly DependencyProperty DockedWindowsProperty =
            DependencyProperty.RegisterAttached("DockedWindows", typeof(Dictionary<Window, DockedPosition>), typeof(DockableWindows), new PropertyMetadata(null));



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



        private static DockedPosition? GetDockedPosition(Window main, Window dockable)
        {
            var docked = GetDockedWindows(main);
            return docked?.GetValueOrDefault(dockable) ?? null;
        }

        private static void SetDockedPosition(Window main, Window dockable, DockedPosition? position)
        {
            var dockedWindows = GetDockedWindows(main);
            if (dockedWindows == null)
                dockedWindows = new Dictionary<Window, DockedPosition>();

            if (position != null)
                dockedWindows[dockable] = position;
            else
                dockedWindows.Remove(dockable);

            SetDockedWindows(main, dockedWindows);
        }

        private static void PositionDockedWindows(Window main)
        {
            var dockedWindows = GetDockedWindows(main);
            if (dockedWindows != null)
                foreach(var docked in dockedWindows.Keys)
                {
                    var position = dockedWindows[docked];
                    docked.Top = main.Top + position.position.Y;
                    docked.Left = main.Left + position.position.X;
                }
        }


        static void WindowChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

        private static bool __inside_DockableWindow_LocationChanged = false;
        private static void DockableWindow_LocationChanged(object? sender, EventArgs e)
        {
            const double SNAP_DISTANCE = 20;

            if (!__inside_DockableWindow_LocationChanged)
            {
                __inside_DockableWindow_LocationChanged = true;
                try
                {
                    if (sender is Window dockable)
                    {
                        var main = DockableWindows.GetWindow(dockable);

                        if (main != null)
                        {
                            var position = GetDockedPosition(main, dockable);

                            var me = WindowEdges.From(main);
                            var de = WindowEdges.From(dockable);
                            //if dockable window bottom is close to the main window top
                            if (Math.Abs(me.top - de.bottom) < SNAP_DISTANCE &&
                                (
                                    (de.left >= me.left && de.left <= me.right) ||
                                    (de.right >= me.left && de.right <= me.right)
                                ))
                            {
                                var newTop = dockable.Top + me.top - de.bottom;
                                SetDockedPosition(main, dockable, new DockedPosition(new Point(dockable.Left - main.Left, newTop - main.Top), Side.Top));

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
                                    SetDockedPosition(main, dockable, null);
                                    Debug.WriteLine("undocked");
                                }
                            }

                            //if dockable window top is close to the main window bottom

                            //if dockable window left is close to the main window right

                            //if dockable window right is close to the main window left
                        }
                    }
                }
                finally
                {
                    __inside_DockableWindow_LocationChanged = false;
                }
            }
        }

        private static void MainWindow_LocationChanged(object? sender, EventArgs e)
        {
            if (sender is Window w) {
                PositionDockedWindows(w);
            }
        }
    }
}
