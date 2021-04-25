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
	public enum ProxyGraphicsProfile
    {
        /// <summary>
        /// Use a limited set of graphic features and capabilities, allowing the game to support the widest variety of devices.
        /// </summary>
        Reach = 0,
        /// <summary>
        /// Use the largest available set of graphic features and capabilities to target devices, that have more enhanced graphic capabilities.        
        /// </summary>
        HiDef = 1,

        FL10_0 = 2,
        FL10_1 = 3,
        FL11_0 = 4,
        FL11_1 = 5,
	}
}
