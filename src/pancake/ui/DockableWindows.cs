﻿using Microsoft.Extensions.Logging;
using pancake.lib;
using pancake.ui;
using pancake.ui.controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;


internal class DockableWindows : IDisposable
{
    #region NativeMethods
    private class NativeMethods
    {
        #region Constants
        public const int WM_EXITSIZEMOVE = 0x232;
        public const int WM_ENTERSIZEMOVE = 0x231;
        public const int WM_SIZE = 0x0005;

        #endregion

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, int dwAttribute, out RECT pvAttribute, int cbAttribute);

        [DllImport("User32.dll")]
        public static extern uint GetDpiForWindow(IntPtr hwnd);

        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public bool Close_BottomToTop(NativeMethods.RECT other_r, double closeDist)
                => Math.Abs(other_r.Top - this.Bottom) <= closeDist;

            public bool Close_TopToBottom(NativeMethods.RECT other_r, double closeDist)
                => Math.Abs(other_r.Bottom - this.Top) <= closeDist;

            public bool Close_LeftToRight(NativeMethods.RECT other_r, double closeDist)
                => Math.Abs(other_r.Right - this.Left) <= closeDist;

            public bool Close_RightToLeft(NativeMethods.RECT other_r, double closeDist)
                => Math.Abs(other_r.Left - this.Right) <= closeDist;

            public double Dist_RightToRight(NativeMethods.RECT other_r)
                => Math.Abs(other_r.Right - this.Right);

            public double Dist_LeftToLeft(NativeMethods.RECT other_r)
                => Math.Abs(other_r.Left - this.Left);

            public double Dist_BottomToBottom(NativeMethods.RECT other_r)
                => Math.Abs(other_r.Bottom - this.Bottom);

            public double Dist_TopToTop(NativeMethods.RECT other_r)
                => Math.Abs(other_r.Top - this.Top);


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

            public RECT Scale(decimal factor)
            {
                RECT scaled = new RECT();
                scaled.Left = (int)Math.Floor(Left * factor);
                scaled.Right = (int)Math.Floor(Right * factor);
                scaled.Top = (int)Math.Floor(Top * factor);
                scaled.Bottom = (int)Math.Floor(Bottom * factor);

                return scaled;
            }
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
            // https://github.com/StrongCod3r/SnapWindow/blob/master/WpfExample/WinApi.cs#L44
            int size = Marshal.SizeOf(typeof(RECT));
            DwmGetWindowAttribute(hWnd, (int)DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT rect, size);

            decimal dpiScale = GetDpiForWindow(hWnd) / 96;
            if (dpiScale > 0)
                rect = rect.Scale(1 / dpiScale);

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

    #region DockedPosition Attached Property
    public static DockedPosition? GetDockedPosition(DependencyObject obj) =>
        (DockedPosition)obj.GetValue(DockedPositionProperty);

    public static void SetDockedPosition(DependencyObject obj, DockedPosition? value) =>
        obj.SetValue(DockedPositionProperty, value);

    public static readonly DependencyProperty DockedPositionProperty =
        DependencyProperty.RegisterAttached("DockedPosition", typeof(DockedPosition), typeof(DockableWindows), new PropertyMetadata(null));
    #endregion


    [Flags]
    public enum DockedTo
    {
        None = 0,

        Top_Primary = 1,
        Bottom_Primary = 2,
        Left_Primary = 4,
        Right_Primary = 8,

        Left_Secondary = 16,
        Right_Secondary = 32,
        Top_Secondary = 64,
        Bottom_Secondary = 128
    }

    const double DEFAULT_SNAP_DISTANCE = 20;

    public record DockedPosition(Point offset, DockedTo dockedTo);

    private Window _main;
    readonly double _snapDistance;
    private List<Window> _dockable;

    public double SnapDistance => _snapDistance;
    public bool AllowTop { get; set; } = true;
    public bool AllowBottom { get; set; } = true;
    public bool AllowLeft { get; set; } = true;
    public bool AllowRight { get; set; } = true;

    private readonly ILogger _log;
    private readonly IDispatchProvider _dispatchProvider;

    public DockableWindows(Window main, ILogging logging, IDispatchProvider dispatchProvider, double snapDistance = DEFAULT_SNAP_DISTANCE)
    {
        _main = main;
        _main.LocationChanged += MainWindow_LocationChanged;
        _main.SizeChanged += MainWindow_LocationChanged;

        _snapDistance = snapDistance;

        _dockable = new List<Window>();

        _log = logging.Create<DockedPosition>();

        _log.LogInformation($"created {nameof(DockableWindows)}");

        _dispatchProvider = dispatchProvider;
    }

    void AddHook(Window w, HwndSourceHook callback)
    {
        if (w.IsLoaded)
        {
            var h = GetHandle(w);
            HwndSource source = HwndSource.FromHwnd(h);
            if (source != null)
                source.AddHook(callback);
        }
        else
            w.Loaded += (s, e) => AddHook(w, callback);

    }

