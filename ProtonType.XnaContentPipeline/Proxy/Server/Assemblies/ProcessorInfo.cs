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
using Microsoft.Xna.Framework.Content.Pipeline;
using tainicom.ProtonType.XnaContentPipeline.Common;

namespace tainicom.ProtonType.XnaContentPipeline.ProxyServer.Assemblies
{
    class ProcessorInfo
    {
        public readonly Type Type;
        public readonly DateTime AssemblyTimestamp;
        public readonly ProcessorDescription Description;
        public readonly OpaqueDataDictionary DefaultValues;

        public ProcessorInfo(Type type, DateTime assemblyTimestamp, ProcessorDescription description, OpaqueDataDictionary defaultValues)
        {
            this.Type = type;
            this.AssemblyTimestamp = assemblyTimestamp;
            this.Description = description;
            this.DefaultValues = defaultValues;
        }
    };
}
