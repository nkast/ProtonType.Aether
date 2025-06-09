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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ProxyServer.Assemblies;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    partial class PipelineProxyServer : IPipelineBuilder
    {
        private string BaseDirectory;
        private string ProjectName;

        private readonly ParametersContext _globalContext = new ParametersContext();
        private readonly ContentBuildLogger _globalLogger;
        private readonly AssembliesMgr _assembliesMgr = new AssembliesMgr();
        private readonly Dictionary<Guid, ParametersContext> _items = new Dictionary<Guid, ParametersContext>();

        // Keep track of all built assets. (Required to resolve automatic names "AssetName_n".)
        //   Key = absolute, normalized path of source file
        //   Value = list of build events
        // (Note: When using external references, an asset may be built multiple times
        // with different parameters.)
        private readonly Dictionary<string, List<BuildEvent>> _buildEventsMap = new Dictionary<string, List<BuildEvent>>();


        bool _targetChanged = false;
        SourceFileCollection _previousFileCollection;
        SourceFileCollection _newFileCollection;


        void IPipelineBuilder.SetBaseDirectory(string baseDirectory)
        {
            this.BaseDirectory = baseDirectory;
        }

        void IPipelineBuilder.SetProjectName(string projectName)
        {
            this.ProjectName = projectName;
        }

        Task IPipelineBuilder.AddPackageAsync(IProxyLoggerBase logger, Package package)
        {
            if (package.Name == null)
                throw new ArgumentException("packageName cannot be null!");

            _assembliesMgr.AddPackage((ContentBuildLogger)logger, package);

            return Task.CompletedTask;
        }

        Task IPipelineBuilder.ResolvePackagesAsync(IProxyLoggerBase logger)
        {
            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string projFolder = this.ProjectName;

            _assembliesMgr.ResolvePackages((ContentBuildLogger)logger, projFolder, projectDirectory);

            return Task.CompletedTask;
        }

        Task IPipelineBuilder.AddAssemblyAsync(IProxyLoggerBase logger, string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentException("assemblyPath cannot be null!");

            assemblyPath = ReplaceSymbols(assemblyPath);
            string baseDirectory = (this.BaseDirectory == null)
                                 ? String.Empty
                                 : LegacyPathHelper.Normalize(this.BaseDirectory);

            _assembliesMgr.AddAssembly((ContentBuildLogger)logger, baseDirectory, assemblyPath);

            return Task.CompletedTask;
        }

        Task<List<ImporterDescription>> IPipelineBuilder.GetImportersAsync(IProxyLoggerBase logger)
        {
            List<ImporterDescription> importers = _assembliesMgr.GetImporters();
            return Task.FromResult(importers);
        }

        Task<List<ProcessorDescription>> IPipelineBuilder.GetProcessorsAsync(IProxyLoggerBase logger)
        {
            List<ProcessorDescription> processors = _assembliesMgr.GetProcessors();
            return Task.FromResult(processors);
        }


        void IPipelineBuilder.SetRebuild()
        {
        }

        void IPipelineBuilder.SetIncremental()
        {
        }

        void IPipelineBuilder.SetOutputDir(string outputDir)
        {
            this._globalContext.OutputDir = outputDir;
        }

        void IPipelineBuilder.SetIntermediateDir(string intermediateDir)
        {
            this._globalContext.IntermediateDir = intermediateDir;
        }

        void IPipelineBuilder.SetPlatform(TargetPlatform platform)
        {
            this._globalContext.Platform = platform;
        }

        void IPipelineBuilder.SetConfig(string config)
        {
            this._globalContext.Config = config;
        }

        void IPipelineBuilder.SetProfile(GraphicsProfile profile)
        {
            this._globalContext.Profile = profile;
        }

        void IPipelineBuilder.SetCompression(ContentCompression compression)
        {
            this._globalContext.Compression = compression;
        }

        void IPipelineBuilder.SetImporter(string importer)
        {
            this._globalContext.Importer = importer;
        }

        void IPipelineBuilder.SetProcessor(string processor)
        {
            this._globalContext.Processor = processor;
            this._globalContext.ProcessorParams.Clear();
        }

        void IPipelineBuilder.AddProcessorParam(string processorParam, string processorParamValue)
        {
            this._globalContext.ProcessorParams.Add(processorParam, processorParamValue);
        }

        Task IPipelineBuilder.CleanItemsAsync(IProxyLoggerBase logger)
        {
            string Config = _globalContext.Config;
            TargetPlatform Platform = _globalContext.Platform;
            GraphicsProfile Profile = _globalContext.Profile;
            ContentCompression Compression = _globalContext.Compression;

            string _outputDir = _globalContext.OutputDir;
            string _intermediateDir = _globalContext.IntermediateDir;

            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            string intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

            SourceFileCollection previousFileCollection = LoadFileCollection(intermediatePath);
            if (previousFileCollection != null)
            {
	            bool targetChanged = previousFileCollection.Config != Config
	                              || previousFileCollection.Platform != Platform
	                              || previousFileCollection.Profile != Profile
	                              || previousFileCollection.Compression != Compression
	                               ;

                PipelineManager _manager;
                _manager = new PipelineManager(projectDirectory, this.ProjectName, outputPath, intermediatePath, _assembliesMgr, _buildEventsMap);
                _manager.Compression = Compression;
                _manager.Logger = (ContentBuildLogger)logger;

                CleanItems(previousFileCollection, targetChanged, 
                    _manager);
            }

            return Task.CompletedTask;
        }

        private void CleanItems(SourceFileCollection previousFileCollection, bool targetChanged
            , PipelineManager _manager)
        {
            bool cleanOrRebuild = true; // Clean || Rebuild;
            bool incremental = false;

            for (int i = 0; i < previousFileCollection.SourceFilesCount; i++)
            {
                string prevSourceFile = previousFileCollection.SourceFiles[i];

                bool inContent = System.Linq.Enumerable.Any(_items.Values, (pc) => pc.OriginalPath == prevSourceFile);
                bool cleanOldContent = !inContent && !incremental;
                bool cleanRebuiltContent = inContent && cleanOrRebuild;
                if (targetChanged || cleanRebuiltContent || cleanOldContent)
                {
                    string prevDestFile = previousFileCollection.DestFiles[i];
                    _manager.ResolveOutputFilepath(prevSourceFile, ref prevDestFile);
                    _manager.CleanContent(_manager.Logger, prevDestFile);
                    lock (_buildEventsMap)
                    {
                        _buildEventsMap.Remove(prevSourceFile);
                    }
                }
            }
        }

        Task IPipelineBuilder.BuildBeginAsync(IProxyLoggerBase logger)
        {
            // TODO: we shouldn't be building a new PipelineManager.
            PipelineManager _manager;
            {
                string Config = _globalContext.Config;
                TargetPlatform Platform = _globalContext.Platform;
                GraphicsProfile Profile = _globalContext.Profile;
                ContentCompression Compression = _globalContext.Compression;

                string _outputDir = _globalContext.OutputDir;
                string _intermediateDir = _globalContext.IntermediateDir;

                string projectDirectory = this.BaseDirectory;
                projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

                string outputPath = ReplaceSymbols(_outputDir);
                if (!Path.IsPathRooted(outputPath))
                    outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

                string intermediatePath = ReplaceSymbols(_intermediateDir);
                if (!Path.IsPathRooted(intermediatePath))
                    intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

                // TODO: we shouldn't be building a new PipelineManager.
                _manager = new PipelineManager(projectDirectory, this.ProjectName, outputPath, intermediatePath, _assembliesMgr, _buildEventsMap);
                _manager.Compression = Compression;
            }

            foreach (var itemPair in _items)
            {
                Guid itemGuid = itemPair.Key;
                ParametersContext itemContext = itemPair.Value;

                string sourceFile = itemContext.OriginalPath;
                string link = itemContext.DestinationPath;

                // Make sure the source file is absolute.
                if (!Path.IsPathRooted(sourceFile))
                    sourceFile = Path.Combine(this.BaseDirectory, sourceFile);

                sourceFile = LegacyPathHelper.Normalize(sourceFile);

                // Copy the current processor parameters blind as we
                // will validate and remove invalid parameters during
                // the build process later.
                OpaqueDataDictionary processorParams = new OpaqueDataDictionary();
                foreach (var pair in itemContext.ProcessorParams)
                    processorParams.Add(pair.Key, pair.Value);

                ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(itemContext.Processor);
                OpaqueDataDictionary processorDefaultValues = (processorInfo != null) ? processorInfo.DefaultValues : null;

                try
                {
                    BuildEvent buildEvent = _manager.CreateBuildEvent(
                                          sourceFile,
                                          link,
                                          itemContext.Importer,
                                          itemContext.Processor,
                                          processorParams
                                          );
                    PipelineManager.TrackBuildEvent(this._buildEventsMap, buildEvent, processorDefaultValues);
                }
                catch { /* Ignore exception */ }
            }

            // Load the previously serialized list of built content.
             _previousFileCollection = LoadFileCollection(_manager.IntermediateDirectory);
            if (_previousFileCollection != null)
            {
                // If the target changed in any way then we need to force
                // a full rebuild even under incremental builds.
                _targetChanged = _previousFileCollection.Config != _globalContext.Config
                              || _previousFileCollection.Platform != _globalContext.Platform
                              || _previousFileCollection.Profile != _globalContext.Profile
                              || _previousFileCollection.Compression != _globalContext.Compression
                               ;
            }

            // Create new FileCollection
            _newFileCollection = new SourceFileCollection
            {
                Profile = _manager.Profile = _globalContext.Profile,
                Platform = _manager.Platform = _globalContext.Platform,
                Compression = _manager.Compression = _globalContext.Compression,
                Config = _manager.Config = _globalContext.Config
            };

            return Task.CompletedTask;
        }

        Task IPipelineBuilder.BuildEndAsync(IProxyLoggerBase logger)
        {
            bool Incremental = true;
            bool Rebuild = true;

            string _outputDir = _globalContext.OutputDir;
            string _intermediateDir = _globalContext.IntermediateDir;

            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            string intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

            // If this is an incremental build we merge the list
            // of previous content with the new list.
            if (_previousFileCollection != null && Incremental && !_targetChanged)
                _newFileCollection.Merge(_previousFileCollection);

            // Delete the old file and write the new content 
            // list if we have any to serialize.
            DeleteFileCollection(intermediatePath);

            if (_newFileCollection.SourceFilesCount > 0)
                SaveFileCollection(intermediatePath, _newFileCollection);

            _targetChanged = false;
            _previousFileCollection = null;
            _newFileCollection = null;

            return Task.CompletedTask;
        }


        private void DeleteFileCollection(string intermediatePath)
        {
            string dbname = this.ProjectName;
            if (dbname == String.Empty)
                dbname = PipelineManager.DefaultFileCollectionFilename;

            string intermediateFileCollectionPath = Path.Combine(intermediatePath, Path.ChangeExtension(dbname, SourceFileCollection.Extension));
            if (File.Exists(intermediateFileCollectionPath))
                File.Delete(intermediateFileCollectionPath);
        }

        private void SaveFileCollection(string intermediatePath, SourceFileCollection fileCollection)
        {
            string dbname = this.ProjectName;
            if (dbname == String.Empty)
                dbname = PipelineManager.DefaultFileCollectionFilename;

            string intermediateFileCollectionPath = Path.Combine(intermediatePath, Path.ChangeExtension(dbname, SourceFileCollection.Extension));
            fileCollection.SaveBinary(intermediateFileCollectionPath);
        }

        private SourceFileCollection LoadFileCollection(string intermediatePath)
        {
            string dbname = this.ProjectName;
            if (dbname == String.Empty)
                dbname = PipelineManager.DefaultFileCollectionFilename;

            string intermediateFileCollectionPath = Path.Combine(intermediatePath, Path.ChangeExtension(dbname, SourceFileCollection.Extension));
            return SourceFileCollection.LoadBinary(intermediateFileCollectionPath);
        }


        string ReplaceSymbols(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return parameter;
            return parameter
                .Replace("$(Platform)", _globalContext.Platform.ToString())
                .Replace("$(Configuration)", _globalContext.Config)
                .Replace("$(Config)", _globalContext.Config)
                .Replace("$(Profile)", _globalContext.Profile.ToString());
        }

        public class ContentItem
        {
            public string SourceFile;

            // This refers to the "Link" which can override the default output location
            public string OutputFile;

            public string Importer;
            public string Processor;
            public OpaqueDataDictionary ProcessorParams;
        }

        public class CopyItem
        {
            public string SourceFile;

            public string Link;
        }


        private void Build(BuildLogger logger, ParametersContext buildContext)
        {
            string sourceFile = buildContext.OriginalPath;
            if (!Path.IsPathRooted(sourceFile))
                sourceFile = Path.Combine(this.BaseDirectory, sourceFile);
            sourceFile = LegacyPathHelper.Normalize(sourceFile);

            // link should remain relative, absolute path will get set later when the build occurs
            string link = buildContext.DestinationPath;

            ContentItem c = new ContentItem
            {
                SourceFile = sourceFile,
                OutputFile = link,
                Importer = buildContext.Importer,
                Processor = buildContext.Processor,
                ProcessorParams = new OpaqueDataDictionary()
            };

            // Copy the current processor parameters blind as we
            // will validate and remove invalid parameters during
            // the build process later.
            foreach (var pair in buildContext.ProcessorParams)
            {
                c.ProcessorParams.Add(pair.Key, pair.Value);
            }


            bool Incremental = true;
            bool Rebuild = true;

            string Config = _globalContext.Config;
            TargetPlatform Platform = _globalContext.Platform;
            GraphicsProfile Profile = buildContext.Profile;
            ContentCompression Compression = buildContext.Compression;

            string _outputDir = _globalContext.OutputDir;
            string _intermediateDir = _globalContext.IntermediateDir;

            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            string intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

            // TODO: we shouldn't be building a new PipelineManager.
            PipelineManager _manager;
            _manager = new PipelineManager(projectDirectory, this.ProjectName, outputPath, intermediatePath, _assembliesMgr, _buildEventsMap);
            _manager.Compression = Compression;
            _manager.Logger = logger;

            BuildEvent buildEvent = _manager.CreateBuildEvent(c.SourceFile,
                                    c.OutputFile,
                                    c.Importer,
                                    c.Processor,
                                    c.ProcessorParams
                                    );

            BuildEvent cachedBuildEvent = _manager.LoadBuildEvent(buildEvent.DestFile);
            _manager.BuildContent(buildEvent, logger, cachedBuildEvent, buildEvent.DestFile,
                this, buildContext);

            _newFileCollection.AddFile(c.SourceFile, c.OutputFile);

            BuildState(buildContext, Common.BuildState.Built);
            return;
        }

        private void Copy(BuildLogger logger, ParametersContext buildContext)
        {
            string sourceFile = buildContext.OriginalPath;
            if (!Path.IsPathRooted(sourceFile))
                sourceFile = Path.Combine(BaseDirectory, sourceFile);
            sourceFile = LegacyPathHelper.Normalize(sourceFile);

            string link = buildContext.DestinationPath;

            CopyItem c = new CopyItem { SourceFile = sourceFile, Link = link };


            bool Incremental = true;
            bool Rebuild = true;

            string Config = buildContext.Config;
            TargetPlatform Platform = buildContext.Platform;
            GraphicsProfile Profile = buildContext.Profile;
            ContentCompression Compression = buildContext.Compression;

            string _outputDir = buildContext.OutputDir;
            string _intermediateDir = buildContext.IntermediateDir;

            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            string intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

            // Process copy items (files that bypass the content pipeline)

            // Figure out an asset name relative to the project directory,
            // retaining the file extension.
            // Note that replacing a sub-path like this requires consistent
            // directory separator characters.
            string relativeName = c.Link;
            if (string.IsNullOrWhiteSpace(relativeName))
                relativeName = c.SourceFile.Replace(projectDirectory, string.Empty)
                                    .TrimStart(Path.DirectorySeparatorChar)
                                    .TrimStart(Path.AltDirectorySeparatorChar);
            string dest = Path.Combine(outputPath, relativeName);

            // Only copy if the source file is newer than the destination.
            // We may want to provide an option for overriding this, but for
            // nearly all cases this is the desired behavior.
            if (File.Exists(dest) && !Rebuild)
            {
                DateTime srcTime = File.GetLastWriteTimeUtc(c.SourceFile);
                DateTime dstTime = File.GetLastWriteTimeUtc(dest);
                if (srcTime <= dstTime)
                {
                    if (string.IsNullOrEmpty(c.Link))
                        logger.LogMessage(String.Format("Skipping {0}", c.SourceFile));
                    else
                        logger.LogMessage(String.Format("Skipping {0} => {1}", c.SourceFile, c.Link));

                    BuildState(buildContext, Common.BuildState.Skipping);
                    return;
                }
            }

            DateTime startTime = DateTime.UtcNow;

            // Create the destination directory if it doesn't already exist.
            string destPath = Path.GetDirectoryName(dest);
            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            BuildState(buildContext, Common.BuildState.Copying);

            File.Copy(c.SourceFile, dest, true);

            // Destination file should not be read-only even if original was.
            FileAttributes fileAttribs = File.GetAttributes(dest);
            fileAttribs = fileAttribs & (~FileAttributes.ReadOnly);
            File.SetAttributes(dest, fileAttribs);

            TimeSpan buildTime = DateTime.UtcNow - startTime;

            if (string.IsNullOrEmpty(c.Link))
                logger.LogMessage(String.Format("{0}", c.SourceFile));
            else
                logger.LogMessage(String.Format("{0} => {1}", c.SourceFile, c.Link));

            BuildState(buildContext, Common.BuildState.Built);
            return;
        }

    }
}
