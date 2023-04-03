using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;

namespace pancake.ui.controls
{
    public partial class BaseWindow : Window
    {
        bool ResizeInProcess = false;

        public event EventHandler Resized;
        

        public BaseWindow()
        {
            DefaultStyleKey= typeof(BaseWindow);
            //InitializeComponent();
        }

        public override void OnApplyTemplate()
        {
            if (GetTemplateChild("PART_closeBtn") is Button closeBtn)
            {
                closeBtn.Click += _closeBtn_Click;
            }

            if (GetTemplateChild("PART_leftSizeGrip") is Rectangle leftRect)
            {
                leftRect.MouseLeftButtonDown += Resize_Init;
                leftRect.MouseMove += ResizingLeft_Window;
                leftRect.MouseUp += Resize_End;
            }

            if (GetTemplateChild("PART_rightSizeGrip") is Rectangle rightRect)
            {
                rightRect.MouseLeftButtonDown += Resize_Init;
                rightRect.MouseMove += ResizingRight_Window;
                rightRect.MouseUp += Resize_End;
            }

            MouseDown += Window_MouseDown;

            base.OnApplyTemplate();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.MouseMove += BaseWindow_MouseMove;
        }

        private void BaseWindow_MouseMove(object sender, MouseEventArgs e)
        {
            this.MouseMove -= BaseWindow_MouseMove;

            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();            
        }

        private void Resize_Init(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle senderRect)
            {
                ResizeInProcess = true;
                senderRect.CaptureMouse();
                e.Handled = true;
            }
        }
        private void Resize_End(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle senderRect)
            {
                ResizeInProcess = false; ;
                senderRect.ReleaseMouseCapture();
                e.Handled = true;

                this.Resized?.Invoke(this, EventArgs.Empty);
            }
        }
        private void ResizingLeft_Window(object sender, MouseEventArgs e)
        {
            if (ResizeInProcess && sender is Rectangle senderRect)
            {
                double mouseX = e.GetPosition(this).X;
                    
                Width = Math.Max(MinWidth, Width - mouseX);
                Left = Left + mouseX;

                e.Handled = true;
            }
        }

        private void ResizingRight_Window(object sender, MouseEventArgs e)
        {
            if (ResizeInProcess && sender is Rectangle senderRect)
            {
                double mouseX = e.GetPosition(this).X;
                Width = Math.Max(MinWidth, mouseX);

                e.Handled = true;
            }
        }


        private void _closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
