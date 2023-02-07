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


using Microsoft.Xna.Framework.Content.Pipeline;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    class LegacyPipelineImporterContext : ContentImporterContext
    {
        private readonly PipelineManager _manager;
        private readonly ContentBuildLogger _logger;

        public LegacyPipelineImporterContext(PipelineManager manager, ContentBuildLogger logger)
        {
            _manager = manager;
            _logger = logger;
        }

        public override string IntermediateDirectory { get { return _manager.IntermediateDirectory; } }
        public override string OutputDirectory { get { return _manager.OutputDirectory; } }
        public override ContentBuildLogger Logger { get { return _logger; } }

        public override void AddDependency(string filename)
        {            
        }
    }
}