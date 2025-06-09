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
    partial class PipelineProxyServer : IPCServer
    {        

        public PipelineProxyServer(string uid) : base(uid)
        {
            AttachAssertListener(new AssertListener());

            _globalLogger = new BuildLogger(this, _globalContext.Guid);

            // load build-in importers/processors
            List<Task> addAssemblyTasks = new List<Task>();
            Task addAssemblyTask0 = ((IPipelineBuilder)this).AddAssemblyAsync((IProxyLoggerBase)_globalLogger, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.PassThroughProcessor).Assembly.Location); // Common
            Task addAssemblyTask1 = ((IPipelineBuilder)this).AddAssemblyAsync((IProxyLoggerBase)_globalLogger, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.SoundEffectProcessor).Assembly.Location); // Audio
            Task addAssemblyTask2 = ((IPipelineBuilder)this).AddAssemblyAsync((IProxyLoggerBase)_globalLogger, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.VideoProcessor).Assembly.Location); // Media
            Task addAssemblyTask3 = ((IPipelineBuilder)this).AddAssemblyAsync((IProxyLoggerBase)_globalLogger, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.TextureProcessor).Assembly.Location); // Graphics
            Task addAssemblyTask4 = ((IPipelineBuilder)this).AddAssemblyAsync((IProxyLoggerBase)_globalLogger, typeof(Microsoft.Xna.Framework.Content.Pipeline.Processors.EffectProcessor).Assembly.Location); // Graphics Effects
            addAssemblyTasks.AddRange(new Task[] { addAssemblyTask0, addAssemblyTask1, addAssemblyTask2, addAssemblyTask3,addAssemblyTask4 });
            Task.WaitAll(addAssemblyTasks.ToArray());
        }

        [Conditional("DEBUG")]
        public static void AttachAssertListener(TraceListener traceListener)
        {
            if (!Debugger.IsAttached
            &&  Trace.Listeners.Count == 1
            &&  Trace.Listeners[0].GetType() == typeof(DefaultTraceListener))
            {
                Trace.Listeners.Clear();
                Trace.Listeners.Add(traceListener);
            }
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
                    case ProxyMsgType.ProjectName:
                        SetProjectName();
                        break;
                    case ProxyMsgType.AddPackage:
                        AddPackage();
                        break;
                    case ProxyMsgType.ResolvePackages:
                        ResolvePackages();
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
                        SetRebuild();
                        break;
                    case ProxyMsgType.ParamIncremental:
                        SetIncremental();
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
                    case ProxyMsgType.ParamCompression:
                        SetCompression();
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
                    case ProxyMsgType.AddItem:
                        AddItem();
                        break;

                    case ProxyMsgType.CleanItems:
                        CleanItems();
                        break;

                    case ProxyMsgType.BuildBegin:
                        BuildBegin();
                        break;
                    case ProxyMsgType.Build:
                        Build();
                        break;
                    case ProxyMsgType.BuildEnd:
                        BuildEnd();
                        break;

                    default:
                        throw new Exception("Unknown Message.");
                }
            }
        }

        private void WriteTaskEnd(Guid contextGuid, TaskResult taskResult)
        {
            WriteMsg(ProxyMsgType.TaskEnd);
            WriteGuid(contextGuid);
            WriteTaskResult(taskResult);
            Writer.Flush();
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
            ((IPipelineBuilder)this).SetBaseDirectory(baseDirectory);
        }

        private void SetProjectName()
        {
            string projectName = Reader.ReadString();
            ((IPipelineBuilder)this).SetProjectName(projectName);
        }

        private void AddPackage()
        {
            Guid contextGuid = ReadGuid();
            Package package;
            package.Name = Reader.ReadString();
            package.Version = Reader.ReadString();

            ContentBuildLogger logger = new BuildLogger(this, contextGuid);

            Task addPackageTask = ((IPipelineBuilder)this).AddPackageAsync((IProxyLoggerBase)logger, package);
            addPackageTask.Wait();

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void ResolvePackages()
        {
            Guid contextGuid = ReadGuid();

            ContentBuildLogger logger = new BuildLogger(this, contextGuid);

            Task resolvePackagesTask = ((IPipelineBuilder)this).ResolvePackagesAsync((IProxyLoggerBase)logger);
            resolvePackagesTask.Wait();

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void AddAssembly()
        {
            Guid contextGuid = ReadGuid();
            string assemblyPath = Reader.ReadString();

            ContentBuildLogger logger = new BuildLogger(this, contextGuid);

            Task addAssemblyTask = ((IPipelineBuilder)this).AddAssemblyAsync((IProxyLoggerBase)logger, assemblyPath);
            addAssemblyTask.Wait();

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void GetImporters()
        {
            Guid contextGuid = ReadGuid();

            lock (Writer)
            {
                Task<List<ImporterDescription>> getImportersTask = ((IPipelineBuilder)this).GetImportersAsync(null);
                getImportersTask.Wait();
                List<ImporterDescription> importers = getImportersTask.Result;

                foreach (ImporterDescription importerDesc in importers)
                {
                    WriteMsg(ProxyMsgType.Importer);
                    WriteGuid(contextGuid);
                    importerDesc.Write(Writer);
                }

                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void GetProcessors()
        {
            Guid contextGuid = ReadGuid();

            lock (Writer)
            {
                Task<List<ProcessorDescription>> getProcessorsTask = ((IPipelineBuilder)this).GetProcessorsAsync(null);
                getProcessorsTask.Wait();
                List<ProcessorDescription> processors = getProcessorsTask.Result;

                foreach (ProcessorDescription processorDesc in processors)
                {
                    WriteMsg(ProxyMsgType.Processor);
                    WriteGuid(contextGuid);
                    processorDesc.Write(Writer);
                }


                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }


        #region Logger

        internal void LogMessage(Guid contextGuid, string message)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.LogMessage);
                WriteGuid(contextGuid);
                WriteString(message);
                Writer.Flush();
            }
        }

        internal void LogImportantMessage(Guid contextGuid, string message)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.LogImportantMessage);
                WriteGuid(contextGuid);
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

        private void SetRebuild()
        {
            ((IPipelineBuilder)this).SetRebuild();
        }

        private void SetIncremental()
        {
            ((IPipelineBuilder)this).SetIncremental();
        }

        private void SetOutputDir()
        {
            string outputDir = Reader.ReadString();
            ((IPipelineBuilder)this).SetOutputDir(outputDir);
        }

        private void SetIntermediateDir()
        {
            string intermediateDir = Reader.ReadString();
            ((IPipelineBuilder)this).SetIntermediateDir(intermediateDir);
        }

        private void SetPlatform()
        {
            TargetPlatform platform = (TargetPlatform)Reader.ReadInt32();
            ((IPipelineBuilder)this).SetPlatform(platform);
        }

        private void SetConfig()
        {
            string config = Reader.ReadString();
            ((IPipelineBuilder)this).SetConfig(config);
        }

        private void SetProfile()
        {
            GraphicsProfile profile = (GraphicsProfile)Reader.ReadInt32();
            ((IPipelineBuilder)this).SetProfile(profile);
        }

        private void SetCompression()
        {
            ContentCompression compression = (ContentCompression)Reader.ReadInt32();
            ((IPipelineBuilder)this).SetCompression(compression);
        }

        private void SetImporter()
        {
            string importer = ReadString();
            ((IPipelineBuilder)this).SetImporter(importer);

        }

        private void SetProcessor()
        {
            string processor = ReadString();
            ((IPipelineBuilder)this).SetProcessor(processor);
        }

        private void AddProcessorParam()
        {
            string processorParam = Reader.ReadString();
            string processorParamValue = Reader.ReadString();
            ((IPipelineBuilder)this).AddProcessorParam(processorParam, processorParamValue);
        }

        private void AddItem()
        {
            Guid contextGuid = ReadGuid();

            string originalPath = Reader.ReadString();
            string destinationPath = ReadString();
            bool isCopy = Reader.ReadBoolean();
                        
            AddItemAsync(originalPath, destinationPath, isCopy);

            ParametersContext itemContext = this._globalContext.CreateContext(contextGuid
                , isCopy
                );
            _items.Add(contextGuid, itemContext);

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void AddItemAsync(string originalPath, string destinationPath, bool isCopy)
        {
            this._globalContext.OriginalPath = originalPath;
            this._globalContext.DestinationPath = destinationPath;
        }

        private void CleanItems()
        {
            Guid contextGuid = ReadGuid();

            ContentBuildLogger logger = new BuildLogger(this, contextGuid);

            Task cleanItemsTask = ((IPipelineBuilder)this).CleanItemsAsync((IProxyLoggerBase)logger);
            cleanItemsTask.Wait();

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void BuildBegin()
        {
            Guid contextGuid = ReadGuid();

            ContentBuildLogger logger = new BuildLogger(this, contextGuid);

            Task buildBeginTask = ((IPipelineBuilder)this).BuildBeginAsync((IProxyLoggerBase)logger);
            buildBeginTask.Wait();

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }

        private void Build()
        {
            Guid contextGuid = ReadGuid();
            Guid itemContextGuid = ReadGuid();

            BuildLogger logger = new BuildLogger(this, contextGuid);

            ParametersContext itemContext = _items[itemContextGuid];
            _items.Remove(itemContext.Guid);
            ParametersContext buildContext = new ParametersContext(contextGuid, itemContext);

            Task buildTask = BuildAsync(logger, buildContext);
            buildTask.ContinueWith((task) =>
            {
                lock (Writer)
                {
                    WriteMsg(ProxyMsgType.TaskEnd);
                    WriteGuid(contextGuid);
                    WriteTaskResult(TaskResult.SUCCEEDED);
                    Writer.Flush();
                }
            });
        }

        private Task BuildAsync(BuildLogger logger, ParametersContext buildContext)
        {
            return Task.Run(() =>
            {
                try
                {
                    switch (buildContext.isCopy)
                    {
                        case true:
                            BuildState(buildContext, Common.BuildState.Building);
                            Copy(logger, buildContext);
                            break;
                        case false:
                            BuildState(buildContext, Common.BuildState.Building);
                            Build(logger, buildContext);
                            break;
                        default:
                            throw new InvalidOperationException();
                    }

                    return;
                }
                catch (InvalidContentException ex)
                {
                    string msg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += Environment.NewLine;
                        msg += ex.InnerException.ToString();
                    }

                    string sourceFile = buildContext.OriginalPath;
                    if (!Path.IsPathRooted(sourceFile))
                        sourceFile = Path.Combine(BaseDirectory, sourceFile);
                    sourceFile = LegacyPathHelper.Normalize(sourceFile);

                    this.LogError(buildContext.Guid, sourceFile, ex.ContentIdentity, msg);
                    BuildState(buildContext, Common.BuildState.Failed);
                    return;
                }
                catch (PipelineException ex)
                {
                    string msg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += Environment.NewLine;
                        msg += ex.InnerException.ToString();
                    }

                    string sourceFile = buildContext.OriginalPath;
                    if (!Path.IsPathRooted(sourceFile))
                        sourceFile = Path.Combine(BaseDirectory, sourceFile);
                    sourceFile = LegacyPathHelper.Normalize(sourceFile);

                    this.LogError(buildContext.Guid, sourceFile, null, msg);
                    BuildState(buildContext, Common.BuildState.Failed);
                    return;
                }
                catch (Exception ex)
                {
                    string msg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        msg += Environment.NewLine;
                        msg += ex.InnerException.ToString();
                    }

                    string sourceFile = buildContext.OriginalPath;
                    if (!Path.IsPathRooted(sourceFile))
                        sourceFile = Path.Combine(BaseDirectory, sourceFile);
                    sourceFile = LegacyPathHelper.Normalize(sourceFile);

                    this.LogError(buildContext.Guid, sourceFile, null, msg);
                    BuildState(buildContext, Common.BuildState.Failed);
                    return;
                }
            });
        }

        internal void BuildState(ParametersContext buildContext, BuildState buildState)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BuildState);
                WriteGuid(buildContext.Guid);
                Writer.Write((Int32)buildState);
                Writer.Flush();
            }
        }

        private void BuildEnd()
        {
            Guid contextGuid = ReadGuid();

            Task buildEndTask = ((IPipelineBuilder)this).BuildEndAsync(null);
            buildEndTask.Wait();

            lock (Writer)
            {
                WriteTaskEnd(contextGuid, TaskResult.SUCCEEDED);
            }
        }


    }
}
