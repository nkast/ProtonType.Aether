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
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ProxyServer.Assemblies;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    class PipelineProxyServer : IPCServer
    {
        private string BaseDirectory;
        private string ProjectFilename;

        private readonly ParametersContext _globalContext = new ParametersContext();
        private readonly ContentBuildLogger _globalLogger;
        private readonly AssembliesMgr _assembliesMgr;
        
        public PipelineProxyServer(string uid) : base(uid)
        {
            _globalContext = new ParametersContext();
            _globalLogger = new BuildLogger(this, _globalContext.Guid);
            _assembliesMgr = new AssembliesMgr();

            // load build-in importers/processors
            AddAssembly(_globalContext.Guid, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.PassThroughProcessor).Assembly.Location); // Common
            AddAssembly(_globalContext.Guid, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.SoundEffectProcessor).Assembly.Location); // Audio
            AddAssembly(_globalContext.Guid, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.VideoProcessor).Assembly.Location); // Media
            AddAssembly(_globalContext.Guid, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.TextureProcessor).Assembly.Location); // Graphics
            AddAssembly(_globalContext.Guid, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.EffectProcessor).Assembly.Location); // Graphics Effects
        }
 
        private void WriteMsg(ProxyMsgType msgType)
        {
            Writer.Write((Int32)msgType);
        }

        private ProxyMsgType ReadMsg()
        {
            return (ProxyMsgType)Reader.ReadInt32();
        }
        
        private void WriteString(string value)
        {
            bool isNotNull = value != null;
            Writer.Write(isNotNull);
            if (!isNotNull) return;

            Writer.Write(value);
        }

        private string ReadString()
        {
            bool isNotNull = Reader.ReadBoolean();
            if (!isNotNull) return null;

            return Reader.ReadString();
        }

        private void WriteGuid(Guid guid)
        {
            Writer.Write(guid.ToByteArray());
        }

        private Guid ReadGuid()
        {
            return new Guid(Reader.ReadBytes(16));
        }

        private void WriteTaskResult(TaskResult taskResult)
        {
            Writer.Write((Int32)taskResult);
        }

        private TaskResult ReadTaskResult()
        {
            return (TaskResult)Reader.ReadInt32();
        }

        private void WriteContentIdentity(ContentIdentity value)
        {
            bool isNotNull = value != null;
            Writer.Write(isNotNull);
            if (!isNotNull) return;

            WriteString(value.SourceFilename);
            WriteString(value.SourceTool);
            WriteString(value.FragmentIdentifier);
        }

        private ContentIdentity ReadContentIdentity()
        {
            bool isNotNull = Reader.ReadBoolean();
            if (!isNotNull) return null;
            
            string sourceFilename = ReadString();
            string sourceTool = ReadString();
            string fragmentIdentifier = ReadString();

            return new ContentIdentity(sourceFilename, sourceTool, fragmentIdentifier);
        }

        internal override void Run()
        {
            for (ProxyMsgType msg = ProxyMsgType.Undefined; msg != ProxyMsgType.Terminate; )
            {
                try { msg = ReadMsg(); }
                catch(Exception ex) { break; }

                switch (msg)
                {
                    case ProxyMsgType.Terminate:
                        Terminate();
                        return;
                    case ProxyMsgType.BaseDirectory:
                        SetBaseDirectory();
                        break;                    
                    case ProxyMsgType.ProjectFilename:
                        SetProjectFilename();
                        break;
                    case ProxyMsgType.AddAssembly:
                        AddAssembly();
                        break;
                    case ProxyMsgType.GetImporters:
                        GetImporters();
                        break;
                    case ProxyMsgType.GetProcessors:
                        GetProcessors();
                        break;

                    case ProxyMsgType.ParamRebuild:
                        break;
                    case ProxyMsgType.ParamIncremental:
                        break;
                    case ProxyMsgType.ParamOutputDir:
                        SetOutputDir();
                        break;
                    case ProxyMsgType.ParamIntermediateDir:
                        SetIntermediateDir();
                        break;
                    case ProxyMsgType.ParamPlatform:
                        SetPlatform();
                        break;
                    case ProxyMsgType.ParamConfig:
                        SetConfig();
                        break;
                    case ProxyMsgType.ParamProfile:
                        SetProfile();
                        break;
                    case ProxyMsgType.ParamCompress:
                        SetCompress();
                        break;

                    case ProxyMsgType.ParamImporter:
                        SetImporter();
                        break;
                    case ProxyMsgType.ParamProcessor:
                        SetProcessor();
                        break;
                    case ProxyMsgType.ParamProcessorParam:
                        AddProcessorParam();
                        break;
                        
                    case ProxyMsgType.Copy:
                        Copy();
                        break;
                    case ProxyMsgType.Build:
                        Build();
                        break;

                    default:
                        throw new Exception("Unknown Message.");
                }
            }
        }

        private void Terminate()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.Terminate);
                Writer.Flush();
            }
        }

        private void SetBaseDirectory()
        {
            string baseDirectory = Reader.ReadString();
            this.BaseDirectory = baseDirectory;
        }

        private void SetProjectFilename()
        {
            string projectFilename = Reader.ReadString();
            this.ProjectFilename = projectFilename;
        }

        private void AddAssembly()
        {
            Guid contextGuid = ReadGuid();
            string assemblyPath = Reader.ReadString();

            AddAssembly(contextGuid, assemblyPath);
            TaskResult taskResult = TaskResult.SUCCEEDED;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.TaskEnd);
                WriteGuid(contextGuid);
                WriteTaskResult(taskResult);
                Writer.Flush();
            }
        }

        private void AddAssembly(Guid contextGuid, string assemblyPath)
        {
            if (assemblyPath == null)
                throw new ArgumentException("assemblyPath cannot be null!");

            assemblyPath = ReplaceSymbols(assemblyPath);
            string baseDirectory = (this.BaseDirectory == null) ? String.Empty : LegacyPathHelper.Normalize(this.BaseDirectory);

            ContentBuildLogger logger = new BuildLogger(this, contextGuid);
            _assembliesMgr.AddAssembly(logger, baseDirectory, assemblyPath);
        }

        private void GetImporters()
        {
            Guid contextGuid = ReadGuid();

            lock (Writer)
            {
                for (IEnumerator<ImporterDescription> e = _assembliesMgr.GetImporters(); e.MoveNext(); )
                {
                    WriteMsg(ProxyMsgType.Importer);
                    WriteGuid(contextGuid);
                    e.Current.Write(Writer);
                }

                TaskResult taskResult = TaskResult.SUCCEEDED;

                WriteMsg(ProxyMsgType.TaskEnd);
                WriteGuid(contextGuid);
                WriteTaskResult(taskResult);
                Writer.Flush();
            }
        }
        
        private void GetProcessors()
        {
            Guid contextGuid = ReadGuid();

            lock (Writer)
            {
                for (IEnumerator<ProcessorDescription> e = _assembliesMgr.GetProcessors(); e.MoveNext(); )
                {
                    WriteMsg(ProxyMsgType.Processor);
                    WriteGuid(contextGuid);
                    e.Current.Write(Writer);
                }
                TaskResult taskResult = TaskResult.SUCCEEDED;

                WriteMsg(ProxyMsgType.TaskEnd);
                WriteGuid(contextGuid);
                WriteTaskResult(taskResult);
                Writer.Flush();
            }           
        }
        

        #region Logger

        internal void LogMessage(Guid contextGuid, string currentFilename, string message)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.LogMessage);
                WriteGuid(contextGuid);
                WriteString(currentFilename);
                WriteString(message);
                Writer.Flush();
            }
        }

        internal void LogImportantMessage(Guid contextGuid, string currentFilename, string message)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.LogImportantMessage);
                WriteGuid(contextGuid);
                WriteString(currentFilename);
                WriteString(message);
                Writer.Flush();
            }
        }

        internal void LogWarning(Guid contextGuid, string currentFilename, string helpLink, ContentIdentity contentIdentity, string message)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.LogWarning);
                WriteGuid(contextGuid);
                WriteString(currentFilename);
                WriteString(helpLink);
                WriteContentIdentity(contentIdentity);
                WriteString(message);
                Writer.Flush();
            }
        }
        
        internal void LogError(Guid contextGuid, string currentFilename, ContentIdentity contentIdentity, string message)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.LogError);
                WriteGuid(contextGuid);
                WriteString(currentFilename);
                WriteContentIdentity(contentIdentity);
                WriteString(message);
                Writer.Flush();
            }
        }


        #endregion Logger


        private void SetOutputDir()
        {
            string outputDir = Reader.ReadString();
            this._globalContext.OutputDir = outputDir;
        }

        private void SetIntermediateDir()
        {
            string intermediateDir = Reader.ReadString();
            this._globalContext.IntermediateDir = intermediateDir;
        }

        private void SetPlatform()
        {
            TargetPlatform platform = (TargetPlatform)Reader.ReadInt32();
            this._globalContext.Platform = platform;
        }

        private void SetConfig()
        {
            string config = Reader.ReadString();
            this._globalContext.Config = config;
        }

        private void SetProfile()
        {
            this._globalContext.Profile = (GraphicsProfile)Reader.ReadInt32();
        }

        private void SetCompress()
        {
            bool compress = Reader.ReadBoolean();
            this._globalContext.Compress = compress;
        }
        
        private void SetImporter()
        {
            string importer = ReadString();
            this._globalContext.Importer = importer;

        }
        private void SetProcessor()
        {
            string processor = ReadString();
            this._globalContext.Processor = processor;
            this._globalContext.ProcessorParams.Clear();
        }
        private void AddProcessorParam()
        {
            string processorParam = Reader.ReadString();
            string processorParamValue = Reader.ReadString();
            this._globalContext.ProcessorParams.Add(processorParam, processorParamValue);
        }

        private void Copy()
        {
            Guid contextGuid = ReadGuid();

            this._globalContext.OriginalPath = Reader.ReadString();
            this._globalContext.DestinationPath = ReadString();

            ParametersContext itemContext = this._globalContext.CreateContext(contextGuid);
            
            TaskResult taskResult = Copy(itemContext);

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.TaskEnd);
                WriteGuid(contextGuid);
                WriteTaskResult(taskResult);
                Writer.Flush();
            }
        }


        private void Build()
        {
            Guid contextGuid = ReadGuid();
            this._globalContext.OriginalPath = Reader.ReadString();
            this._globalContext.DestinationPath = ReadString();

            ParametersContext itemContext = this._globalContext.CreateContext(contextGuid);

            TaskResult taskResult = Build(itemContext);
            
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.TaskEnd);
                WriteGuid(contextGuid);
                WriteTaskResult(taskResult);
                Writer.Flush();
            }
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

        private TaskResult Build(ParametersContext itemContext)
        {
            string sourceFile = itemContext.OriginalPath;
            string link = itemContext.DestinationPath;
            
            // Make sure the source file is absolute.
            if (!Path.IsPathRooted(sourceFile))
                sourceFile = Path.Combine(this.BaseDirectory, sourceFile);

            // link should remain relative, absolute path will get set later when the build occurs

            sourceFile = LegacyPathHelper.Normalize(sourceFile);

            ContentItem c = new ContentItem
            {
                SourceFile = sourceFile,
                OutputFile = link,
                Importer = itemContext.Importer,
                Processor = itemContext.Processor,
                ProcessorParams = new OpaqueDataDictionary()
            };

            // Copy the current processor parameters blind as we
            // will validate and remove invalid parameters during
            // the build process later.
            foreach (var pair in itemContext.ProcessorParams)
            {
                c.ProcessorParams.Add(pair.Key, pair.Value);
            }


            bool Incremental = true;
            bool Rebuild = true;

            string Config = itemContext.Config;
            TargetPlatform Platform = itemContext.Platform;
            GraphicsProfile Profile = itemContext.Profile;
            bool CompressContent = itemContext.Compress;

            string _outputDir = itemContext.OutputDir;
            string _intermediateDir = itemContext.IntermediateDir;

            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            string intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));
            
            PipelineManager _manager;
            _manager = new PipelineManager(projectDirectory, this.ProjectFilename, outputPath, intermediatePath, _assembliesMgr);
            _manager.CompressContent = CompressContent;
            ContentBuildLogger logger = new BuildLogger(this, itemContext.Guid);
            _manager.Logger = logger;


            // Load the previously serialized list of built content.
            SourceFileCollection previousFileCollection = LoadFileCollection(intermediatePath);
            if (previousFileCollection == null)
                previousFileCollection = new SourceFileCollection();

            // If the target changed in any way then we need to force
            // a full rebuild even under incremental builds.
            bool targetChanged = previousFileCollection.Config != Config ||
                                 previousFileCollection.Platform != Platform ||
                                 previousFileCollection.Profile != Profile;


            SourceFileCollection newFileCollection = new SourceFileCollection
            {
                Profile = _manager.Profile = Profile,
                Platform = _manager.Platform = Platform,
                Config = _manager.Config = Config
            };
            
            try
            {
                BuildEvent buildEvent = _manager.CreateBuildEvent(c.SourceFile,
                                      c.OutputFile,
                                      c.Importer,
                                      c.Processor,
                                      c.ProcessorParams
                                      );

                BuildEvent cachedBuildEvent = _manager.LoadBuildEvent(buildEvent.DestFile);
                _manager.BuildContent(buildEvent, logger, cachedBuildEvent, buildEvent.DestFile);

                newFileCollection.AddFile(c.SourceFile, c.OutputFile);
            }
            catch (InvalidContentException ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg += Environment.NewLine;
                    msg += ex.InnerException.ToString();
                }
                this.LogError(itemContext.Guid, c.SourceFile, ex.ContentIdentity, msg);

                return TaskResult.FAILED;
            }
            catch (PipelineException ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg += Environment.NewLine;
                    msg += ex.InnerException.ToString();
                }
                this.LogError(itemContext.Guid, c.SourceFile, null, msg);

                return TaskResult.FAILED;
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg += Environment.NewLine;
                    msg += ex.InnerException.ToString();
                }
                this.LogError(itemContext.Guid, c.SourceFile, null, msg);

                return TaskResult.FAILED;
            }

            // If this is an incremental build we merge the list
            // of previous content with the new list.
            if (Incremental && !targetChanged)
            {
                newFileCollection.Merge(previousFileCollection);
            }

            // Delete the old file and write the new content 
            // list if we have any to serialize.
            DeleteFileCollection(intermediatePath);

            if (newFileCollection.SourceFilesCount > 0)
                SaveFileCollection(intermediatePath, newFileCollection);

            return TaskResult.SUCCEEDED;
        }

        private void DeleteFileCollection(string intermediatePath)
        {
            string intermediateFileCollectionPath = Path.Combine(intermediatePath, Path.ChangeExtension(this.ProjectFilename, SourceFileCollection.Extension));
            if (File.Exists(intermediateFileCollectionPath))
                File.Delete(intermediateFileCollectionPath);
        }

        private void SaveFileCollection(string intermediatePath, SourceFileCollection fileCollection)
        {
            string intermediateFileCollectionPath = Path.Combine(intermediatePath, Path.ChangeExtension(this.ProjectFilename, SourceFileCollection.Extension));
            fileCollection.SaveBinary(intermediateFileCollectionPath);
        }

        private SourceFileCollection LoadFileCollection(string intermediatePath)
        {
            string intermediateFileCollectionPath = Path.Combine(intermediatePath, Path.ChangeExtension(this.ProjectFilename, SourceFileCollection.Extension));
            return SourceFileCollection.LoadBinary(intermediateFileCollectionPath);
        }

        public class CopyItem
        {
            public string SourceFile;
            public string Link;
        }

        private TaskResult Copy(ParametersContext itemContext)
        {
            string sourceFile = itemContext.OriginalPath;
            string link = itemContext.DestinationPath;

            if (!Path.IsPathRooted(sourceFile))
                sourceFile = Path.Combine(BaseDirectory, sourceFile);

            sourceFile = LegacyPathHelper.Normalize(sourceFile);

            CopyItem c = new CopyItem { SourceFile = sourceFile, Link = link };


            bool Incremental = true;
            bool Rebuild = true;

            string Config = itemContext.Config;
            TargetPlatform Platform = itemContext.Platform;
            GraphicsProfile Profile = itemContext.Profile;
            bool CompressContent = itemContext.Compress;

            string _outputDir = itemContext.OutputDir;
            string _intermediateDir = itemContext.IntermediateDir;

            string projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            string outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            string intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

            ContentBuildLogger logger = new BuildLogger(this, itemContext.Guid);

            // Process copy items (files that bypass the content pipeline)
            try
            {
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

                        return TaskResult.SUCCEEDED;
                    }
                }

                DateTime startTime = DateTime.UtcNow;

                // Create the destination directory if it doesn't already exist.
                string destPath = Path.GetDirectoryName(dest);
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);

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
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                if (ex.InnerException != null)
                {
                    msg += Environment.NewLine;
                    msg += ex.InnerException.ToString();
                }
                this.LogError(itemContext.Guid, c.SourceFile, null, msg);

                return TaskResult.FAILED;
            }

            return TaskResult.SUCCEEDED;
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
    }
}
