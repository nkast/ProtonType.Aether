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
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    public class ParametersContext
    {
        public readonly Guid Guid;

        public string OutputDir;
        public string IntermediateDir;
        public TargetPlatform Platform;
        public GraphicsProfile Profile;
        public bool Compress;
        public string Config;

        public string Importer;
        public string Processor;
        public Dictionary<string, string> ProcessorParams = new Dictionary<string, string>();
        public string OriginalPath;
        public string DestinationPath;

        public ParametersContext()
        {
            Guid = Guid.Empty;
        }

        public ParametersContext(Guid guid)
        {
            this.Guid = guid;
        }

        internal ParametersContext CreateContext(Guid guid)
        {
            ParametersContext context = new ParametersContext(guid);
            
            context.OutputDir = this.OutputDir;
            context.IntermediateDir = this.IntermediateDir;
            context.Platform = this.Platform;
            context.Profile = this.Profile;
            context.Compress = this.Compress;
            context.Config = this.Config;

            context.Importer = this.Importer;
            context.Processor = this.Processor;
            foreach(var processorParam in this.ProcessorParams)
                context.ProcessorParams.Add(processorParam.Key, processorParam.Value);
            context.OriginalPath = this.OriginalPath;
            context.DestinationPath = this.DestinationPath;

            return context;
        }
    }
}
