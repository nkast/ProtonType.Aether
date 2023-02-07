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
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common.Converters;
using nkast.ProtonType.XnaContentPipeline.ProxyServer.Assemblies;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    class PipelineManager
    {
        // Keep track of all built assets. (Required to resolve automatic names "AssetName_n".)
        //   Key = absolute, normalized path of source file
        //   Value = list of build events
        // (Note: When using external references, an asset may be built multiple times
        // with different parameters.)
        private readonly Dictionary<string, List<PipelineBuildEvent>> _pipelineBuildEvents;

        public string ProjectDirectory { get; private set; }
        public string OutputDirectory { get; private set; }
        public string IntermediateDirectory { get; private set; }

        internal AssembliesMgr _assembliesMgr;

        private ContentCompiler _compiler;

        public ContentBuildLogger Logger { get; set; }
        
        /// <summary>
        /// The current target graphics profile for which all content is built.
        /// </summary>
        public GraphicsProfile Profile { get; set; }

        /// <summary>
        /// The current target platform for which all content is built.
        /// </summary>
        public TargetPlatform Platform { get; set; }

        /// <summary>
        /// The build configuration passed thru to content processors.
        /// </summary>
        public string Config { get; set; }

        /// <summary>
        /// Gets or sets if the content is compressed.
        /// </summary>
        public bool CompressContent { get; set; }

        /// <summary>        
        /// If true exceptions thrown from within an importer or processor are caught and then 
        /// thrown from the context. Default value is true.
        /// </summary>
        public bool RethrowExceptions { get; set; }

        public PipelineManager(string projectDir, string outputDir, string intermediateDir, AssembliesMgr assembliesMgr)
        {
            _pipelineBuildEvents = new Dictionary<string, List<PipelineBuildEvent>>();
            RethrowExceptions = true;

            Logger = null;

            ProjectDirectory = LegacyPathHelper.NormalizeDirectory(projectDir);
            OutputDirectory = LegacyPathHelper.NormalizeDirectory(outputDir);
            IntermediateDirectory = LegacyPathHelper.NormalizeDirectory(intermediateDir);

	        RegisterCustomConverters();
            
            _assembliesMgr = assembliesMgr;
        }

	    public void AssignTypeConverter<TType, TTypeConverter> ()
	    {
		    TypeDescriptor.AddAttributes (typeof (TType), new TypeConverterAttribute (typeof (TTypeConverter)));
	    }

	    private void RegisterCustomConverters ()
	    {
            AssignTypeConverter<Microsoft.Xna.Framework.Color, StringToColorConverter>();
	    }

        public OpaqueDataDictionary ValidateProcessorParameters(string processorName, OpaqueDataDictionary processorParameters)
        {
            var result = new OpaqueDataDictionary();

            var processorInfo = _assembliesMgr.GetProcessorInfo(processorName);

            if (processorInfo == null || processorParameters == null)
            {
                return result;
            }

            var processorType = processorInfo.Type;
            foreach (var param in processorParameters)
            {
                var propInfo = processorType.GetProperty(param.Key, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null || propInfo.GetSetMethod(false) == null)
                    continue;

                // Make sure we can assign the value.
                if (!propInfo.PropertyType.IsInstanceOfType(param.Value))
                {
                    // Make sure we can convert the value.
                    var typeConverter = TypeDescriptor.GetConverter(propInfo.PropertyType);
                    if (!typeConverter.CanConvertFrom(param.Value.GetType()))
                        continue;
                }

                result.Add(param.Key, param.Value);
            }

            return result;
        }

        private void ResolveOutputFilepath(string sourceFilepath, ref string outputFilepath)
        {
            // If the output path is null... build it from the source file path.
            if (string.IsNullOrEmpty(outputFilepath))
            {
                var filename = Path.GetFileNameWithoutExtension(sourceFilepath) + ".xnb";
                var directory = LegacyPathHelper.GetRelativePath(ProjectDirectory,
                                                           Path.GetDirectoryName(sourceFilepath) +
                                                           Path.DirectorySeparatorChar);
                outputFilepath = Path.Combine(OutputDirectory, directory, filename);
            }
            else
            {
                // If the extension is not XNB or the source file extension then add XNB.
                var sourceExt = Path.GetExtension(sourceFilepath);
                if (outputFilepath.EndsWith(sourceExt, StringComparison.InvariantCultureIgnoreCase))
                    outputFilepath = outputFilepath.Substring(0, outputFilepath.Length - sourceExt.Length);
                if (!outputFilepath.EndsWith(".xnb", StringComparison.InvariantCultureIgnoreCase))
                    outputFilepath += ".xnb";

                // If the path isn't rooted then put it into the output directory.
                if (!Path.IsPathRooted(outputFilepath))
                    outputFilepath = Path.Combine(OutputDirectory, outputFilepath);
            }

            outputFilepath = LegacyPathHelper.Normalize(outputFilepath);
        }

        private PipelineBuildEvent LoadBuildEvent(string destFile, out string eventFilepath)
        {
            var contentPath = Path.ChangeExtension(LegacyPathHelper.GetRelativePath(OutputDirectory, destFile), PipelineBuildEvent.Extension);
            eventFilepath = Path.Combine(IntermediateDirectory, contentPath);
            return PipelineBuildEvent.Load(eventFilepath);
        }

        public void RegisterContent(string sourceFilepath, string outputFilepath = null, string importerName = null, string processorName = null, OpaqueDataDictionary processorParameters = null)
        {
            sourceFilepath = LegacyPathHelper.Normalize(sourceFilepath);
            ResolveOutputFilepath(sourceFilepath, ref outputFilepath);

            _assembliesMgr.ResolveImporterAndProcessor(sourceFilepath, ref importerName, ref processorName);

            var contentEvent = new PipelineBuildEvent
            {
                SourceFile = sourceFilepath,
                DestFile = outputFilepath,
                Importer = importerName,
                Processor = processorName,
                Parameters = ValidateProcessorParameters(processorName, processorParameters),
            };

            // Register pipeline build event. (Required to correctly resolve external dependencies.)
            TrackPipelineBuildEvent(contentEvent);
        }

        public PipelineBuildEvent BuildContent(ContentBuildLogger logger, string sourceFilepath, string outputFilepath = null, string importerName = null, string processorName = null, OpaqueDataDictionary processorParameters = null)
        {
            sourceFilepath = LegacyPathHelper.Normalize(sourceFilepath);
            ResolveOutputFilepath(sourceFilepath, ref outputFilepath);

            _assembliesMgr.ResolveImporterAndProcessor(sourceFilepath, ref importerName, ref processorName);

            // Record what we're building and how.
            var contentEvent = new PipelineBuildEvent
            {
                SourceFile = sourceFilepath,
                DestFile = outputFilepath,
                Importer = importerName,
                Processor = processorName,
                Parameters = ValidateProcessorParameters(processorName, processorParameters),
            };

            // Load the previous content event if it exists.
            string eventFilepath;
            var cachedEvent = LoadBuildEvent(contentEvent.DestFile, out eventFilepath);

            BuildContent(contentEvent, logger, cachedEvent, eventFilepath);

            return contentEvent;
        }

        private void BuildContent(PipelineBuildEvent pipelineEvent, ContentBuildLogger logger, PipelineBuildEvent cachedEvent, string eventFilepath)
        {
            if (!File.Exists(pipelineEvent.SourceFile))
            {
                logger.LogMessage("{0}", pipelineEvent.SourceFile);
                throw new PipelineException("The source file '{0}' does not exist.", pipelineEvent.SourceFile);
            }

            logger.PushFile(pipelineEvent.SourceFile);

            // Keep track of all build events. (Required to resolve automatic names "AssetName_n".)
            TrackPipelineBuildEvent(pipelineEvent);

            var rebuild = NeedsRebuild(_assembliesMgr, pipelineEvent, cachedEvent);
            if (rebuild)
                logger.LogMessage("{0}", pipelineEvent.SourceFile);
            else
                logger.LogMessage("Skipping {0}", pipelineEvent.SourceFile);

            try
            {
                if (!rebuild)
                {
                    // While this asset doesn't need to be rebuilt the dependent assets might.
                    foreach (var asset in cachedEvent.BuildAsset)
                    {
                        string assetEventFilepath;
                        var assetCachedEvent = LoadBuildEvent(asset, out assetEventFilepath);

                        // If we cannot find the cached event for the dependancy
                        // then we have to trigger a rebuild of the parent content.
                        if (assetCachedEvent == null)
                        {
                            rebuild = true;
                            break;
                        }

                        var depEvent = new PipelineBuildEvent
                        {
                            SourceFile = assetCachedEvent.SourceFile,
                            DestFile = assetCachedEvent.DestFile,
                            Importer = assetCachedEvent.Importer,
                            Processor = assetCachedEvent.Processor,
                            Parameters = assetCachedEvent.Parameters,
                        };

                        // Give the asset a chance to rebuild.                    
                        BuildContent(depEvent, logger, assetCachedEvent, assetEventFilepath);
                    }
                }

                // Do we need to rebuild?
                if (rebuild)
                {
                    var startTime = DateTime.UtcNow;

                    // Import and process the content.
                    var processedObject = ProcessContent(pipelineEvent, logger);

                    // Write the content to disk.
                    WriteXnb(processedObject, pipelineEvent);

                    // Store the timestamp of the DLLs containing the importer and processor.
                    ImporterInfo  importerInfo  = _assembliesMgr.GetImporterInfo(pipelineEvent.Importer);
                    ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(pipelineEvent.Processor);
                    pipelineEvent.ImporterTime  = (importerInfo != null) ? importerInfo.AssemblyTimestamp : DateTime.MaxValue;                    
                    pipelineEvent.ProcessorTime = (processorInfo != null) ? processorInfo.AssemblyTimestamp : DateTime.MaxValue;

                    // Store the new event into the intermediate folder.
                    pipelineEvent.Save(eventFilepath);

                    var buildTime = DateTime.UtcNow - startTime;
                }
            }
            finally
            {
                logger.PopFile();
            }
        }

        public object ProcessContent(PipelineBuildEvent pipelineEvent, ContentBuildLogger logger)
        {
            if (!File.Exists(pipelineEvent.SourceFile))
                throw new PipelineException("The source file '{0}' does not exist.", pipelineEvent.SourceFile);

            // Store the last write time of the source file
            // so we can detect if it has been changed.
            pipelineEvent.SourceTime = File.GetLastWriteTime(pipelineEvent.SourceFile);

            // Make sure we can find the importer and processor.
            var importerInfo = _assembliesMgr.GetImporterInfo(pipelineEvent.Importer);
            if (importerInfo == null)
                throw new PipelineException("Failed to create importer '{0}'", pipelineEvent.Importer);
            var importer = _assembliesMgr.CreateImporter(importerInfo);
            if (importer == null)
                throw new PipelineException("Failed to create importer '{0}'", pipelineEvent.Importer);

            // Try importing the content.
            object importedObject;
            if (RethrowExceptions)
            {
                try
                {
                    var importContext = new LegacyPipelineImporterContext(this, logger);
                    importedObject = importer.Import(pipelineEvent.SourceFile, importContext);
                }
                catch (PipelineException)
                {
                    throw;
                }
                catch (InvalidContentException)
                {
                    throw;
                }
                catch (Exception inner)
                {
                    throw new PipelineException(string.Format("Importer '{0}' had unexpected failure.", pipelineEvent.Importer), inner);
                }
            }
            else
            {
                var importContext = new LegacyPipelineImporterContext(this, logger);
                importedObject = importer.Import(pipelineEvent.SourceFile, importContext);
            }

            // The pipelineEvent.Processor can be null or empty. In this case the
            // asset should be imported but not processed.
            if (string.IsNullOrEmpty(pipelineEvent.Processor))
                return importedObject;

            var processorInfo = _assembliesMgr.GetProcessorInfo(pipelineEvent.Processor);
            if (processorInfo == null)
                throw new PipelineException("Failed to create processor '{0}'", pipelineEvent.Processor);
            var processor = _assembliesMgr.CreateProcessor(processorInfo, pipelineEvent.Parameters);
            if (processor == null)
                throw new PipelineException("Failed to create processor '{0}'", pipelineEvent.Processor);

            // Make sure the input type is valid.
            if (!processor.InputType.IsAssignableFrom(importedObject.GetType()))
            {
                throw new PipelineException(
                    string.Format("The type '{0}' cannot be processed by {1} as a {2}.",
                    importedObject.GetType().FullName,
                    pipelineEvent.Processor,
                    processor.InputType.FullName));
            }

            // Process the imported object.

            object processedObject;
            if (RethrowExceptions)
            {
                try
                {
                    var processContext = new LegacyPipelineProcessorContext(this, logger, pipelineEvent);
                    processedObject = processor.Process(importedObject, processContext);
                }
                catch (PipelineException)
                {
                    throw;
                }
                catch (InvalidContentException)
                {
                    throw;
                }
                catch (Exception inner)
                {
                    throw new PipelineException(string.Format("Processor '{0}' had unexpected failure.", pipelineEvent.Processor), inner);
                }
            }
            else
            {
                var processContext = new LegacyPipelineProcessorContext(this, logger, pipelineEvent);
                processedObject = processor.Process(importedObject, processContext);
            }

            return processedObject;
        }

        public void CleanContent(ContentBuildLogger logger, string sourceFilepath, string outputFilepath = null)
        {
            // First try to load the event file.
            ResolveOutputFilepath(sourceFilepath, ref outputFilepath);
            string eventFilepath = null;
            var cachedEvent = LoadBuildEvent(outputFilepath, out eventFilepath);

            if (cachedEvent != null)
            {
                // Recursively clean additional (nested) assets.
                foreach (var asset in cachedEvent.BuildAsset)
                {
                    string assetEventFilepath;
                    var assetCachedEvent = LoadBuildEvent(asset, out assetEventFilepath);

                    if (assetCachedEvent == null)
                    {
                        logger.LogMessage("Cleaning {0}", asset);

                        // Remove asset (.xnb file) from output folder.
                        if (File.Exists(asset))
                            File.Delete(asset);

                        // Remove event file (.mgcontent file) from intermediate folder.
                        if (File.Exists(assetEventFilepath))
                            File.Delete(assetEventFilepath);
                        continue;
                    }

                    CleanContent(logger, string.Empty, asset);
                }

                // Remove related output files (non-XNB files) that were copied to the output folder.
                foreach (var asset in cachedEvent.BuildOutput)
                {
                    logger.LogMessage("Cleaning {0}", asset);
                    if (File.Exists(asset))
                        File.Delete(asset);
                }
            }

            logger.LogMessage("Cleaning {0}", outputFilepath);

            // Remove asset (.xnb file) from output folder.
            if (File.Exists(outputFilepath))
                File.Delete(outputFilepath);

            // Remove event file (.mgcontent file) from intermediate folder.
            if (File.Exists(eventFilepath))
                File.Delete(eventFilepath);

            lock (_pipelineBuildEvents)
            {
                _pipelineBuildEvents.Remove(sourceFilepath);
            }
        }

        private void WriteXnb(object content, PipelineBuildEvent pipelineEvent)
        {
            // Make sure the output directory exists.
            var outputFileDir = Path.GetDirectoryName(pipelineEvent.DestFile);

            Directory.CreateDirectory(outputFileDir);

            if (_compiler == null)
                _compiler = new ContentCompiler();

            // Write the XNB.
            using (var stream = new FileStream(pipelineEvent.DestFile, FileMode.Create, FileAccess.Write, FileShare.None))
                _compiler.Compile(stream, content, Platform, Profile, CompressContent, OutputDirectory, outputFileDir);

            // Store the last write time of the output XNB here
            // so we can verify it hasn't been tampered with.
            pipelineEvent.DestTime = File.GetLastWriteTime(pipelineEvent.DestFile);
        }

        /// <summary>
        /// Stores the pipeline build event (in memory) if no matching event is found.
        /// </summary>
        /// <param name="pipelineEvent">The pipeline build event.</param>
        private void TrackPipelineBuildEvent(PipelineBuildEvent pipelineEvent)
        {
            List<PipelineBuildEvent> pipelineBuildEvents;
            lock (_pipelineBuildEvents)
            {
                bool eventsFound = _pipelineBuildEvents.TryGetValue(pipelineEvent.SourceFile, out pipelineBuildEvents);
                if (!eventsFound)
                {
                    pipelineBuildEvents = new List<PipelineBuildEvent>();
                    _pipelineBuildEvents.Add(pipelineEvent.SourceFile, pipelineBuildEvents);
                }

                if (FindMatchingEvent(pipelineBuildEvents, pipelineEvent.DestFile, pipelineEvent.Importer, pipelineEvent.Processor, pipelineEvent.Parameters) == null)
                    pipelineBuildEvents.Add(pipelineEvent);
            }
        }

        /// <summary>
        /// Gets an automatic asset name, such as "AssetName_0".
        /// </summary>
        /// <param name="sourceFileName">The source file name.</param>
        /// <param name="importerName">The name of the content importer. Can be <see langword="null"/>.</param>
        /// <param name="processorName">The name of the content processor. Can be <see langword="null"/>.</param>
        /// <param name="processorParameters">The processor parameters. Can be <see langword="null"/>.</param>
        /// <returns>The asset name.</returns>
        public string GetAssetName(ContentBuildLogger logger, string sourceFileName, string importerName, string processorName, OpaqueDataDictionary processorParameters)
        {
            Debug.Assert(Path.IsPathRooted(sourceFileName), "Absolute path expected.");

            // Get source file name, which is used for lookup in _pipelineBuildEvents.
            sourceFileName = LegacyPathHelper.Normalize(sourceFileName);
            string relativeSourceFileName = LegacyPathHelper.GetRelativePath(ProjectDirectory, sourceFileName);

            List<PipelineBuildEvent> pipelineBuildEvents;
            lock (_pipelineBuildEvents)
            {
                if (_pipelineBuildEvents.TryGetValue(sourceFileName, out pipelineBuildEvents))
                {
                    // This source file has already been build.
                    // --> Compare pipeline build events.
                    _assembliesMgr.ResolveImporterAndProcessor(sourceFileName, ref importerName, ref processorName);

                    var matchingEvent = FindMatchingEvent(pipelineBuildEvents, null, importerName, processorName, processorParameters);
                    if (matchingEvent != null)
                    {
                        // Matching pipeline build event found.
                        string existingName = matchingEvent.DestFile;
                        existingName = LegacyPathHelper.GetRelativePath(OutputDirectory, existingName);
                        existingName = existingName.Substring(0, existingName.Length - 4);   // Remove ".xnb".
                        return existingName;
                    }

                    logger.LogMessage(string.Format("Warning: Asset {0} built multiple times with different settings.", relativeSourceFileName));
                }
            }

            // No pipeline build event with matching settings found.
            // Get default asset name by searching the existing .mgcontent files.
            string directoryName = Path.GetDirectoryName(relativeSourceFileName);
            string fileName = Path.GetFileNameWithoutExtension(relativeSourceFileName);
            string assetName = Path.Combine(directoryName, fileName);
            assetName = LegacyPathHelper.Normalize(assetName);

            for (int index = 0; ; index++)
            {
                string destFile = assetName + '_' + index;
                string eventFile;
                var existingBuildEvent = LoadBuildEvent(destFile, out eventFile);
                if (existingBuildEvent == null)
                    return destFile;

                var existingBuildEventDestFile = existingBuildEvent.DestFile;
                existingBuildEventDestFile = LegacyPathHelper.GetRelativePath(ProjectDirectory, existingBuildEventDestFile);
                existingBuildEventDestFile = Path.Combine(Path.GetDirectoryName(existingBuildEventDestFile), Path.GetFileNameWithoutExtension(existingBuildEventDestFile));
                existingBuildEventDestFile = LegacyPathHelper.Normalize(existingBuildEventDestFile);
                
                var fullDestFile = Path.Combine(OutputDirectory, destFile);
                var relativeDestFile = LegacyPathHelper.GetRelativePath(ProjectDirectory, fullDestFile);
                relativeDestFile = LegacyPathHelper.Normalize(relativeDestFile);

                if (existingBuildEventDestFile.Equals(relativeDestFile) &&
                    existingBuildEvent.Importer  == importerName &&
                    existingBuildEvent.Processor == processorName)
                {
                    OpaqueDataDictionary defaultValues = null;
                    var processorInfo = _assembliesMgr.GetProcessorInfo(processorName);
                    if (processorInfo != null)
                        defaultValues = processorInfo.DefaultValues;
                    if (AreParametersEqual(existingBuildEvent.Parameters, processorParameters, defaultValues))
                        return destFile;
                }
            }
        }

        /// <summary>
        /// Determines whether the specified list contains a matching pipeline build event.
        /// </summary>
        /// <param name="pipelineBuildEvents">The list of pipeline build events.</param>
        /// <param name="destFile">Absolute path to the output file. Can be <see langword="null"/>.</param>
        /// <param name="importerName">The name of the content importer. Can be <see langword="null"/>.</param>
        /// <param name="processorName">The name of the content processor. Can be <see langword="null"/>.</param>
        /// <param name="processorParameters">The processor parameters. Can be <see langword="null"/>.</param>
        /// <returns>
        /// The matching pipeline build event, or <see langword="null"/>.
        /// </returns>
        private PipelineBuildEvent FindMatchingEvent(List<PipelineBuildEvent> pipelineBuildEvents, string destFile, string importerName, string processorName, OpaqueDataDictionary processorParameters)
        {
            foreach (var existingBuildEvent in pipelineBuildEvents)
            {
                if ((destFile == null || existingBuildEvent.DestFile.Equals(destFile))
                    && existingBuildEvent.Importer == importerName
                    && existingBuildEvent.Processor == processorName)
                {
                    OpaqueDataDictionary defaultValues = null;
                    var processorInfo = _assembliesMgr.GetProcessorInfo(processorName);
                    if (processorInfo != null)
                        defaultValues = processorInfo.DefaultValues;
                    if (AreParametersEqual(existingBuildEvent.Parameters, processorParameters, defaultValues))
                    {
                        return existingBuildEvent;
                    }
                }
            }

            return null;
        }

        internal static bool NeedsRebuild(AssembliesMgr assembliesMgr, PipelineBuildEvent buildEvent, PipelineBuildEvent cachedbuildEvent)
        {
            // If we have no previously cached build event then we cannot
            // be sure that the state hasn't changed... force a rebuild.
            if (cachedbuildEvent == null)
                return true;

            // Verify that the last write time of the source file matches
            // what we recorded when it was built.  If it is different
            // that means someone modified it and we need to rebuild.
            var sourceWriteTime = File.GetLastWriteTime(buildEvent.SourceFile);
            if (cachedbuildEvent.SourceTime != sourceWriteTime)
                return true;

            // Do the same test for the dest file.
            var destWriteTime = File.GetLastWriteTime(buildEvent.DestFile);
            if (cachedbuildEvent.DestTime != destWriteTime)
                return true;

            // If the source file is newer than the dest file
            // then it must have been updated and needs a rebuild.
            if (sourceWriteTime >= destWriteTime)
                return true;

            // Are any of the dependancy files newer than the dest file?
            foreach (var depFile in cachedbuildEvent.Dependencies)
            {
                if (File.GetLastWriteTime(depFile) >= destWriteTime)
                    return true;
            }

            // This shouldn't happen...  but if the source or dest files changed
            // then force a rebuild.
            if (cachedbuildEvent.SourceFile != buildEvent.SourceFile ||
                cachedbuildEvent.DestFile != buildEvent.DestFile)
                return true;

            // Did the importer assembly change?
            var cachedImporterInfo = assembliesMgr.GetImporterInfo(cachedbuildEvent.Importer);
            DateTime importerAssemblyTimestamp = (cachedImporterInfo != null) ? cachedImporterInfo.AssemblyTimestamp : DateTime.MaxValue;
            if (importerAssemblyTimestamp > cachedbuildEvent.ImporterTime)
                return true;

            // Did the importer change?
            if (cachedbuildEvent.Importer != buildEvent.Importer)
                return true;

            // Did the processor assembly change?
            var cachedProcessorInfo = assembliesMgr.GetProcessorInfo(cachedbuildEvent.Processor);
            DateTime processorInfoAssemblyTimestamp = (cachedProcessorInfo != null) ? cachedProcessorInfo.AssemblyTimestamp : DateTime.MaxValue;
            if (processorInfoAssemblyTimestamp > cachedbuildEvent.ProcessorTime)
                return true;

            // Did the processor change?
            if (cachedbuildEvent.Processor != buildEvent.Processor)
                return true;

            // Did the parameters change?
            OpaqueDataDictionary defaultValues = null;
            var buildProcessorInfo = assembliesMgr.GetProcessorInfo(buildEvent.Processor);
            if (buildProcessorInfo != null)
                defaultValues = buildProcessorInfo.DefaultValues;
            if (!AreParametersEqual(cachedbuildEvent.Parameters, buildEvent.Parameters, defaultValues))
                return true;

            return false;
        }

        

        private static readonly OpaqueDataDictionary EmptyParameters = new OpaqueDataDictionary();

        internal static bool AreParametersEqual(OpaqueDataDictionary parameters0, OpaqueDataDictionary parameters1, OpaqueDataDictionary defaultValues)
        {
            Debug.Assert(defaultValues != null, "defaultValues must not be empty.");
            Debug.Assert(EmptyParameters != null && EmptyParameters.Count == 0);

            // Same reference or both null?
            if (parameters0 == parameters1)
                return true;

            if (parameters0 == null)
                parameters0 = EmptyParameters;
            if (parameters1 == null)
                parameters1 = EmptyParameters;

            // Are both dictionaries empty?
            if (parameters0.Count == 0 && parameters1.Count == 0)
                return true;

            // Compare the values with the second dictionary or
            // the default values.
            if (parameters0.Count < parameters1.Count)
            {
                var dummy = parameters0;
                parameters0 = parameters1;
                parameters1 = dummy;
            }

            // Compare parameters0 with parameters1 or defaultValues.
            foreach (var pair in parameters0)
            {
                object value0 = pair.Value;
                object value1;

                // Search for matching parameter.
                if (!parameters1.TryGetValue(pair.Key, out value1) && !defaultValues.TryGetValue(pair.Key, out value1))
                    return false;

                if (!AreEqual(value0, value1))
                    return false;
            }

            // Compare parameters which are only in parameters1 with defaultValues.
            foreach (var pair in parameters1)
            {
                if (parameters0.ContainsKey(pair.Key))
                    continue;

                object defaultValue;
                if (!defaultValues.TryGetValue(pair.Key, out defaultValue))
                    return false;

                if (!AreEqual(pair.Value, defaultValue))
                    return false;
            }

            return true;
        }

        private static bool AreEqual(object value0, object value1)
        {
            // Are values equal or both null?
            if (Equals(value0, value1))
                return true;

            // Is one value null?
            if (value0 == null || value1 == null)
                return false;

            // Values are of different type: Compare string representation.
            if (ConvertToString(value0) != ConvertToString(value1))
                return false;

            return true;
        }

        internal static string ConvertToString(object value)
        {
            if (value == null)
                return null;

            //Convert.ToString(value, CultureInfo.InvariantCulture);
            var typeConverter = TypeDescriptor.GetConverter(value.GetType());
            return typeConverter.ConvertToInvariantString(value);
        }

    }
}
