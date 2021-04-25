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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace tainicom.ProtonType.XnaContentPipeline.ProxyServer
{
    class LegacyPipelineProcessorContext : ContentProcessorContext
    {
        private readonly PipelineManager _manager;
        private readonly PipelineBuildEvent _pipelineEvent;
        private readonly ContentBuildLogger _logger;

        public LegacyPipelineProcessorContext(PipelineManager manager, ContentBuildLogger logger, PipelineBuildEvent pipelineEvent)
        {
            _manager = manager;
            _pipelineEvent = pipelineEvent;
            _logger = logger;
        }

        public override TargetPlatform TargetPlatform { get { return _manager.Platform; } }
        public override GraphicsProfile TargetProfile { get { return _manager.Profile; } }

        public override string BuildConfiguration { get { return _manager.Config; } }

        public override string IntermediateDirectory { get { return _manager.IntermediateDirectory; } }
        public override string OutputDirectory { get { return _manager.OutputDirectory; } }
        public override string OutputFilename { get { return _pipelineEvent.DestFile; } }

        public override OpaqueDataDictionary Parameters { get { return _pipelineEvent.Parameters; } }

        public override ContentBuildLogger Logger { get { return _logger; } }

        public override void AddDependency(string filename)
        {
            if (!_pipelineEvent.Dependencies.Contains(filename))
                _pipelineEvent.Dependencies.Add(filename);
        }

        public override void AddOutputFile(string filename)
        {
            if (!_pipelineEvent.BuildOutput.Contains(filename))
                _pipelineEvent.BuildOutput.Add(filename);
        }

        public override TOutput Convert<TInput, TOutput>(   TInput input, 
                                                            string processorName,
                                                            OpaqueDataDictionary processorParameters)
        {
            var processorInfo = _manager._assembliesMgr.GetProcessorInfo(processorName);
            var processor = _manager._assembliesMgr.CreateProcessor(processorInfo, processorParameters);
            var processContext = new LegacyPipelineProcessorContext(_manager, _logger, new PipelineBuildEvent { Parameters = processorParameters });
            var processedObject = processor.Process(input, processContext);
           
            // Add its dependencies and built assets to ours.
            foreach (var i in processContext._pipelineEvent.Dependencies)
            {
                if (!_pipelineEvent.Dependencies.Contains(i))
                    _pipelineEvent.Dependencies.Add(i);
            }
            foreach (var i in processContext._pipelineEvent.BuildAsset)
            {
                if (!_pipelineEvent.BuildAsset.Contains(i))
                    _pipelineEvent.BuildAsset.Add(i);
            }

            return (TOutput)processedObject;
        }

        public override TOutput BuildAndLoadAsset<TInput, TOutput>( ExternalReference<TInput> sourceAsset,
                                                                    string processorName,
                                                                    OpaqueDataDictionary processorParameters,
                                                                    string importerName)
        {
            var sourceFilepath = LegacyPathHelper.Normalize(sourceAsset.Filename);

            // The processorName can be null or empty. In this case the asset should
            // be imported but not processed. This is, for example, necessary to merge
            // animation files as described here:
            // http://blogs.msdn.com/b/shawnhar/archive/2010/06/18/merging-animation-files.aspx.
            bool processAsset = !string.IsNullOrEmpty(processorName);
            _manager._assembliesMgr.ResolveImporterAndProcessor(sourceFilepath, ref importerName, ref processorName);

            var buildEvent = new PipelineBuildEvent 
            { 
                SourceFile = sourceFilepath,
                Importer = importerName,
                Processor = processAsset ? processorName : null,
                Parameters = _manager.ValidateProcessorParameters(processorName, processorParameters),
            };

            var processedObject = _manager.ProcessContent(buildEvent, _logger);

            // Record that we processed this dependent asset.
            if (!_pipelineEvent.Dependencies.Contains(sourceFilepath))
                _pipelineEvent.Dependencies.Add(sourceFilepath);

            return (TOutput)processedObject;
        }

        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset,
                                                                                string processorName,
                                                                                OpaqueDataDictionary processorParameters,
                                                                                string importerName, 
                                                                                string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                assetName = _manager.GetAssetName(_logger, sourceAsset.Filename, importerName, processorName, processorParameters);

            // Build the content.
            var buildEvent = _manager.BuildContent(_logger, sourceAsset.Filename, assetName, importerName, processorName, processorParameters);

            // Record that we built this dependent asset.
            if (!_pipelineEvent.BuildAsset.Contains(buildEvent.DestFile))
                _pipelineEvent.BuildAsset.Add(buildEvent.DestFile);

            return new ExternalReference<TOutput>(buildEvent.DestFile);
        }
    }
}
