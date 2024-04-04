using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Store;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;

namespace deer_wanna_bot.CaptureSample
{
    public class BasicSampleApplication : IDisposable
    {
        private Compositor compositor;
        public ContainerVisual root;

        private ContainerVisual drawingVisual;

        public SpriteVisual content;
        private CompositionSurfaceBrush brush;

        private IDirect3DDevice device;
        private BasicCapture capture;

        public BasicSampleApplication(Compositor c)
        {
            compositor = c;
            device = Direct3D11Helper.CreateDevice();

            // Setup the root.
            root = compositor.CreateContainerVisual();
            
            // Setup the content.
            brush = compositor.CreateSurfaceBrush();
            //brush.HorizontalAlignmentRatio = 0.5f;
            //brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.None;


            var shadow = compositor.CreateDropShadow();
            shadow.Mask = brush;
            content = compositor.CreateSpriteVisual();
            content.Brush = brush;
            content.Shadow = shadow;
            root.Children.InsertAtTop(content);

            drawingVisual = compositor.CreateContainerVisual();
            root.Children.InsertAtTop(drawingVisual);            

            var visual = compositor.CreateSpriteVisual();
            visual.Size = new Vector2(100, 100);
            visual.Scale = new Vector3(1, 1, 1);

            visual.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
            visual.Offset = new Vector3(0, 0, 0);
            drawingVisual.Children.InsertAtTop(visual);
        }

        public Visual Visual => root;

        public void Dispose()
        {
            StopCapture();
            compositor = null;
            root.Dispose();
            content.Dispose();
            brush.Dispose();
            device.Dispose();
        }

        public void StartCaptureFromItem(GraphicsCaptureItem item)
        {
            StopCapture();
            capture = new BasicCapture(device, item);
            capture.TestEvent += Capture_TestEvent;

            var surface = capture.CreateSurface(compositor);
            brush.Surface = surface;
            
            capture.StartCapture();
        }

        private void Capture_TestEvent(object sender, OpenCvSharp.Mat src)
        {           
            if (testMat == null || testMat.Empty())
                return;

            drawingVisual.Children.RemoveAll();

            OpenCvSharp.TemplateMatchModes[] modes = {
                OpenCvSharp.TemplateMatchModes.CCoeff,
                OpenCvSharp.TemplateMatchModes.CCoeffNormed,
                OpenCvSharp.TemplateMatchModes.SqDiff,
                OpenCvSharp.TemplateMatchModes.SqDiffNormed,
                OpenCvSharp.TemplateMatchModes.CCorr,
                OpenCvSharp.TemplateMatchModes.CCorrNormed,
            };

            OpenCvSharp.Mat gray = new OpenCvSharp.Mat();
            OpenCvSharp.Cv2.CvtColor(src, gray, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);

            OpenCvSharp.Cv2.ImWrite("capture_src.jpg", src);
            OpenCvSharp.Cv2.ImWrite("capture_gray.jpg", gray);

            OpenCvSharp.Mat tempMat = new OpenCvSharp.Mat();
            OpenCvSharp.Cv2.CvtColor(testMat, tempMat, OpenCvSharp.ColorConversionCodes.BGR2GRAY);
            OpenCvSharp.Cv2.ImWrite("temp_src.jpg", testMat);
            OpenCvSharp.Cv2.ImWrite("temp_gray.jpg", tempMat);

            foreach (var mode in modes)
            {
                OpenCvSharp.Mat ret = new OpenCvSharp.Mat();
                OpenCvSharp.Cv2.MatchTemplate(gray, tempMat, ret, mode);
                OpenCvSharp.Cv2.Normalize(ret, ret, 0, 1, OpenCvSharp.NormTypes.MinMax);
                double minVal;
                double maxVal;
                OpenCvSharp.Point minLoc;
                OpenCvSharp.Point maxLoc;
                OpenCvSharp.Cv2.MinMaxLoc(ret, out minVal, out maxVal, out minLoc, out maxLoc);

                OpenCvSharp.Point matchLoc;

                switch(mode)
                {
                    case OpenCvSharp.TemplateMatchModes.SqDiff:
                    case OpenCvSharp.TemplateMatchModes.SqDiffNormed:
                        matchLoc = minLoc;
                        break;
                    default:
                        matchLoc = maxLoc;
                        break;
                }

                OpenCvSharp.Mat t = src.Clone();
                OpenCvSharp.Cv2.Rectangle(t, new OpenCvSharp.Rect(matchLoc.X, matchLoc.Y, tempMat.Cols, tempMat.Rows), new OpenCvSharp.Scalar(255, 0, 0), 10);

                OpenCvSharp.Cv2.ImWrite("test_" + mode + ".jpg", t);
                t.Dispose();

                
                /*
                var visual = compositor.CreateSpriteVisual();
                visual.Size = new Vector2( tempMat.Cols, tempMat.Rows);
                visual.Brush = compositor.CreateColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0));
                visual.Offset = new Vector3(matchLoc.X, matchLoc.Y, 0);
                visual.Opacity = 0.5f;
                drawingVisual.Children.InsertAtTop(visual);
                */
                ret.Dispose();
            }

            gray.Dispose();
            tempMat.Dispose();
            testMat.Dispose();
            testMat = null;
        }

        OpenCvSharp.Mat testMat;
        public void setTestMat(OpenCvSharp.Mat m)
        {
            testMat = m;
        }

        public void StopCapture()
        {
            capture?.Dispose();
            brush.Surface = null;
        }

        public OpenCvSharp.Mat getLastMat()
        {
            return capture?.lastMat;
        }
    }
}
