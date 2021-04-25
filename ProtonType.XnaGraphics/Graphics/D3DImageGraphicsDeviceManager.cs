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
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;
using Matrix = Microsoft.Xna.Framework.Matrix;


namespace tainicom.ProtonType.XnaGraphics
{
    public class D3DImageGraphicsDeviceManager : BaseGraphicsDeviceManager
    {
        private readonly Image _control;
        private RenderTarget2D _renderTarget;
#if MONOGAME
        private D3D11Image _imageSource;
#else
        private System.Windows.Interop.D3DImage _imageSource;
#endif

        private bool _resetBackBuffer;

        /// <summary>
        /// Gets a value indicating whether the controls runs in the context of a designer (e.g.
        /// Visual Studio Designer or Expression Blend).
        /// </summary>
        /// <value>
        /// <see langword="true" /> if controls run in design mode; otherwise, 
        /// <see langword="false" />.
        /// </value>
        public bool IsInDesignMode
        {
            get
            {
                if (!_isInDesignMode.HasValue)
                    _isInDesignMode = (bool)DependencyPropertyDescriptor.FromProperty(DesignerProperties.IsInDesignModeProperty, typeof(FrameworkElement)).Metadata.DefaultValue;

                return _isInDesignMode.Value;
            }
        }
        private bool? _isInDesignMode;
        
        public override RenderTarget2D DefaultRenderTarget { get { return _renderTarget; } }

        public override int ClientWidth { get { return (int)_control.ActualWidth; } }
        public override int ClientHeight { get { return (int)_control.ActualHeight; } }

        public override System.Windows.Size RenderSize { get { return new System.Windows.Size(ClientWidth, ClientHeight); } }

       
        #region Constructors
        public D3DImageGraphicsDeviceManager(Image image) : base()
        {
            _control = image;
            
            if (IsInDesignMode)
                return;
            
            CreateDevice();
            InitializeImageSource();

            //Initialize;
            _control.SizeChanged += _image_SizeChanged;
        }
        #endregion
        
        #region IDisposable implementation
        
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed) return;

            if (disposing)
            {
                _control.Source = null;
#if MONOGAME 
                if (_imageSource != null)
                    _imageSource.Dispose();
#endif
                if (_renderTarget != null)
                    _renderTarget.Dispose();
            }
            
            _imageSource = null;
            _renderTarget = null;

            base.Dispose(disposing);
        }
        #endregion
        
        private void InitializeImageSource()
        {
#if MONOGAME 
            _imageSource = new D3D11Image();
#else
            _imageSource = new System.Windows.Interop.D3DImage();
#endif
            _imageSource.IsFrontBufferAvailableChanged += OnIsFrontBufferAvailableChanged;
            CreateBackBuffer();
            _control.Source = _imageSource;
        }

        private void CreateBackBuffer()
        {
#if MONOGAME 
            _imageSource.ThrowIfDisposed();
            _imageSource.SetBackBuffer2(null);
#else
            _imageSource.Lock();
            _imageSource.SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
            _imageSource.Unlock();
#endif

            if (_renderTarget != null)
            {
                _renderTarget.Dispose();
                _renderTarget = null;
            }

            int width = Math.Max((int)_control.ActualWidth, 1);
            int height = Math.Max((int)_control.ActualHeight, 1);
#if MONOGAME
            SurfaceFormat surfaceFormat = SurfaceFormat.Bgra32;
            _renderTarget = new RenderTarget2D(this.GraphicsDevice, width, height, false, surfaceFormat, DepthFormat.Depth24Stencil8, PreferredMultiSampleCount, RenderTargetUsage.DiscardContents, true);
#else
            SurfaceFormat surfaceFormat = SurfaceFormat.Color;
            _renderTarget = new RenderTarget2D(this.GraphicsDevice, width, height, false, surfaceFormat, DepthFormat.Depth24Stencil8, PreferredMultiSampleCount, RenderTargetUsage.DiscardContents);
#endif

#if MONOGAME 
            _imageSource.ThrowIfDisposed();
            _imageSource.SetBackBuffer2(_renderTarget);
#else
            var rtPtr = NativeMethods.GetRenderTargetSurface(_renderTarget);
            _imageSource.Lock();
            _imageSource.SetBackBuffer(System.Windows.Interop.D3DResourceType.IDirect3DSurface9, rtPtr);
            _imageSource.Unlock();
#endif
        }
        
        public override void CreateDevice()
        {
            if (this._graphicsDeviceService == null)
            {
                var window = Application.Current.MainWindow;
                IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(Application.Current.MainWindow).EnsureHandle();
                _graphicsDeviceService = GraphicsDeviceService.AddRef(hWnd,
                    1, 1);
            }
        }
                
        public override bool BeginDraw()
        {
            // Recreate back buffer if necessary.
            if (_resetBackBuffer)
                CreateBackBuffer();

            GraphicsDevice.SetRenderTarget(_renderTarget);
            GraphicsDevice.Clear(Color.Black);

            return true;
        }
        
        public override void EndDraw()
        {
#if MONOGAME
            GraphicsDevice.Flush();
#endif
            // resolve texture
            GraphicsDevice.SetRenderTarget(null);

#if MONOGAME
            _imageSource.ThrowIfDisposed();

            if (_imageSource._backBuffer != null)
            {
                _imageSource.Lock();
                _imageSource.AddDirtyRect(new Int32Rect(0, 0, _imageSource.PixelWidth, _imageSource.PixelHeight));
                _imageSource.Unlock();
            }
#else
            if (_imageSource != null)
            {
                _imageSource.Lock();
                _imageSource.AddDirtyRect(new Int32Rect(0, 0, _imageSource.PixelWidth, _imageSource.PixelHeight));
                _imageSource.Unlock();
            }
#endif

            _resetBackBuffer = false;
        }


        /// <summary>
        /// Raises the <see cref="FrameworkElement.SizeChanged" /> event, using the specified 
        /// information as part of the eventual event data.
        /// </summary>
        /// <param name="sizeInfo">Details of the old and new size involved in the change.</param>
        void _image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _resetBackBuffer = true;
        }

        private void OnIsFrontBufferAvailableChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
        {
            if (_imageSource.IsFrontBufferAvailable)
                _resetBackBuffer = true;
        }

    }
}
