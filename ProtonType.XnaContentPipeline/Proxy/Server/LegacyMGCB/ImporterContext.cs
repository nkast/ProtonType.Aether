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
    class ImporterContext : ContentImporterContext
    {
        private readonly PipelineManager _manager;
        private readonly ContentBuildLogger _logger;
        private readonly BuildEvent _buildEvent;

        public ImporterContext(PipelineManager manager, ContentBuildLogger logger, BuildEvent buildEvent)
        {
            _manager = manager;
            _logger = logger;
            _buildEvent = buildEvent;
        }

        public override string IntermediateDirectory { get { return _manager.IntermediateDirectory; } }
        public override string OutputDirectory { get { return _manager.OutputDirectory; } }
        public override ContentBuildLogger Logger { get { return _logger; } }

        public override void AddDependency(string filename)
        {
            if (!_buildEvent.Dependencies.Contains(filename))
                _buildEvent.Dependencies.Add(filename);
        }
    }
}