    void RemoveHook(Window w, HwndSourceHook callback)
    {
        var h = GetHandle(w);
        HwndSource source = HwndSource.FromHwnd(h);
        source?.RemoveHook(callback);
    }

    private void AddDockable(Window w)
    {
        if (!_dockable.Contains(w))
        {
            _dockable.Add(w);
            w.IsVisibleChanged += _dockable_IsVisibleChanged;
            w.Closed += (s, e) => RemoveDockable((s as Window)!);
            AddHook(w, Dockable_WndProc);
            if (w is BaseWindow bw)
                bw.Resized += _dockable_SizeChanged;

            _log.LogInformation("added window {0} to dockable list.", GetWindowName(w));
        }
        else
            _log.LogInformation("did not add window {0} to dockable list. already in list", GetWindowName(w));
    }



    private void RemoveDockable(Window w)
    {
        if (_dockable.Contains(w))
        {
            RemoveHook(w, Dockable_WndProc);
            w.IsVisibleChanged -= _dockable_IsVisibleChanged;
            _dockable.Remove(w);
            if (w is BaseWindow bw)
                bw.Resized -= _dockable_SizeChanged;

            _log.LogInformation("removed window {0} to dockable list.", GetWindowName(w));
        }
        else
            _log.LogInformation("did not remove window {0} to dockable list. it was not in list", GetWindowName(w));

        SetDockedPosition(w, null);
    }

    public void DockWindowTo(Window dockable, DockedTo dockTo)
    {
        AddDockable(dockable);

        var mainSize = NativeMethods.GetExtendedFrameBounds(_main);
        var dockableSize = NativeMethods.GetExtendedFrameBounds(dockable);

        SetDockedPosition(dockable, new DockedPosition(new Point(dockableSize.Left - mainSize.Left, dockableSize.Top - mainSize.Top), dockTo));
        if (_main.IsLoaded && _main.IsVisible)
        {
            _log.LogInformation("new docked window is visible.");
            PositionDockedWindows();
        }
    }

    private Window? GetDockableWithHandle(IntPtr h)
        => _dockable.FirstOrDefault(w => GetHandle(w) == h);

    public void PositionDockedWindows()
    {
        _log.LogInformation("starting to position docked windows...");

        var mainSize = NativeMethods.GetExtendedFrameBounds(_main);

        foreach (var docked in _dockable)
        {
            var position = GetDockedPosition(docked);
            if (position != null)
            {
                var dockedSize = NativeMethods.GetExtendedFrameBounds(docked);

                double top;
                double left;
                double width = docked.Width;
                double height = docked.Height;

                if (position.dockedTo.HasFlag(DockedTo.Top_Secondary) && position.dockedTo.HasFlag(DockedTo.Bottom_Secondary))
                {
                    top = _main.Top;
                    height = _main.Height;
                }
                else if (position.dockedTo.HasFlag(DockedTo.Bottom_Primary))
                    top = mainSize.Top + mainSize.Height;
                else if (position.dockedTo.HasFlag(DockedTo.Top_Primary))
                    top = mainSize.Top - dockedSize.Height;
                else if (position.dockedTo.HasFlag(DockedTo.Top_Secondary))
                    top = _main.Top;
                else if (position.dockedTo.HasFlag(DockedTo.Bottom_Secondary))
                    top = _main.Top + (mainSize.Height - dockedSize.Height);
                else
                    top = _main.Top + position.offset.Y;


                if (position.dockedTo.HasFlag(DockedTo.Left_Secondary) && position.dockedTo.HasFlag(DockedTo.Right_Secondary))
                {
                    left = _main.Left;
                    width = _main.Width;
                }
                else if (position.dockedTo.HasFlag(DockedTo.Left_Primary))
                    left = _main.Left - dockedSize.Width;
                else if (position.dockedTo.HasFlag(DockedTo.Right_Primary))
                    left = _main.Left + mainSize.Width;
                else if (position.dockedTo.HasFlag(DockedTo.Left_Secondary))
                    left = _main.Left;
                else if (position.dockedTo.HasFlag(DockedTo.Right_Secondary))
                    left = _main.Left + (mainSize.Width - dockedSize.Width);
                else
                    left = _main.Left + position.offset.X;

                docked.Top = top;
                docked.Left = left;
                docked.Width = width;
                docked.Height = height;
            }
        }
    }

    private IntPtr Dockable_WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        // https://github.com/StrongCod3r/SnapWindow/blob/master/WpfExample/SnapWindow.cs#L23

