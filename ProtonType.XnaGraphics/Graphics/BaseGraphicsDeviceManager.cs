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
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaGraphics
{
    public abstract class BaseGraphicsDeviceManager : IGraphicsDeviceManager, IDisposable
    {
        protected const int PreferredMultiSampleCount = 4;

        abstract public int ClientWidth { get; }
        abstract public int ClientHeight { get; }

        abstract public System.Windows.Size RenderSize { get; }


        protected bool IsDisposed { get { return isDisposed; } }

        public BaseGraphicsDeviceManager()
        {

        }
        
        abstract public RenderTarget2D DefaultRenderTarget { get; }

        // However many GraphicsDeviceControl instances you have, they all share
        // the same underlying GraphicsDevice, managed by this helper service.
        protected GraphicsDeviceService _graphicsDeviceService;

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        protected GraphicsDevice GraphicsDevice
        {
            get
            {
                if (IsDisposed)
                    throw new InvalidOperationException();
                if (_graphicsDeviceService == null)
                    CreateDevice();
                return _graphicsDeviceService.GraphicsDevice;
            }
        }

        #region IDisposable implementation

        bool isDisposed = false;

        ~BaseGraphicsDeviceManager()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed) return;

            if (disposing)
            {
                if (_graphicsDeviceService != null)
                    _graphicsDeviceService.Release(disposing);
            }

            _graphicsDeviceService = null;
                        
            isDisposed = true;
        }

        #endregion IDisposable implementation


        #region IGraphicsDeviceManager implementation

        abstract public bool BeginDraw();
        abstract public void EndDraw();

        public virtual void CreateDevice()
        {
            
        }
        
        #endregion  IGraphicsDeviceManager implementation



    }
}
