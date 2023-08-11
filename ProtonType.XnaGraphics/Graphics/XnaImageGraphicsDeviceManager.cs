#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceControl.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region License
//   Copyright 2021 Kastellanos Nikolaos
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
#endregion

using System;
using System.Windows;
using Microsoft.Xna.Framework.Graphics;
using nkast.ProtonType.XnaGraphics.Controls;

namespace nkast.ProtonType.XnaGraphics
{
    public class XnaImageGraphicsDeviceManager : BaseGraphicsDeviceManager
    {
        private readonly XNAImage _control;
        private RenderTarget2D _renderTarget;
        System.Windows.Media.Imaging.WriteableBitmap _imageSource;
        byte[] rawImage;
        
        
        public override RenderTarget2D DefaultRenderTarget { get { return _renderTarget; } }

        public override int ClientWidth { get { return (int)_control.ActualWidth; } }
        public override int ClientHeight { get { return (int)_control.ActualHeight; } }

        public override System.Windows.Size RenderSize { get { return new System.Windows.Size(ClientWidth, ClientHeight); } }


        public XnaImageGraphicsDeviceManager(XNAImage control)
        {
            this._control = control;

            CreateDevice();
        }
        
        
        #region IDisposable implementation        
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                if (_renderTarget != null)
                    _renderTarget.Dispose();
            }

            _renderTarget = null;

