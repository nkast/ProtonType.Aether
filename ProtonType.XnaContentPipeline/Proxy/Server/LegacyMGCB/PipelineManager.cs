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
        private readonly Dictionary<string, List<BuildEvent>> _buildEventsMap;

        public string ProjectDirectory { get; private set; }
        public string ProjectFilename { get; private set; }

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

        public PipelineManager(string projectDir, string projectFilename, string outputDir, string intermediateDir, AssembliesMgr assembliesMgr)
        {
            _buildEventsMap = new Dictionary<string, List<BuildEvent>>();

            Logger = null;

            ProjectDirectory = LegacyPathHelper.NormalizeDirectory(projectDir);
            ProjectFilename = projectFilename;

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
            OpaqueDataDictionary result = new OpaqueDataDictionary();

            ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(processorName);

            if (processorInfo == null || processorParameters == null)
            {
                return result;
            }

            Type processorType = processorInfo.Type;
            foreach (var param in processorParameters)
            {
                PropertyInfo property = processorType.GetProperty(param.Key, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                if (property == null || property.GetSetMethod(false) == null)
                    continue;

                // Make sure we can assign the value.
                if (!property.PropertyType.IsInstanceOfType(param.Value))
                {
                    // Make sure we can convert the value.
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(property.PropertyType);
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
            if (String.IsNullOrEmpty(outputFilepath))
            {
                string filename = Path.GetFileNameWithoutExtension(sourceFilepath) + ".xnb";
                string directory = LegacyPathHelper.GetRelativePath(ProjectDirectory,
                                                           Path.GetDirectoryName(sourceFilepath) +
                                                           Path.DirectorySeparatorChar);
                outputFilepath = Path.Combine(OutputDirectory, directory, filename);
            }
            else
            {
                // If the extension is not XNB or the source file extension then add XNB.
                string sourceExt = Path.GetExtension(sourceFilepath);
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

        private void DeleteBuildEvent(string destFile)
        {
            string relativeEventPath = Path.ChangeExtension(LegacyPathHelper.GetRelativePath(OutputDirectory, destFile), BuildEvent.Extension);
            string intermediateEventPath = Path.Combine(IntermediateDirectory, ProjectFilename, relativeEventPath);
            if (File.Exists(intermediateEventPath))
                File.Delete(intermediateEventPath);
        }

        private void SaveBuildEvent(string destFile, BuildEvent buildEvent)
        {
            string relativeEventPath = Path.ChangeExtension(LegacyPathHelper.GetRelativePath(OutputDirectory, destFile), BuildEvent.Extension);
            string intermediateEventPath = Path.Combine(IntermediateDirectory, ProjectFilename, relativeEventPath);
            intermediateEventPath = Path.GetFullPath(intermediateEventPath);
            buildEvent.SaveBinary(intermediateEventPath);
        }

        internal BuildEvent LoadBuildEvent(string destFile)
        {
            string relativeEventPath = Path.ChangeExtension(LegacyPathHelper.GetRelativePath(OutputDirectory, destFile), BuildEvent.Extension);
            string intermediateEventPath = Path.Combine(IntermediateDirectory, Path.GetFileNameWithoutExtension(ProjectFilename), relativeEventPath);
            intermediateEventPath = Path.GetFullPath(intermediateEventPath);
            return BuildEvent.LoadBinary(intermediateEventPath);
        }

        internal BuildEvent CreateBuildEvent(string sourceFilepath, string outputFilepath, string importerName, string processorName, OpaqueDataDictionary processorParameters)
        {
            sourceFilepath = LegacyPathHelper.Normalize(sourceFilepath);
            ResolveOutputFilepath(sourceFilepath, ref outputFilepath);
            _assembliesMgr.ResolveImporterAndProcessor(sourceFilepath, ref importerName, ref processorName);
            OpaqueDataDictionary ProcessorParams = ValidateProcessorParameters(processorName, processorParameters);

            BuildEvent buildEvent = new BuildEvent
            {
                SourceFile = sourceFilepath,
                DestFile = outputFilepath,
                Importer = importerName,
                Processor = processorName,
                ProcessorParams = ProcessorParams,
            };
            return buildEvent;
        }

        internal void BuildContent(BuildEvent buildEvent, ContentBuildLogger logger, BuildEvent cachedBuildEvent, string destFilePath)
        {
            if (!File.Exists(buildEvent.SourceFile))
            {
                logger.LogMessage("{0}", buildEvent.SourceFile);
                throw new PipelineException("The source file '{0}' does not exist.", buildEvent.SourceFile);
            }

            logger.PushFile(buildEvent.SourceFile);

            // Keep track of all build events. (Required to resolve automatic names "AssetName_n".)
            TrackBuildEvent(buildEvent);

            bool rebuild = NeedsRebuild(_assembliesMgr, buildEvent, cachedBuildEvent);
            if (rebuild)
                logger.LogMessage("{0}", buildEvent.SourceFile);
            else
                logger.LogMessage("Skipping {0}", buildEvent.SourceFile);

            try
            {
                if (!rebuild)
                {
                    // While this asset doesn't need to be rebuilt the dependent assets might.
                    foreach (string asset in cachedBuildEvent.BuildAsset)
                    {
                        BuildEvent assetCachedBuildEvent = LoadBuildEvent(asset);

                        // If we cannot find the cached event for the dependancy
                        // then we have to trigger a rebuild of the parent content.
                        if (assetCachedBuildEvent == null)
                        {
                            rebuild = true;
                            break;
                        }

                        BuildEvent depBuildEvent = new BuildEvent
                        {
                            SourceFile = assetCachedBuildEvent.SourceFile,
                            DestFile = assetCachedBuildEvent.DestFile,
                            Importer = assetCachedBuildEvent.Importer,
                            Processor = assetCachedBuildEvent.Processor,
                            ProcessorParams = assetCachedBuildEvent.ProcessorParams,
                        };

                        // Give the asset a chance to rebuild.                   
                        BuildContent(depBuildEvent, logger, assetCachedBuildEvent, asset);
                    }
                }

                // Do we need to rebuild?
                if (rebuild)
                {
                    DateTime startTime = DateTime.UtcNow;

                    // Import and process the content.
                    object processedObject = ProcessContent(buildEvent, logger);

                    // Write the content to disk.
                    WriteXnb(processedObject, buildEvent);

                    // Store the timestamp of the DLLs containing the importer and processor.
                    ImporterInfo importerInfo = _assembliesMgr.GetImporterInfo(buildEvent.Importer);
                    ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(buildEvent.Processor);
                    buildEvent.ImporterTime = (importerInfo != null) ? importerInfo.AssemblyTimestamp : DateTime.MaxValue;
                    buildEvent.ProcessorTime = (processorInfo != null) ? processorInfo.AssemblyTimestamp : DateTime.MaxValue;

                    // Store the new event into the intermediate folder.
                    SaveBuildEvent(destFilePath, buildEvent);

                    TimeSpan buildTime = DateTime.UtcNow - startTime;
                }
            }
            finally
            {
                logger.PopFile();
            }
        }

        public object ProcessContent(BuildEvent buildEvent, ContentBuildLogger logger)
        {
            if (!File.Exists(buildEvent.SourceFile))
                throw new PipelineException("The source file '{0}' does not exist.", buildEvent.SourceFile);

            // Store the last write time of the source file
            // so we can detect if it has been changed.
            buildEvent.SourceTime = File.GetLastWriteTime(buildEvent.SourceFile);

            // Make sure we can find the importer and processor.
            ImporterInfo importerInfo = _assembliesMgr.GetImporterInfo(buildEvent.Importer);
            if (importerInfo == null)
                throw new PipelineException("Failed to create importer '{0}'", buildEvent.Importer);
            IContentImporter importer = _assembliesMgr.CreateImporter(importerInfo);
            if (importer == null)
                throw new PipelineException("Failed to create importer '{0}'", buildEvent.Importer);

            // Try importing the content.
            object importedObject;
            try
            {
                ImporterContext importContext = new ImporterContext(this, logger, buildEvent);
                importedObject = importer.Import(buildEvent.SourceFile, importContext);
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
                throw new PipelineException(string.Format("Importer '{0}' had unexpected failure.", buildEvent.Importer), inner);
            }

            // The pipelineEvent.Processor can be null or empty. In this case the
            // asset should be imported but not processed.
            if (string.IsNullOrEmpty(buildEvent.Processor))
                return importedObject;

            ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(buildEvent.Processor);
            if (processorInfo == null)
                throw new PipelineException("Failed to create processor '{0}'", buildEvent.Processor);
            IContentProcessor processor = _assembliesMgr.CreateProcessor(processorInfo, buildEvent.ProcessorParams);
            if (processor == null)
                throw new PipelineException("Failed to create processor '{0}'", buildEvent.Processor);

            // Make sure the input type is valid.
            if (!processor.InputType.IsAssignableFrom(importedObject.GetType()))
            {
                throw new PipelineException(
                    string.Format("The type '{0}' cannot be processed by {1} as a {2}.",
                    importedObject.GetType().FullName,
                    buildEvent.Processor,
                    processor.InputType.FullName));
            }

            // Process the imported object.
            object processedObject;
            try
            {
                var processContext = new ProcessorContext(this, logger, buildEvent);
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
                throw new PipelineException(string.Format("Processor '{0}' had unexpected failure.", buildEvent.Processor), inner);
            }

            return processedObject;
        }

        public void CleanContent(ContentBuildLogger logger, string sourceFilepath, string outputFilepath)
        {
            // First try to load the event file.
            ResolveOutputFilepath(sourceFilepath, ref outputFilepath);
            BuildEvent cachedBuildEvent = LoadBuildEvent(outputFilepath);

            if (cachedBuildEvent != null)
            {
                // Recursively clean additional (nested) assets.
                foreach (string asset in cachedBuildEvent.BuildAsset)
                {
                    BuildEvent assetCachedBuildEvent = LoadBuildEvent(asset);

                    if (assetCachedBuildEvent == null)
                    {
                        logger.LogMessage("Cleaning {0}", asset);

                        // Remove asset (.xnb file) from output folder.
                        if (File.Exists(asset))
                            File.Delete(asset);

                        // Remove event file (.mgcontent file) from intermediate folder.
                        DeleteBuildEvent(asset);
                        continue;
                    }

                    CleanContent(logger, string.Empty, asset);
                }

                // Remove related output files (non-XNB files) that were copied to the output folder.
                foreach (string asset in cachedBuildEvent.BuildOutput)
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
            DeleteBuildEvent(outputFilepath);

            lock (_buildEventsMap)
            {
                _buildEventsMap.Remove(sourceFilepath);
            }
        }

        private void WriteXnb(object content, BuildEvent buildEvent)
        {
            // Make sure the output directory exists.
            string outputFileDir = Path.GetDirectoryName(buildEvent.DestFile);

            Directory.CreateDirectory(outputFileDir);

            if (_compiler == null)
                _compiler = new ContentCompiler();

            // Write the XNB.
            using (Stream stream = new FileStream(buildEvent.DestFile, FileMode.Create, FileAccess.Write, FileShare.None))
                _compiler.Compile(stream, content, Platform, Profile, CompressContent, OutputDirectory, outputFileDir);

            // Store the last write time of the output XNB here
            // so we can verify it hasn't been tampered with.
            buildEvent.DestTime = File.GetLastWriteTime(buildEvent.DestFile);
        }

        /// <summary>
        /// Stores the pipeline build event (in memory) if no matching event is found.
        /// </summary>
        /// <param name="buildEvent">The pipeline build event.</param>
        internal void TrackBuildEvent(BuildEvent buildEvent)
        {
            List<BuildEvent> buildEvents;
            lock (_buildEventsMap)
            {
                if (!_buildEventsMap.TryGetValue(buildEvent.SourceFile, out buildEvents))
                {
                    buildEvents = new List<BuildEvent>(1);
                    _buildEventsMap.Add(buildEvent.SourceFile, buildEvents);
                }

                BuildEvent matchedBuildEvent = FindMatchingEvent(buildEvents, buildEvent.DestFile, buildEvent.Importer, buildEvent.Processor, buildEvent.ProcessorParams);
                if (matchedBuildEvent == null)
                    buildEvents.Add(buildEvent);
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
        public string GetAssetName(string sourceFileName, string importerName, string processorName, OpaqueDataDictionary processorParameters, ContentBuildLogger logger)
        {
            Debug.Assert(Path.IsPathRooted(sourceFileName), "Absolute path expected.");

            // Get source file name, which is used for lookup in _buildEventsMap.
            sourceFileName = LegacyPathHelper.Normalize(sourceFileName);
            string relativeSourceFileName = LegacyPathHelper.GetRelativePath(ProjectDirectory, sourceFileName);

            List<BuildEvent> buildEvents;
            lock (_buildEventsMap)
            {
                if (_buildEventsMap.TryGetValue(sourceFileName, out buildEvents))
                {
                    // This source file has already been build.
                    // --> Compare pipeline build events.
                    _assembliesMgr.ResolveImporterAndProcessor(sourceFileName, ref importerName, ref processorName);

                    BuildEvent matchedBuildEvent = FindMatchingEvent(buildEvents, null, importerName, processorName, processorParameters);
                    if (matchedBuildEvent != null)
                    {
                        // Matching pipeline build event found.
                        string existingName = matchedBuildEvent.DestFile;
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
                BuildEvent existingBuildEvent = LoadBuildEvent(destFile);
                if (existingBuildEvent == null)
                    return destFile;

                string existingBuildEventDestFile = existingBuildEvent.DestFile;
                existingBuildEventDestFile = LegacyPathHelper.GetRelativePath(ProjectDirectory, existingBuildEventDestFile);
                existingBuildEventDestFile = Path.Combine(Path.GetDirectoryName(existingBuildEventDestFile), Path.GetFileNameWithoutExtension(existingBuildEventDestFile));
                existingBuildEventDestFile = LegacyPathHelper.Normalize(existingBuildEventDestFile);

                string fullDestFile = Path.Combine(OutputDirectory, destFile);
                string relativeDestFile = LegacyPathHelper.GetRelativePath(ProjectDirectory, fullDestFile);
                relativeDestFile = LegacyPathHelper.Normalize(relativeDestFile);

                if (existingBuildEventDestFile.Equals(relativeDestFile) &&
                    existingBuildEvent.Importer  == importerName &&
                    existingBuildEvent.Processor == processorName)
                {
                    OpaqueDataDictionary defaultValues = null;
                    ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(processorName);
                    if (processorInfo != null)
                        defaultValues = processorInfo.DefaultValues;
                    if (AreParametersEqual(existingBuildEvent.ProcessorParams, processorParameters, defaultValues))
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
        private BuildEvent FindMatchingEvent(List<BuildEvent> pipelineBuildEvents, string destFile, string importerName, string processorName, OpaqueDataDictionary processorParameters)
        {
            foreach (BuildEvent existingBuildEvent in pipelineBuildEvents)
            {
                if ((destFile == null || existingBuildEvent.DestFile.Equals(destFile))
                &&  existingBuildEvent.Importer == importerName
                &&  existingBuildEvent.Processor == processorName)
                {
                    OpaqueDataDictionary defaultValues = null;
                    ProcessorInfo processorInfo = _assembliesMgr.GetProcessorInfo(processorName);
                    if (processorInfo != null)
                        defaultValues = processorInfo.DefaultValues;
                    if (AreParametersEqual(existingBuildEvent.ProcessorParams, processorParameters, defaultValues))
                    {
                        return existingBuildEvent;
                    }
                }
            }

            return null;
        }

        internal static bool NeedsRebuild(AssembliesMgr assembliesMgr, BuildEvent buildEvent, BuildEvent cachedbuildEvent)
        {
            // If we have no previously cached build event then we cannot
            // be sure that the state hasn't changed... force a rebuild.
            if (cachedbuildEvent == null)
                return true;

            // Verify that the last write time of the source file matches
            // what we recorded when it was built.  If it is different
            // that means someone modified it and we need to rebuild.
            DateTime sourceWriteTime = File.GetLastWriteTime(buildEvent.SourceFile);
            if (cachedbuildEvent.SourceTime != sourceWriteTime)
                return true;

            // Do the same test for the dest file.
            DateTime destWriteTime = File.GetLastWriteTime(buildEvent.DestFile);
            if (cachedbuildEvent.DestTime != destWriteTime)
                return true;

            // If the source file is newer than the dest file
            // then it must have been updated and needs a rebuild.
            if (sourceWriteTime >= destWriteTime)
                return true;

            // Are any of the dependancy files newer than the dest file?
            foreach (string depFile in cachedbuildEvent.Dependencies)
            {
                if (File.GetLastWriteTime(depFile) >= destWriteTime)
                    return true;
            }

            // This shouldn't happen...  but if the source or dest files changed
            // then force a rebuild.
            if (cachedbuildEvent.SourceFile != buildEvent.SourceFile ||
                cachedbuildEvent.DestFile != buildEvent.DestFile)
                return true;

            // Did the importer change?
            if (cachedbuildEvent.Importer != buildEvent.Importer)
                return true;

            // Did the processor change?
            if (cachedbuildEvent.Processor != buildEvent.Processor)
                return true;

            // Did the importer assembly change?
            ImporterInfo cachedImporterInfo = assembliesMgr.GetImporterInfo(cachedbuildEvent.Importer);
            DateTime importerAssemblyTimestamp = (cachedImporterInfo != null) ? cachedImporterInfo.AssemblyTimestamp : DateTime.MaxValue;
            if (importerAssemblyTimestamp > cachedbuildEvent.ImporterTime)
                return true;

            // Did the processor assembly change?
            ProcessorInfo cachedProcessorInfo = assembliesMgr.GetProcessorInfo(cachedbuildEvent.Processor);
            DateTime processorInfoAssemblyTimestamp = (cachedProcessorInfo != null) ? cachedProcessorInfo.AssemblyTimestamp : DateTime.MaxValue;
            if (processorInfoAssemblyTimestamp > cachedbuildEvent.ProcessorTime)
                return true;

            // Did the parameters change?
            OpaqueDataDictionary defaultValues = null;
            ProcessorInfo buildProcessorInfo = assembliesMgr.GetProcessorInfo(buildEvent.Processor);
            if (buildProcessorInfo != null)
                defaultValues = buildProcessorInfo.DefaultValues;
            if (!AreParametersEqual(cachedbuildEvent.ProcessorParams, buildEvent.ProcessorParams, defaultValues))
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
                OpaqueDataDictionary dummy = parameters0;
                parameters0 = parameters1;
                parameters1 = dummy;
            }

            // Compare parameters0 with parameters1 or defaultValues.
            foreach (KeyValuePair<string, object> pair in parameters0)
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
            foreach (KeyValuePair<string, object> pair in parameters1)
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
            TypeConverter typeConverter = TypeDescriptor.GetConverter(value.GetType());
            return typeConverter.ConvertToInvariantString(value);
        }

    }
}