        switch (msg)
        {
            case NativeMethods.WM_EXITSIZEMOVE:
                var dockable = GetDockableWithHandle(hwnd);
                _log.LogInformation($"docked window location/size changed: {GetWindowName(dockable)}");
                TryToDock(dockable);
                break;
        }
        return IntPtr.Zero;
    }

    private void _dockable_SizeChanged(object? sender, EventArgs e)
    {
        if (sender is Window dockable)
        {
            _log.LogInformation($"docked window size changed: {GetWindowName(dockable)}");
            TryToDock(dockable);
        }
    }

    private void _dockable_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is Window w && w.IsVisible)
        {
            _log.LogInformation("docked window visibility changed: {0}", GetWindowName(w));
            PositionDockedWindows();
        }
    }

    private void Dockable_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is Window dockable)
        {
            dockable.MouseUp -= Dockable_MouseUp;
            _log.LogInformation("dockable window mouse up: {0}", GetWindowName(dockable));
            TryToDock(dockable);
        }
    }

    private void TryToDock(Window? dockable)
    {
        if (dockable != null)
        {
            var mainSize = NativeMethods.GetExtendedFrameBounds(_main);
            var dockableSize = NativeMethods.GetExtendedFrameBounds(dockable);

            DockedTo dockTo = GetPrimaryDock(mainSize, dockableSize);
            dockTo |= GetSeconadryDock(mainSize, dockableSize, dockTo);

            if (dockTo == DockedTo.None)
            {
                if (GetDockedPosition(dockable) != null)
                    SetDockedPosition(dockable, null);
            }
            else
            {
                SetDockedPosition(dockable, new DockedPosition(new Point(dockableSize.Left - mainSize.Left, dockableSize.Top - mainSize.Top), dockTo));
                PositionDockedWindows();
            }
        }
    }

    private DockedTo GetPrimaryDock(NativeMethods.RECT mainSize, NativeMethods.RECT dockableSize)
    {
        var dockTo = DockedTo.None;

        if (
            AllowTop &&
            dockableSize.Close_BottomToTop(mainSize, SnapDistance) &&
            dockableSize.HasHorizontalOverlap(mainSize))
            dockTo = DockedTo.Top_Primary;
        else if (
            AllowBottom &&
            dockableSize.Close_TopToBottom(mainSize, SnapDistance) &&
            dockableSize.HasHorizontalOverlap(mainSize))
            dockTo = DockedTo.Bottom_Primary;
        else if (
            AllowRight &&
            dockableSize.Close_LeftToRight(mainSize, SnapDistance) &&
            dockableSize.HasVerticalOverlap(mainSize))
            dockTo = DockedTo.Right_Primary;
        else if (
            AllowLeft &&
            dockableSize.Close_RightToLeft(mainSize, SnapDistance) &&
            dockableSize.HasVerticalOverlap(mainSize))
            dockTo = DockedTo.Left_Primary;
        return dockTo;
    }

    private DockedTo GetSeconadryDock(NativeMethods.RECT mainSize, NativeMethods.RECT dockableSize, DockedTo dockTo)
    {
        if (dockTo.HasFlag(DockedTo.Top_Primary) || dockTo.HasFlag(DockedTo.Bottom_Primary))
        {
            var left = dockableSize.Dist_LeftToLeft(mainSize);
            var right = dockableSize.Dist_RightToRight(mainSize);

            if (left <= SnapDistance && right <= SnapDistance)
                return DockedTo.Left_Secondary | DockedTo.Right_Secondary;
            else if (left <= SnapDistance)
                return DockedTo.Left_Secondary;
            else if (right <= SnapDistance)
                return DockedTo.Right_Secondary;
        }
        else if (dockTo.HasFlag(DockedTo.Left_Primary) || dockTo.HasFlag(DockedTo.Right_Primary))
        {
            var top = dockableSize.Dist_TopToTop(mainSize);
            var bottom = dockableSize.Dist_BottomToBottom(mainSize);
            if (top <= SnapDistance && bottom <= SnapDistance)
                return DockedTo.Top_Secondary | DockedTo.Bottom_Secondary;
            else if (top <= SnapDistance)
                return DockedTo.Top_Secondary;
            else if (bottom <= SnapDistance)
                return DockedTo.Bottom_Secondary;
        }

        return DockedTo.None;
    }

    private void MainWindow_LocationChanged(object? sender, EventArgs e)
    {
        //sizing and positioning for the window may not be correct when this event
        //is called at startup. The `BeginInvoke()` give the window a chance to update.
        _dispatchProvider.BeginInvoke(() =>
        {
            _log.LogInformation($"main window location changed: {GetWindowName(_main)}");

            PositionDockedWindows();
        });

    }

    private string GetWindowName(Window? w) =>
         w == null ? "<NULL>" : $"{w.GetType().FullName ?? "<NULL>"}/\"{w.Name}\"/{w.Left},{w.Top}/{w.Width},{w.Height}";

    public void Dispose()
    {
        _main.LocationChanged -= MainWindow_LocationChanged;
        _main.SizeChanged -= MainWindow_LocationChanged;

        foreach (var dw in _dockable.ToList())
            RemoveDockable(dw);
    }
}