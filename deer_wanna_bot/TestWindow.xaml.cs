using Composition.WindowsRuntimeHelpers;
using deer_wanna_bot.CaptureSample;
using System;
using System.Numerics;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Media3D;
using Windows.ApplicationModel.Store;
using Windows.Graphics.Capture;
using Windows.UI.Composition;

namespace deer_wanna_bot
{
    /// <summary>
    /// TestWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class TestWindow : Window
    {
        Compositor compositor;
        CompositionTarget target;
        ContainerVisual root;
        IntPtr hwnd;

        private BasicSampleApplication sample;

        public TestWindow()
        {
            InitializeComponent();
        }

        public void setTestMat(OpenCvSharp.Mat m)
        {
            sample.setTestMat(m);
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            var interopWindow = new WindowInteropHelper(this);
            hwnd = interopWindow.Handle;

            compositor = new Compositor();

            target = compositor.CreateDesktopWindowTarget(hwnd, false);

            int width = (int) this.RenderSize.Width;
            int height = (int) this.RenderSize.Height;

            root = compositor.CreateContainerVisual();

            //root.RelativeSizeAdjustment = new Vector2(0.5f, 0.5f);// Vector2.One;
            //root.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            //root.RelativeOffsetAdjustment = new Vector3(0, 0, 0);
            root.Size = new Vector2(width, height);
            //root.Offset = new Vector3(-(width / 2), -1, 0);
            target.Root = root;


            sample = new BasicSampleApplication(compositor);
            root.Children.InsertAtTop(sample.Visual);
        }

        public void startHwndCapture(IntPtr hwnd)
        {
            sample.StopCapture();
            try
            {
                GraphicsCaptureItem item = CaptureHelper.CreateItemForWindow(hwnd);
                if (item != null)
                {
                    sample.StartCaptureFromItem(item);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private void onUnloaded(object sender, RoutedEventArgs e)
        {
            sample.StopCapture();
        }

        public OpenCvSharp.Mat getMat()
        {
            return sample.getLastMat();
        }

        private void onSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if(root != null)
            {
                root.Size = new Vector2((float)e.NewSize.Width, (float)e.NewSize.Height);
            }
        }
    }
}
