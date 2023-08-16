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
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ProxyServer.Assemblies;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    class PipelineProxyServer : IPCServer
    {
        private string BaseDirectory;
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
            
            var sourceFilename = ReadString();
            var sourceTool = ReadString();
            var fragmentIdentifier = ReadString();

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
                        break;
                    case ProxyMsgType.BaseDirectory:
                        SetBaseDirectory();
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

        private void SetBaseDirectory()
        {
            var baseDirectory = Reader.ReadString();
            this.BaseDirectory = baseDirectory;
        }

        private void AddAssembly()
        {
            var contextGuid = ReadGuid();
            var assemblyPath = Reader.ReadString();

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
            var baseDirectory = (this.BaseDirectory == null) ? String.Empty : LegacyPathHelper.Normalize(this.BaseDirectory);

            var logger = new BuildLogger(this, contextGuid);
            _assembliesMgr.AddAssembly(logger, baseDirectory, assemblyPath);
        }

        private void GetImporters()
        {
            lock (Writer)
            {
                for (var e = _assembliesMgr.GetImporters(); e.MoveNext(); )
                {
                    WriteMsg(ProxyMsgType.Importer);
                    e.Current.Write(Writer);
                }
                WriteMsg(ProxyMsgType.End);
                Writer.Flush();
            }
        }
        
        private void GetProcessors()
        {
            lock (Writer)
            {
                for (var e = _assembliesMgr.GetProcessors(); e.MoveNext(); )
                {
                    WriteMsg(ProxyMsgType.Processor);
                    e.Current.Write(Writer);
                }
                WriteMsg(ProxyMsgType.End);
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
            this._globalContext.OutputDir = Reader.ReadString();
        }

        private void SetIntermediateDir()
        {
            this._globalContext.IntermediateDir = Reader.ReadString();
        }

        private void SetPlatform()
        {
            this._globalContext.Platform = (TargetPlatform)Reader.ReadInt32();
        }

        private void SetConfig()
        {
            this._globalContext.Config = Reader.ReadString();
        }

        private void SetProfile()
        {
            this._globalContext.Profile = (GraphicsProfile)Reader.ReadInt32();
        }

        private void SetCompress()
        {
            this._globalContext.Compress = Reader.ReadBoolean();
        }
        
        private void SetImporter()
        {
            this._globalContext.Importer = ReadString();

        }
        private void SetProcessor()
        {
            this._globalContext.Processor = ReadString();
            this._globalContext.ProcessorParams.Clear();
        }
        private void AddProcessorParam()
        {
            var ProcessorParam = Reader.ReadString();
            var ProcessorParamValue = Reader.ReadString();
            this._globalContext.ProcessorParams.Add(ProcessorParam, ProcessorParamValue);
        }

        private void Copy()
        {
            var contextGuid = ReadGuid();
            this._globalContext.OriginalPath = Reader.ReadString();
            this._globalContext.DestinationPath = ReadString();

            var itemContext = this._globalContext.CreateContext(contextGuid);
            
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
            var contextGuid = ReadGuid();
            this._globalContext.OriginalPath = Reader.ReadString();
            this._globalContext.DestinationPath = ReadString();

            var itemContext = this._globalContext.CreateContext(contextGuid);

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
            var Config = itemContext.Config;
            var Platform = itemContext.Platform;
            var Profile = itemContext.Profile;
            var CompressContent = itemContext.Compress;
            var _outputDir = itemContext.OutputDir;
            var _intermediateDir = itemContext.IntermediateDir;

            var projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);
            
            var outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            var intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));
            
            PipelineManager _manager;
            _manager = new PipelineManager(projectDirectory, outputPath, intermediatePath, _assembliesMgr);
            _manager.CompressContent = CompressContent;
            ContentBuildLogger logger = new BuildLogger(this, itemContext.Guid);
            _manager.Logger = logger;


            // Load the previously serialized list of built content.
            var contentFile = Path.Combine(intermediatePath, PipelineBuildEvent.Extension);
            var previousContent = SourceFileCollection.Read(contentFile);

            // If the target changed in any way then we need to force
            // a full rebuild even under incremental builds.
            var targetChanged = previousContent.Config != Config ||
                                previousContent.Platform != Platform ||
                                previousContent.Profile != Profile;


            var newContent = new SourceFileCollection
            {
                Profile = _manager.Profile = Profile,
                Platform = _manager.Platform = Platform,
                Config = _manager.Config = Config
            };
            
            try
            {
                _manager.BuildContent(logger,
                                      c.SourceFile,
                                      c.OutputFile,
                                      c.Importer,
                                      c.Processor,
                                      c.ProcessorParams);

                newContent.SourceFiles.Add(c.SourceFile);
                newContent.DestFiles.Add(c.OutputFile);
            }
            catch (InvalidContentException ex)
            {
                var msg = ex.Message;
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
                var msg = ex.Message;
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
                var msg = ex.Message;
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
                newContent.Merge(previousContent);
            }

            // Delete the old file and write the new content 
            // list if we have any to serialize.
            if (File.Exists(contentFile))
                File.Delete(contentFile);

            if (newContent.SourceFiles.Count > 0)
                newContent.Write(contentFile);

            return TaskResult.SUCCEEDED;
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
            var Config = itemContext.Config;
            var Platform = itemContext.Platform;
            var Profile = itemContext.Profile;
            var CompressContent = itemContext.Compress;
            var _outputDir = itemContext.OutputDir;
            var _intermediateDir = itemContext.IntermediateDir;

            var projectDirectory = this.BaseDirectory;
            projectDirectory = LegacyPathHelper.Normalize(projectDirectory);

            var outputPath = ReplaceSymbols(_outputDir);
            if (!Path.IsPathRooted(outputPath))
                outputPath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, outputPath)));

            var intermediatePath = ReplaceSymbols(_intermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = LegacyPathHelper.Normalize(Path.GetFullPath(Path.Combine(projectDirectory, intermediatePath)));

            var logger = new BuildLogger(this, itemContext.Guid);

            // Process copy items (files that bypass the content pipeline)
            try
            {
                // Figure out an asset name relative to the project directory,
                // retaining the file extension.
                // Note that replacing a sub-path like this requires consistent
                // directory separator characters.
                var relativeName = c.Link;
                if (string.IsNullOrWhiteSpace(relativeName))
                    relativeName = c.SourceFile.Replace(projectDirectory, string.Empty)
                                        .TrimStart(Path.DirectorySeparatorChar)
                                        .TrimStart(Path.AltDirectorySeparatorChar);
                var dest = Path.Combine(outputPath, relativeName);

                // Only copy if the source file is newer than the destination.
                // We may want to provide an option for overriding this, but for
                // nearly all cases this is the desired behavior.
                if (File.Exists(dest) && !Rebuild)
                {
                    var srcTime = File.GetLastWriteTimeUtc(c.SourceFile);
                    var dstTime = File.GetLastWriteTimeUtc(dest);
                    if (srcTime <= dstTime)
                    {
                        if (string.IsNullOrEmpty(c.Link))
                            logger.LogMessage(String.Format("Skipping {0}", c.SourceFile));
                        else
                            logger.LogMessage(String.Format("Skipping {0} => {1}", c.SourceFile, c.Link));

                        return TaskResult.SUCCEEDED;
                    }
                }

                var startTime = DateTime.UtcNow;

                // Create the destination directory if it doesn't already exist.
                var destPath = Path.GetDirectoryName(dest);
                if (!Directory.Exists(destPath))
                    Directory.CreateDirectory(destPath);

                File.Copy(c.SourceFile, dest, true);

                // Destination file should not be read-only even if original was.
                var fileAttr = File.GetAttributes(dest);
                fileAttr = fileAttr & (~FileAttributes.ReadOnly);
                File.SetAttributes(dest, fileAttr);

                var buildTime = DateTime.UtcNow - startTime;

                if (string.IsNullOrEmpty(c.Link))
                    logger.LogMessage(String.Format("{0}", c.SourceFile));
                else
                    logger.LogMessage(String.Format("{0} => {1}", c.SourceFile, c.Link));
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
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
