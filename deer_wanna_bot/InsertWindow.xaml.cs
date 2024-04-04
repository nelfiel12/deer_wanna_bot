using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace deer_wanna_bot
{
    /// <summary>
    /// InsertWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class InsertWindow : System.Windows.Window
    {
        Mat m_templateMat;
        Mat m_maskMat;
        Mat m_srcMat;

        public InsertWindow()
        {
           
            InitializeComponent();

            Canvas.Children.Add(m_RectElement);

            TemplateImage.Focus();

            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += (sender, args) =>
            {
                if (Clipboard.ContainsImage())
                {
                    var bitmapSource = Clipboard.GetImage();

                    if (MaskBorder.IsFocused)
                    {
                        m_maskMat = bitmapSource.ToMat();
                        MaskImage.Source = bitmapSource;
                    }
                    else if (TemplateBorder.IsFocused)
                    {
                        m_templateMat = bitmapSource.ToMat();
                        TemplateImage.Source = bitmapSource;
                    }
                }
            };
        }

        private void TemplateButton_Click(object sender, RoutedEventArgs e)
        {
            TemplateBorder.Focus();
        }

        private void MaskButton_Click(object sender, RoutedEventArgs e)
        {
            MaskBorder.Focus();
        }

        public void SetScreenMat(Mat mat)
        {
            m_srcMat = mat;
            ImageSrc.Source = mat?.ToBitmapSource() ?? null;
        }

        Rectangle m_RectElement = new Rectangle
                {
                    Stroke = Brushes.Red,
                    StrokeThickness = 2
                };        
        
        System.Windows.Point m_downPt;
        bool m_bPressed = false;
        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed)
            {
                m_bPressed = true;
                Canvas.CaptureMouse();
                m_downPt = e.GetPosition(Canvas);                

                m_RectElement.Width = 0;
                m_RectElement.Height = 0;
                
                Canvas.SetLeft(m_RectElement, m_downPt.X);
                Canvas.SetTop(m_RectElement, m_downPt.Y);
            }
        }

        private void Canvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Released)
            {
                if (Canvas.IsMouseCaptured)
                    Canvas.ReleaseMouseCapture();

                m_bPressed = false;
            }
            
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if(e.LeftButton == MouseButtonState.Pressed && m_bPressed)
            {
                if(m_downPt != null)
                {
                    var pos = e.GetPosition(Canvas);

                    if (pos.X < 0)
                        pos.X = 0;
                    else if (Canvas.ActualWidth < pos.X)
                        pos.X = Canvas.ActualWidth;

                    if (pos.Y < 0)
                        pos.Y = 0;
                    else if (Canvas.ActualHeight < pos.Y)
                        pos.Y = Canvas.ActualHeight;

                    var x = Math.Min(pos.X, m_downPt.X);
                    var y = Math.Min(pos.Y, m_downPt.Y);

                    var w = Math.Max(pos.X, m_downPt.X) - x;
                    var h = Math.Max(pos.Y, m_downPt.Y) - y;

                    m_RectElement.Width = w;
                    m_RectElement.Height = h;

                    Canvas.SetLeft(m_RectElement, x);
                    Canvas.SetTop(m_RectElement, y);
                }
            }
        }

        private void Click_Save(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("TODO");
        }
    }
}
