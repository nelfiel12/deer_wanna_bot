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

namespace deer_wanna_bot
{
    /// <summary>
    /// CaptureWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CaptureWindow : Window
    {
        public CaptureWindow()
        {
            InitializeComponent();
        }

        public void setMat(OpenCvSharp.Mat m)
        {
            Image.Source = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToBitmapSource(m);
        }
    }
}
