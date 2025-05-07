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
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    class ProcessorContext : ContentProcessorContext
    {
        private readonly PipelineManager _manager;
        private readonly ContentBuildLogger _logger;
        private readonly BuildEvent _buildEvent;

        public ProcessorContext(PipelineManager manager, ContentBuildLogger logger, BuildEvent buildEvent)
        {
            _manager = manager;
            _logger = logger;
            _buildEvent = buildEvent;
        }

        public override TargetPlatform TargetPlatform { get { return _manager.Platform; } }
        public override GraphicsProfile TargetProfile { get { return _manager.Profile; } }

        public override string BuildConfiguration { get { return _manager.Config; } }

        public override string IntermediateDirectory { get { return _manager.IntermediateDirectory; } }
        public override string OutputDirectory { get { return _manager.OutputDirectory; } }
        public override string OutputFilename { get { return _buildEvent.DestFile; } }

        public override OpaqueDataDictionary Parameters { get { return _buildEvent.ProcessorParams; } }

        public override ContentBuildLogger Logger { get { return _logger; } }

        public override void AddDependency(string filename)
        {
            if (!_buildEvent.Dependencies.Contains(filename))
                _buildEvent.Dependencies.Add(filename);
        }

        public override void AddOutputFile(string filename)
        {
            if (!_buildEvent.BuildOutput.Contains(filename))
                _buildEvent.BuildOutput.Add(filename);
        }

        public override TOutput Convert<TInput, TOutput>(   TInput input, 
                                                            string processorName,
                                                            OpaqueDataDictionary processorParameters)
        {
            var processorInfo = _manager._assembliesMgr.GetProcessorInfo(processorName);
            var processor = _manager._assembliesMgr.CreateProcessor(processorInfo, processorParameters);
            var processContext = new ProcessorContext(_manager, _logger, new BuildEvent { ProcessorParams = processorParameters });
            var processedObject = processor.Process(input, processContext);
           
            // Add its dependencies and built assets to ours.
            foreach (var i in processContext._buildEvent.Dependencies)
            {
                if (!_buildEvent.Dependencies.Contains(i))
                    _buildEvent.Dependencies.Add(i);
            }
            foreach (var i in processContext._buildEvent.BuildAsset)
            {
                if (!_buildEvent.BuildAsset.Contains(i))
                    _buildEvent.BuildAsset.Add(i);
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

            // Make sure the source file is absolute.
            string sourceFile = sourceAsset.Filename;
            if (!Path.IsPathRooted(sourceFile))
                sourceFile = Path.Combine(_manager.ProjectDirectory, sourceFile);
            sourceFile = LegacyPathHelper.Normalize(sourceFile);

            BuildEvent buildEvent = new BuildEvent
            { 
                SourceFile = sourceFile,
                Importer = importerName,
                Processor = processAsset ? processorName : null,
                ProcessorParams = _manager.ValidateProcessorParameters(processorName, processorParameters),
            };

            object importedObject = _manager.ImportContent(buildEvent, _logger);
            object processedObject = _manager.ProcessContent(buildEvent, _logger, importedObject);

            // Record that we processed this dependent asset.
            if (!_buildEvent.Dependencies.Contains(sourceFilepath))
                _buildEvent.Dependencies.Add(sourceFilepath);

            return (TOutput)processedObject;
        }

        public override ExternalReference<TOutput> BuildAsset<TInput, TOutput>(ExternalReference<TInput> sourceAsset,
                                                                                string processorName,
                                                                                OpaqueDataDictionary processorParameters,
                                                                                string importerName,
                                                                                string assetName)
        {
            if (string.IsNullOrEmpty(assetName))
                assetName = _manager.GetAssetName(sourceAsset.Filename, importerName, processorName, processorParameters, _logger);

            // Make sure the source file is absolute.
            string sourceFile = sourceAsset.Filename;
            if (!Path.IsPathRooted(sourceFile))
                sourceFile = Path.Combine(_manager.ProjectDirectory, sourceFile);
            sourceFile = LegacyPathHelper.Normalize(sourceFile);

            // Build the content.
            BuildEvent buildEvent = _manager.CreateBuildEvent(sourceFile, assetName, importerName, processorName, processorParameters);

            // Load the previous content event if it exists.
            BuildEvent cachedBuildEvent = _manager.LoadBuildEvent(buildEvent.DestFile);
            _manager.BuildContent(buildEvent, _logger, cachedBuildEvent, buildEvent.DestFile,
                null, null);

            // Record that we built this dependent asset.
            if (!_buildEvent.BuildAsset.Contains(buildEvent.DestFile))
                _buildEvent.BuildAsset.Add(buildEvent.DestFile);

            return new ExternalReference<TOutput>(buildEvent.DestFile);
        }
    }
}
