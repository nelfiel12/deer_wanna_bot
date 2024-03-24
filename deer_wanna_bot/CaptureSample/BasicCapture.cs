using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.DirectX;
using Windows.Graphics;
using Windows.UI.Composition;
using Composition.WindowsRuntimeHelpers;
using OpenCvSharp;
using SharpDX;
using SharpDX.Direct3D11;
using System.Drawing.Imaging;
using System.Windows.Media.Media3D;
using SharpDX.DXGI;
using System.Diagnostics;
using System.Drawing;
using static SharpDX.Utilities;
using System.IO;

namespace deer_wanna_bot.CaptureSample
{
    public class BasicCapture : IDisposable
    {
        private GraphicsCaptureItem item;
        private Direct3D11CaptureFramePool framePool;
        private GraphicsCaptureSession session;
        private SizeInt32 lastSize;

        private IDirect3DDevice device;
        private SharpDX.Direct3D11.Device d3dDevice;
        private SharpDX.DXGI.SwapChain1 swapChain;

        public event EventHandler<OpenCvSharp.Mat> TestEvent;

        public OpenCvSharp.Mat lastMat;

        public BasicCapture(IDirect3DDevice d, GraphicsCaptureItem i)
        {
            item = i;
            device = d;
            d3dDevice = Direct3D11Helper.CreateSharpDXDevice(device);

            var dxgiFactory = new SharpDX.DXGI.Factory2();
            var description = new SharpDX.DXGI.SwapChainDescription1()
            {
                Width = item.Size.Width,
                Height = item.Size.Height,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription()
                {
                    Count = 1,
                    Quality = 0
                },
                Usage = SharpDX.DXGI.Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
                AlphaMode = SharpDX.DXGI.AlphaMode.Premultiplied,
                Flags = SharpDX.DXGI.SwapChainFlags.None
            };
            swapChain = new SharpDX.DXGI.SwapChain1(dxgiFactory, d3dDevice, ref description);

            framePool = Direct3D11CaptureFramePool.Create(
                device,
                DirectXPixelFormat. B8G8R8A8UIntNormalized,
                2,
                i.Size);
            session = framePool.CreateCaptureSession(i);
            lastSize = i.Size;

            framePool.FrameArrived += OnFrameArrived;
        }

        public void Dispose()
        {
            session?.Dispose();
            framePool?.Dispose();
            swapChain?.Dispose();
            d3dDevice?.Dispose();
        }

        public void StartCapture()
        {
            session.StartCapture();
        }

        public ICompositionSurface CreateSurface(Compositor compositor)
        {
            return compositor.CreateCompositionSurfaceForSwapChain(swapChain);
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
        {
            var newSize = false;

            using (var frame = sender.TryGetNextFrame())
            {
                if (frame.ContentSize.Width != lastSize.Width ||
                    frame.ContentSize.Height != lastSize.Height)
                {
                    // The thing we have been capturing has changed size.
                    // We need to resize the swap chain first, then blit the pixels.
                    // After we do that, retire the frame and then recreate the frame pool.
                    newSize = true;
                    lastSize = frame.ContentSize;
                    swapChain.ResizeBuffers(
                        2,
                        lastSize.Width,
                        lastSize.Height,
                        SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                        SharpDX.DXGI.SwapChainFlags.None);
                }

                using (var backBuffer = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0))
                using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface))
                {
                    d3dDevice.ImmediateContext.CopyResource(bitmap, backBuffer);

                    convertToMap(bitmap);
                }

            } // Retire the frame.

            swapChain.Present(0, SharpDX.DXGI.PresentFlags.None);

            if (newSize)
            {
                framePool.Recreate(
                    device,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized,
                    2,
                    lastSize);
            }
        }

        private void convertToMap(Texture2D texture)
        {
            var sw = new Stopwatch();
            sw.Start();

            // Create texture copy
            var copy = new Texture2D(d3dDevice, new Texture2DDescription
            {
                Width = texture.Description.Width,
                Height = texture.Description.Height,
                MipLevels = 1,
                ArraySize = 1,
                Format = texture.Description.Format,
                Usage = ResourceUsage.Staging,
                SampleDescription = new SampleDescription(1, 0),
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                OptionFlags = ResourceOptionFlags.None
            });

            // Copy data
            d3dDevice.ImmediateContext.CopyResource(texture, copy);
            var mapSource = d3dDevice.ImmediateContext.MapSubresource(copy, 0, MapMode.Read, SharpDX.Direct3D11.MapFlags.None);

            var sourcePtr = mapSource.DataPointer;

            OpenCvSharp.Mat m = new OpenCvSharp.Mat(copy.Description.Height, copy.Description.Width, MatType.CV_8UC4, mapSource.DataPointer);

            var mPtr = m.Data;

            for (int y = 0; y < m.Rows; y++)
            {
                Utilities.CopyMemory(mPtr, sourcePtr, m.Cols * m.Channels());

                sourcePtr = IntPtr.Add(sourcePtr, mapSource.RowPitch);
                mPtr = IntPtr.Add(mPtr, m.Cols * m.Channels());
            }

            d3dDevice.ImmediateContext.UnmapSubresource(copy, 0);
            copy.Dispose();

            lastMat = m;

            TestEvent?.Invoke(this, m);
            sw.Stop();
            Debug.WriteLine($"SW: {sw.ElapsedMilliseconds} ms");
        }
    }
}
