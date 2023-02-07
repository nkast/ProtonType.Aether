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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using SysDrawing = System.Drawing;
using System.Runtime.InteropServices;

namespace nkast.ProtonType.XnaGraphics.Controls
{
    /// <summary>
    /// Interaction logic for XNAImage.xaml
    /// </summary>
    public class XNAImage : Image
    {
        WinForms.Form _hWindow;
        
        public XNAImage():base()
        {
            this.SnapsToDevicePixels = true;
            this.UseLayoutRounding = true;

            this.Focusable = true;
            
            // create a borderless hWindow to dispatch input data to the Mouse.
            _hWindow = new WinForms.Form();
            _hWindow.FindForm().FormBorderStyle = WinForms.FormBorderStyle.None;
            _hWindow.Padding = new WinForms.Padding(0);
            _hWindow.Margin = new WinForms.Padding(0);
            _hWindow.Size = new System.Drawing.Size(0, 0);

            this.MouseWheel += XNAImage_MouseWheel;
        }


        const int WM_MOUSEWHEEL = 0x020A;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr windowHandle, uint Msg, IntPtr wParam, IntPtr lParam);

        private static System.Drawing.Point LocationFromLParam(IntPtr lParam)
        {
            short x = (short)((((long)lParam) >> 0) & 0xffff);
            short y = (short)((((long)lParam) >> 16) & 0xffff);
            return new System.Drawing.Point(x, y);
        }

        private int DeltaToWParam(int p0, int delta)
        {
            return ((delta << 16) | (p0 & 0xFFFF));
        }

        private static int DeltaFromWParam(IntPtr wParam)
        {
            return (short)((((long)wParam) >> 16) & 0xffff);
        }

        void XNAImage_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // raise event for winforms control
            {
                IntPtr res = SendMessage(_hWindow.Handle, (uint)WM_MOUSEWHEEL, new IntPtr(DeltaToWParam(0, e.Delta)), new IntPtr(0));
            }
        }
        
        protected override Size MeasureOverride(Size constraint)
        {
            var result = base.MeasureOverride(constraint);
            return constraint;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var result = base.ArrangeOverride(arrangeSize);

            // update 
            var pos = this.PointToScreen(new Point(0,0));
            _hWindow.Location = new SysDrawing.Point((int)pos.X, (int)pos.Y);

            return arrangeSize;
        }

        protected override void OnGotFocus(RoutedEventArgs e)
        {
            Microsoft.Xna.Framework.Input.Mouse.WindowHandle = _hWindow.Handle;
            //Microsoft.Xna.Framework.Input.Touch.TouchPanel.WindowHandle = this.EditorControl.Handle;
        }
    }
}