            base.Dispose(disposing);
        }
        #endregion
        
        public override void CreateDevice()
        {
            if (this._graphicsDeviceService == null)
            {
                var window = Application.Current.MainWindow;
                IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).EnsureHandle();
                this._graphicsDeviceService = GraphicsDeviceService.AddRef(hWnd,
                     1, 1);
            }
        }

        /// <summary>
        /// Attempts to begin drawing the control. Returns an error message string
        /// if this was not possible, which can happen if the graphics device is
        /// lost, or if we are running inside the Form designer.
        /// </summary>
        public override bool BeginDraw()
        {
            if (this._graphicsDeviceService == null)
                return false;

            // Make sure the graphics device is big enough, and is not lost.
            this.HandleDeviceReset();

            this.GraphicsDevice.SetRenderTarget(this._renderTarget);

            int w = Math.Max(1, ClientWidth);
            int h = Math.Max(1, ClientHeight);
            var viewport = new Microsoft.Xna.Framework.Graphics.Viewport(0, 0, w, h);

            // Many GraphicsDeviceControl instances can be sharing the same
            // GraphicsDevice. The device backbuffer will be resized to fit the
            // largest of these controls. But what if we are currently drawing
            // a smaller control? To avoid unwanted stretching, we set the
            // viewport to only use the top left portion of the full backbuffer.
            if (viewport.Width == 0 || viewport.Height == 0)
                throw new Exception("Viewport size cannot be Zero.");
            this.GraphicsDevice.Viewport = viewport;

            return true;
        }

        /// <summary>
        /// Ends drawing the control. This is called after derived classes
        /// have finished their Draw method, and is responsible for presenting
        /// the finished image onto the screen, using the appropriate WinForms
        /// control handle to make sure it shows up in the right place.
        /// </summary>
        public override void EndDraw()
        {
            try
            {
                this.GraphicsDevice.SetRenderTarget(null); // resolve _swapChainRenderTarget
                this.GraphicsDevice.Clear(Microsoft.Xna.Framework.Color.Black);
                
                int w = Math.Max(1, (int)this._control.ActualWidth);
                int h = Math.Max(1, (int)this._control.ActualHeight);
                System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Bgra32;
                int rawStride = (w * pf.BitsPerPixel + 7) / 8;
                int rawImageLen = rawStride * h;

                try
                {
                    DefaultRenderTarget.GetData(rawImage);

                    // Reserve the back buffer for updates.
                    _imageSource.Lock();
                    // Get a pointer to the back buffer.
                    IntPtr pBackBuffer = _imageSource.BackBuffer;

#if KNI
                    System.Runtime.InteropServices.Marshal.Copy(rawImage, 0, pBackBuffer, rawImageLen);
#else
                    CopyAndConvertRGBA2BGRA(rawImage, pBackBuffer, rawImageLen);
#endif

                    var dirtyRect = new System.Windows.Int32Rect(0, 0, w, h);
                    _imageSource.AddDirtyRect(dirtyRect);
                }
                finally
                {
                    // Release the back buffer and make it available for display.
                    _imageSource.Unlock();
                }
            }
            catch
            {
                // Present might throw if the device became lost while we were
                // drawing. The lost device will be handled by the next BeginDraw,
                // so we just swallow the exception.
            }
        }

        unsafe private void CopyAndConvertRGBA2BGRA(byte[] rawImage, IntPtr pBackBuffer, int rawImageLen)
        {
            {
                byte* pDst = (byte*)pBackBuffer.ToPointer();
                fixed (byte* pSrc = rawImage)
                {
                    byte* pSrc2 = pSrc;
                    var totalBytes = rawImageLen / 4;
                    System.Threading.Tasks.Parallel.For(0, totalBytes, (i) =>
                    {
                        pDst[i * 4 + 0] = pSrc2[i * 4 + 2];
                        pDst[i * 4 + 1] = pSrc2[i * 4 + 1];
                        pDst[i * 4 + 2] = pSrc2[i * 4 + 0];
                        pDst[i * 4 + 3] = pSrc2[i * 4 + 3];
                    });
                }
            }
        }
        
        /// <summary>
        /// Helper used by BeginDraw. This checks the graphics device status,
        /// making sure it is big enough for drawing the current control, and
        /// that the device is not lost. Returns an error string if the device
        /// could not be reset.
        /// </summary>
        private void HandleDeviceReset()
        {
            int w = Math.Max(1, ClientWidth);
            int h = Math.Max(1, ClientHeight);

            bool deviceNeedsReset = false;

            switch (this.GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Lost:
                    // If the graphics device is lost, we cannot use it at all.
                    throw new Exception("Graphics device lost");

                case GraphicsDeviceStatus.NotReset:
                    // If device is in the not-reset state, we should try to reset it.
                    deviceNeedsReset = true;
                    break;

                default:
                    // If the device state is ok, check whether it is big enough.
                    PresentationParameters pp = this.GraphicsDevice.PresentationParameters;
                    // in MonoGame we don't need to resize the main BackBuffer. We draw on _swapChainRenderTarget.
                    //deviceNeedsReset = (w > pp.BackBufferWidth) ||
                    //                   (h > pp.BackBufferHeight);
                    break;
            }

            // Do we need to reset the device?
            if (deviceNeedsReset)
            {
                try
                {
                    _graphicsDeviceService.ResetDevice(w, h);
                }
                catch (Exception e)
                {
                    throw new Exception("Graphics device reset failed\n\n", e);
                }
            }

            // check whether _swapChainRenderTarget is big enough.
            if (_renderTarget != null)
            {
                if (w != _renderTarget.Width || h != _renderTarget.Height)
                {
                    _renderTarget.Dispose();
                    _renderTarget = null;
                }
            }

            // recreate RenderTarget
            if (_renderTarget == null)
            {
                var surfaceFormat = Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color;
#if KNI
                surfaceFormat = Microsoft.Xna.Framework.Graphics.SurfaceFormat.Bgra32;
#else
                surfaceFormat = Microsoft.Xna.Framework.Graphics.SurfaceFormat.Color;
#endif
                _renderTarget = new RenderTarget2D(
                    this.GraphicsDevice, w, h,
                    false, surfaceFormat, DepthFormat.Depth24Stencil8, PreferredMultiSampleCount,
                    RenderTargetUsage.DiscardContents);
            }

            // / Define parameters used to create the BitmapSource.
            System.Windows.Media.PixelFormat pf = System.Windows.Media.PixelFormats.Bgra32;
            int rawStride = (w * pf.BitsPerPixel + 7) / 8;
            int rawImageLen = rawStride * h;
            
            // Create a BitmapSource.
            if (_imageSource == null || _imageSource.Width != w || _imageSource.Height != h)
            {
                _imageSource = new System.Windows.Media.Imaging.WriteableBitmap(w, h, 96, 96, pf, null);
                _control.Source = _imageSource;
            }

            if (rawImage == null || rawImage.Length != rawImageLen)
            {
                rawImage = new byte[rawImageLen];
            }

            return;
        }
    }
}
