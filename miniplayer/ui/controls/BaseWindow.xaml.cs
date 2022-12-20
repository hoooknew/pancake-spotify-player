using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace miniplayer.ui.controls
{
    /// <summary>
    /// Interaction logic for BaseWindow.xaml
    /// </summary>
    public partial class BaseWindow : Window
    {
        bool ResizeInProcess = false;

        public BaseWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
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
            }
        }
        private void Resizing_Window(object sender, MouseEventArgs e)
        {
            if (ResizeInProcess)
            {
                if (sender is Rectangle senderRect)
                {
                    double mouseX = e.GetPosition(this).X;
                    if (senderRect.Name == "_rightSizeGrip")
                    {
                        this.Width = Math.Max(this.MinWidth, mouseX);
                    }
                    else if (senderRect.Name == "_leftSizeGrip")
                    {
                        this.Width = Math.Max(this.MinWidth, this.Width - mouseX);
                        this.Left = this.Left + mouseX;
                    }
                }

                e.Handled = true;
            }
        }


        private void _closeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
