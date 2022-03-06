// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

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

namespace tainicom.ProtonType.XnaContentPipeline.ProxyClient
{
    /// <summary>
    /// Defines a set of graphic capabilities.
    /// </summary>
    public enum ProxyTargetPlatform
    {
       
        /// <summary>
        /// All desktop versions of Windows using DirectX.
        /// </summary>
        Windows,

        /// <summary>
        /// Xbox 360 video game and entertainment system
        /// </summary>
        Xbox360,

        /// <summary>
        /// Windows Phone
        /// </summary>
        WindowsPhone,

        // MonoGame-specific platforms listed below

        /// <summary>
        /// Apple iOS-based devices (iPod Touch, iPhone, iPad)
        /// (MonoGame)
        /// </summary>
        iOS,

        /// <summary>
        /// Android-based devices
        /// (MonoGame)
        /// </summary>
        Android,

        /// <summary>
        /// All desktop versions using OpenGL.
        /// (MonoGame)
        /// </summary>
        DesktopGL,

        /// <summary>
        /// Apple Mac OSX-based devices (iMac, MacBook, MacBook Air, etc)
        /// (MonoGame)
        /// </summary>
        MacOSX,

        /// <summary>
        /// Windows Store App
        /// (MonoGame)
        /// </summary>
        WindowsStoreApp,

        /// <summary>
        /// Google Chrome Native Client
        /// (MonoGame)
        /// </summary>
        NativeClient,

        /// <summary>
        /// Windows Phone 8
        /// (MonoGame)
        /// </summary>
        WindowsPhone8,

        /// <summary>
        /// Raspberry Pi
        /// (MonoGame)
        /// </summary>
        RaspberryPi,

        /// <summary>
        /// Sony PlayStation4
        /// </summary>
        PlayStation4,

        /// <summary>
        /// Sony PlayStation5
        /// </summary>
        PlayStation5,

        /// <summary>
        /// Xbox One
        /// </summary>
        XboxOne,

        /// <summary>
        /// Nintendo Switch
        /// </summary>
        Switch,

        /// <summary>
        /// Google Stadia
        /// </summary>
        Stadia,

        /// <summary>
        /// WebAssembly and Bridge.NET
        /// </summary>
        Web
    }
}
