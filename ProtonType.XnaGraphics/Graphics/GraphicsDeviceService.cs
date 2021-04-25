#region File Description
//-----------------------------------------------------------------------------
// GraphicsDeviceService.cs
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
using Microsoft.Xna.Framework.Graphics;

// The IGraphicsDeviceService interface requires a DeviceCreated event, but we
// always just create the device inside our constructor, so we have no place to
// raise that event. The C# compiler warns us that the event is never used, but
// we don't care so we just disable this warning.
#pragma warning disable 67

namespace tainicom.ProtonType.XnaGraphics
{
    /// <summary>
    /// Helper class responsible for creating and managing the GraphicsDevice.
    /// All GraphicsDeviceControl instances share the same GraphicsDeviceService,
    /// so even though there can be many controls, there will only ever be a single
    /// underlying GraphicsDevice. This implements the standard IGraphicsDeviceService
    /// interface, which provides notification events for when the device is reset
    /// or disposed.
    /// </summary>
    public class GraphicsDeviceService : IGraphicsDeviceService
    {
        #region Fields

        // Singleton device service instance.
        static GraphicsDeviceService singletonInstance;
                
        // Store the current device settings.
        PresentationParameters parameters;

        // Keep track of how many controls are sharing the singletonInstance.
        static int referenceCount;

        #endregion


        /// <summary>
        /// Gets the current graphics device.
        /// </summary>
        public GraphicsDevice GraphicsDevice { get; private set; }



        /// <summary>
        /// Constructor is private, because this is a singleton class:
        /// client controls should use the public AddRef method instead.
        /// </summary>
        GraphicsDeviceService()
        {
        }

        private void CreateDevice(IntPtr windowHandle, int width, int height)
        {
            GraphicsAdapter adapter = GraphicsAdapter.DefaultAdapter;
            GraphicsProfile graphicsProfile = GraphicsProfile.Reach;

            parameters = new PresentationParameters();
            parameters.BackBufferWidth = Math.Max(width, 1);
            parameters.BackBufferHeight = Math.Max(height, 1);
            parameters.BackBufferFormat = SurfaceFormat.Color;
            parameters.DepthStencilFormat = DepthFormat.Depth24Stencil8;
            parameters.MultiSampleCount = 4;
            parameters.DeviceWindowHandle = windowHandle;
            parameters.PresentationInterval = PresentInterval.Immediate;
            parameters.IsFullScreen = false;
            
            if (adapter.IsProfileSupported(GraphicsProfile.HiDef))
                graphicsProfile = GraphicsProfile.HiDef;

            CreateDevice(GraphicsAdapter.DefaultAdapter, graphicsProfile, parameters);
        }
                
        /// <summary>
        /// Gets a reference to the singleton instance.
        /// </summary>
        public static GraphicsDeviceService AddRef(IntPtr windowHandle, int width, int height)
        {
            // Increment the "how many controls sharing the device" reference count.
            // TODO: remove referenceCount and provide method to create/destroy the device.
            if (System.Threading.Interlocked.Increment(ref referenceCount) == 1)
            {
                // If this is the first control to start using the
                // device, we must create the singleton instance.
                singletonInstance = new GraphicsDeviceService();
                singletonInstance.CreateDevice(windowHandle, width, height);
            }

            return singletonInstance;
        }


        /// <summary>
        /// Releases a reference to the singleton instance.
        /// </summary>
        public void Release(bool disposing)
        {
            // Decrement the "how many controls sharing the device" reference count.
            // TODO: remove referenceCount and provide method to create/destroy the device.
            if (System.Threading.Interlocked.Decrement(ref referenceCount) == 0)
            {
                // If this is the last control to finish using the
                // device, we should dispose the singleton instance.
                if (disposing)
                {
                    OnDeviceDisposing(this, EventArgs.Empty);
                    GraphicsDevice.Dispose();
                }

                GraphicsDevice = null;
            }
        }


        private void CreateDevice(GraphicsAdapter adapter, GraphicsProfile graphicsProfile, PresentationParameters presentationParameters)
        {
            if (GraphicsDevice != null)
                throw new InvalidOperationException("Device allready exists");

            GraphicsDevice = new GraphicsDevice(adapter, graphicsProfile, presentationParameters);
            OnDeviceCreated(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// Resets the graphics device to whichever is bigger out of the specified
        /// resolution or its current size. This behavior means the device will
        /// demand-grow to the largest of all its GraphicsDeviceControl clients.
        /// </summary>
        public void ResetDevice(int width, int height)
        {
            OnDeviceResetting(this, EventArgs.Empty);

            parameters.BackBufferWidth = Math.Max(parameters.BackBufferWidth, width);
            parameters.BackBufferHeight = Math.Max(parameters.BackBufferHeight, height);

            GraphicsDevice.Reset(parameters);

            OnDeviceReset(this, EventArgs.Empty);
        }
        
        // IGraphicsDeviceService events.
        public event EventHandler<EventArgs> DeviceCreated;
        public event EventHandler<EventArgs> DeviceResetting;
        public event EventHandler<EventArgs> DeviceReset;
        public event EventHandler<EventArgs> DeviceDisposing;
        
        private void OnDeviceCreated(object sender, EventArgs e)
        {
            var handler = DeviceCreated;
            if (handler != null)
                handler(this, e);
        }

        private void OnDeviceResetting(object sender, EventArgs e)
        {
            var handler = DeviceResetting;
            if (handler != null)
                handler(this, e);
        }

        private void OnDeviceReset(object sender, EventArgs e)
        {
            var handler = DeviceReset;
            if (handler != null)
                handler(this, e);
        }

        private void OnDeviceDisposing(object sender, EventArgs e)
        {
            var handler = DeviceDisposing;
            if (handler != null)
                handler(this, e);
        }
    }
}

