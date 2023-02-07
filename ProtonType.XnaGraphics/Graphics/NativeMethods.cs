#region File Description
//-----------------------------------------------------------------------------	
// Copyright 2011, Nick Gravelyn.	
// Licensed under the terms of the Ms-PL:	
// http://www.microsoft.com/opensource/licenses.mspx#Ms-PL	
//-----------------------------------------------------------------------------	
#endregion

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaGraphics
{
    /// <summary>	
    /// An internal set of functionality used for interopping with native Win32 APIs.	
    /// </summary>	
    internal static class NativeMethods
    {
        #region Interfaces

        [ComImport, Guid("85C31227-3DE5-4f00-9B3A-F11AC38C18B5"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IDirect3DTexture9
        {
            void GetDevice();
            void SetPrivateData();
            void GetPrivateData();
            void FreePrivateData();
            void SetPriority();
            void GetPriority();
            void PreLoad();
            void GetType();
            void SetLOD();
            void GetLOD();
            void GetLevelCount();
            void SetAutoGenFilterType();
            void GetAutoGenFilterType();
            void GenerateMipSubLevels();
            void GetLevelDesc();
            int GetSurfaceLevel(uint level, out IntPtr surfacePointer);
        }

        #endregion

        #region Wrapper methods

        public static IntPtr GetRenderTargetSurface(RenderTarget2D renderTarget)
        {
            IntPtr texPointer;
            IDirect3DTexture9 texture;
            IntPtr surfacePointer;

            unsafe
            {
                var texPtrField = renderTarget.GetType().GetField("pComPtr", BindingFlags.NonPublic | BindingFlags.Instance);
                var texPtr = texPtrField.GetValue(renderTarget);
                texPointer = new IntPtr(Pointer.Unbox(texPtr));
                texture = (IDirect3DTexture9)Marshal.GetObjectForIUnknown(texPointer);
            }
            var hr = texture.GetSurfaceLevel(0, out surfacePointer);
            Marshal.ThrowExceptionForHR(hr);
            Marshal.ReleaseComObject(texture);
            return surfacePointer;
        }

        #endregion
    }
}