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

namespace tainicom.ProtonType.XnaGraphics.Controls
{
    public class ScreenTypeDesc
    {
        public string Name { get; set; }
        public readonly int Width;
        public readonly int Height;
        public readonly bool ZoomToFit;

        internal static ScreenTypeDesc Fill = new ScreenTypeDesc("*", 0, 0, true);

        public static ScreenTypeDesc[] ScreenTypes = new ScreenTypeDesc[] 
        { 
            new ScreenTypeDesc("*",0,0,true),
            new ScreenTypeDesc("5:3",800,480, true),
            new ScreenTypeDesc("16:9",1280,720, true),
            new ScreenTypeDesc("4:3",1024,768, true),
            new ScreenTypeDesc("16:10",1280,800, true),
            
            new ScreenTypeDesc("(800x480) 5:3 - WVGA",800,480, false),//WP7
            new ScreenTypeDesc("(1280x720) 16:9 - HD720",1280,720, false),//WP7/8
            new ScreenTypeDesc("(1280x768) 5:3 - WXGA",1280,768, false),//WP7/8            
            new ScreenTypeDesc("(1024x768) 4:3 - XGA",1024,768, false), //W8
            new ScreenTypeDesc("(1366x768) ~16:9",1366,768, false), //W8
            new ScreenTypeDesc("(1280x800) 16:10 - WXGA",1280,800, false), //W8
            
            new ScreenTypeDesc("(400x240) 5:3",400,240, false),
            new ScreenTypeDesc("(640X360) 16:9",640,360, false),
            new ScreenTypeDesc("(512x384) 4:3",512,384, false),
            new ScreenTypeDesc("(640x400) 16:10",640,400, false),

            new ScreenTypeDesc("3:5",480,800, true),
            new ScreenTypeDesc("9:16",720,1280, true),
            new ScreenTypeDesc("3:4",768,1024, true),
            new ScreenTypeDesc("10:16",800,1280, true),
                        
            new ScreenTypeDesc("(512x512) 1:1",512,512, false),
        };

        public ScreenTypeDesc(string name, int width, int height, bool zoomToFit)
        {
            this.Name = name;
            this.Width = width;
            this.Height = height;
            this.ZoomToFit = zoomToFit;
        }
    } 

}
