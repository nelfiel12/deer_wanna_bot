using deer_wanna_bot.CaptureSample;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Media;
using Windows.Foundation.Metadata;
using Windows.UI.Composition;

namespace deer_wanna_bot
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private IntPtr hWnd;
        private TestWindow testWindow;

        private CaptureWindow captureWindow;

        ObservableCollection<Process> processes;

        public MainWindow()
        {
            InitializeComponent();

            Windows.ApplicationModel.DataTransfer.Clipboard.ContentChanged += (sender, args) =>
                {
                    if(Clipboard.ContainsImage())
                    {
                        var bitmapSource = Clipboard.GetImage();
                        this.Image.Source = bitmapSource;
                        
                        OpenCvSharp.Mat m = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(bitmapSource);

                        testWindow.setTestMat(m);
                    }
                };
        }

        private void onLoaded(object sender, RoutedEventArgs e)
        {
            testWindow = new TestWindow();
            testWindow.Show();

            getProcessList();            
        }

        private void getProcessList()
        {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8))
            {
                var processesWithWindows = from p in Process.GetProcesses()
                                           where !string.IsNullOrWhiteSpace(p.MainWindowTitle) && WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                                           select p;
                processes = new ObservableCollection<Process>(processesWithWindows);
                ListBoxProcess.ItemsSource = processes;
            }
            else
            {
                ListBoxProcess.ItemsSource = null;
            }
        }

        private void onList(object sender, RoutedEventArgs e)
        {
            getProcessList();
        }

        private void onListBoxSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var listBox = (System.Windows.Controls.ListBox)sender;
            var process = (Process)listBox.SelectedItem;

            if (process != null)
            {
                var hwnd = process.MainWindowHandle;
                try
                {
                    testWindow.startHwndCapture(hwnd);
                }
                catch (Exception)
                {
                    Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                    processes.Remove(process);
                }
            }
        }

        void templateMatching(OpenCvSharp.Mat src,  OpenCvSharp.Mat temp)
        {
            OpenCvSharp.Mat ori = src.Clone();
            OpenCvSharp.Mat result = new OpenCvSharp.Mat();


            int result_cols = src.Cols - temp.Cols + 1;
            int result_rows = src.Rows - temp.Rows + 1;

            OpenCvSharp.Cv2.CvtColor(src, src, OpenCvSharp.ColorConversionCodes.BGRA2GRAY);
            OpenCvSharp.Cv2.CvtColor(temp, temp, OpenCvSharp.ColorConversionCodes.BGR2GRAY);

            result = new OpenCvSharp.Mat(result_rows, result_cols, OpenCvSharp.MatType.CV_32FC1);

            OpenCvSharp.TemplateMatchModes mode = OpenCvSharp.TemplateMatchModes.SqDiffNormed;

            OpenCvSharp.Cv2.MatchTemplate(src, temp, result, mode);
            OpenCvSharp.Cv2.Normalize(result, result, 0, 1, OpenCvSharp.NormTypes.MinMax, -1, new OpenCvSharp.Mat());

            /*
            double min;
            double max;

            OpenCvSharp.Cv2.MinMaxLoc(result, out min, out max);
            */
            float threshold;
            float value;

            float minVal = 0.05f;
            float maxVal = 0.95f;

            OpenCvSharp.Point matchLoc = new OpenCvSharp.Point();

            for (int y=0; y<result_rows; y++)
            {
                for(int x=0; x<result_cols; x++)
                {
                    value = result.At<float>(y, x);

                    switch(mode)
                    {
                        case OpenCvSharp.TemplateMatchModes.SqDiff:
                        case OpenCvSharp.TemplateMatchModes.SqDiffNormed:
                            threshold = minVal;

                            if(value < threshold)
                            {
                                matchLoc.X = x;
                                matchLoc.Y = y;                                
                            }
                            break;
                        default:
                            threshold = maxVal;

                            if (value > threshold)
                            {
                                matchLoc.X = x;
                                matchLoc.Y = y;
                            }
                            break;
                    }

                    OpenCvSharp.Cv2.Rectangle(ori, matchLoc, new OpenCvSharp.Point(matchLoc.X + temp.Cols, matchLoc.Y + temp.Rows), OpenCvSharp.Scalar.Red, 10);

                }
            }

            //OpenCvSharp.Cv2.ImWrite("result.jpg", ori);

            if (captureWindow == null)
            { 
                captureWindow = new CaptureWindow();
                captureWindow.Show();
            }

            captureWindow.setMat(ori);
        }

        private void onClickFind(object sender, RoutedEventArgs e)
        {
            OpenCvSharp.Mat m = testWindow.getMat();
            OpenCvSharp.Mat testMat = OpenCvSharp.WpfExtensions.BitmapSourceConverter.ToMat(Clipboard.GetImage());

            if (m == null || m.Empty())
            {
                MessageBox.Show("capture window first");
                return;
            }

            if (testMat == null || testMat.Empty())
            {
                MessageBox.Show("copy image to clipboard first");
                return;
            }

            templateMatching(m, testMat);
        }
    }
}
