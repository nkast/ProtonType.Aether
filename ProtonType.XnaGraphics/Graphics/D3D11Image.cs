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
using System.Windows.Interop;
using Microsoft.Xna.Framework.Graphics;
using Texture = SharpDX.Direct3D9.Texture;
using Surface = SharpDX.Direct3D9.Surface;


namespace tainicom.ProtonType.XnaGraphics
{
    /// <summary>
    /// Wraps the <see cref="D3DImage"/> to make it compatible with Direct3D 11.
    /// </summary>
    /// <remarks>
    /// The <see cref="D3D11Image"/> should be disposed if no longer needed!
    /// </remarks>
    public class D3D11Image : D3DImage,
        IDisposable
    {
        #region Fields
        // Use a Direct3D 9 device for interoperability. The device is shared by 
        // all D3D11Images.
        private static D3D9 _d3D9;
        private static int _referenceCount;
        private static readonly object _d3d9Lock = new object();

        private bool _disposed;
        internal Texture _backBuffer;
        #endregion


        #region Creation & Cleanup
        /// <summary>
        /// Initializes a new instance of the <see cref="D3D11Image"/> class.
        /// </summary>
        public D3D11Image()
        {
            InitializeD3D9();
        }


        /// <summary>
        /// Releases unmanaged resources before an instance of the <see cref="D3D11Image"/> class is 
        /// reclaimed by garbage collection.
        /// </summary>
        /// <remarks>
        /// This method releases unmanaged resources by calling the virtual <see cref="Dispose(bool)"/> 
        /// method, passing in <see langword="false"/>.
        /// </remarks>
        ~D3D11Image()
        {
            Dispose(false);
        }


        /// <summary>
        /// Releases all resources used by an instance of the <see cref="D3D11Image"/> class.
        /// </summary>
        /// <remarks>
        /// This method calls the virtual <see cref="Dispose(bool)"/> method, passing in 
        /// <see langword="true"/>, and then suppresses finalization of the instance.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        /// <summary>
        /// Releases the unmanaged resources used by an instance of the <see cref="D3D11Image"/> class 
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release both managed and unmanaged resources; 
        /// <see langword="false"/> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources.
                    SetBackBuffer2(null);
                    if (_backBuffer != null)
                    {
                        _backBuffer.Dispose();
                        _backBuffer = null;
                    }
                }

                // Release unmanaged resources.
                UninitializeD3D9();
                _disposed = true;
            }
        }
        #endregion


        #region Methods
        /// <summary>
        /// Initializes the Direct3D 9 device.
        /// </summary>
        private static void InitializeD3D9()
        {
            lock (_d3d9Lock)
            {
                _referenceCount++;
                if (_referenceCount == 1)
                    _d3D9 = new D3D9();
            }
        }


        /// <summary>
        /// Un-initializes the Direct3D 9 device, if no longer needed.
        /// </summary>
        private static void UninitializeD3D9()
        {
            lock (_d3d9Lock)
            {
                _referenceCount--;
                if (_referenceCount == 0)
                {
                    _d3D9.Dispose();
                    _d3D9 = null;
                }
            }
        }


        internal void ThrowIfDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().FullName);
        }
        
        /// <summary>
        /// Sets the back buffer of the <see cref="D3D11Image"/>.
        /// </summary>
        /// <param name="renderTarget">The Direct3D 11 texture to be used as the back buffer.</param>
        public void SetBackBuffer2(RenderTarget2D renderTarget)
        {
            var previousBackBuffer = _backBuffer;

            // Create shared texture on Direct3D 9 device.
            if (renderTarget == null)
                _backBuffer = null;
            else
                _backBuffer = _d3D9.GetSharedTexture(renderTarget);

            if (_backBuffer != null)
            {
                // Set texture as new back buffer.
                using (Surface surface = _backBuffer.GetSurfaceLevel(0))
                {
                    this.Lock();
                    this.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
                    this.Unlock();
                }
            }
            else
            {
                // Reset back buffer.
                this.Lock();
                this.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
                this.Unlock();
            }

            if (previousBackBuffer != null)
                previousBackBuffer.Dispose();
        }
        #endregion
    }
}
