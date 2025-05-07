#region License
//   Copyright 2021-2025 Kastellanos Nikolaos
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
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.ProxyClient
{
    public class PipelineProxyClient : IPCClient
    {
        ConcurrentDictionary<Guid, PipelineAsyncTaskBase> _tasks = new ConcurrentDictionary<Guid, PipelineAsyncTaskBase>();

        public PipelineProxyClient() : base("ProtonType.XnaContentPipeline.ProxyServer.exe")
        {
            return;
        }


        public void BeginListening()
        {
            Task task = Task.Run(() => 
            {
                Run();
            });
        }

        private void Run()
        {
            for (ProxyMsgType msg = ReadMsg(); ; msg = ReadMsg())
            {
                switch (msg)
                {
                    case ProxyMsgType.LogMessage:
                        {
                            Guid guid = ReadGuid();
                            LogMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogImportantMessage:
                        {
                            Guid guid = ReadGuid();
                            LogImportantMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogWarning:
                        {
                            Guid guid = ReadGuid();
                            LogWarning(guid);
                        }
                        break;

                    case ProxyMsgType.LogError:
                        {
                            Guid guid = ReadGuid();
                            LogError(guid);
                        }
                        break;

                    case ProxyMsgType.Importer:
                        {
                            Guid guid = ReadGuid();
                            PipelineAsyncTaskImporters task = (PipelineAsyncTaskImporters)_tasks[guid];
                            ImporterDescription importer = new ImporterDescription(Reader);
                            task.Importers.Add(importer);
                        }
                        break;

                    case ProxyMsgType.Processor:
                        {
                            Guid guid = ReadGuid();
                            PipelineAsyncTaskProcessors task = (PipelineAsyncTaskProcessors)_tasks[guid];
                            ProcessorDescription importer = new ProcessorDescription(Reader);
                            task.Processors.Add(importer);
                        }
                        break;

                    case ProxyMsgType.BuildState:
                        {
                            Guid guid = ReadGuid();
                            BuildState(guid);
                        }
                        break;

                    case ProxyMsgType.TaskEnd:
                        {
                            Guid guid = ReadGuid();
                            TaskResult taskResult = ReadTaskResult();
                            PipelineAsyncTaskBase task = _tasks[guid];
                            _tasks.TryRemove(guid, out task);
                            switch (taskResult)
                            {
                                case TaskResult.SUCCEEDED:
                                    task.InternalOnSucceeded();
                                    break;
                                case TaskResult.FAILED:
                                    task.InternalOnFailed();
                                    break;
                                default:
                                    throw new InvalidOperationException("Task unknown state.");
                            }
                        }
                        break;

                    case ProxyMsgType.Terminate:
                        {
                            return;
                        }
                        break;

                    default:
                        throw new Exception("Unknown Message");
                }
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

        public void Terminate()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.Terminate);
                Writer.Flush();
            }
        }

        public void SetBaseDirectory(string baseDirectory)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BaseDirectory);
                Writer.Write(baseDirectory);
                Writer.Flush();
            }
        }

        public void SetProjectFilename(string projectFilename)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ProjectFilename);
                Writer.Write(projectFilename);
                Writer.Flush();
            }
        }

        public Task AddPackageAsync(IProxyLogger logger, Package package)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.AddPackage);
                WriteGuid(contextGuid);
                Writer.Write(package.Name);
                Writer.Write(package.Version);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public Task ResolvePackagesAsync(IProxyLogger logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ResolvePackages);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public Task AddAssemblyAsync(IProxyLogger logger, string assemblyPath)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.AddAssembly);
                WriteGuid(contextGuid);
                Writer.Write(assemblyPath);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }
        
        public Task<List<ImporterDescription>> GetImportersAsync(IProxyLogger logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskImporters pipelineAsyncTask = new PipelineAsyncTaskImporters(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.GetImporters);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public Task<List<ProcessorDescription>> GetProcessorsAsync(IProxyLogger logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskProcessors pipelineAsyncTask = new PipelineAsyncTaskProcessors(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.GetProcessors);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public void SetRebuild()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamRebuild);
                Writer.Flush();
            }
        }

        public void SetIncremental()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamIncremental);
                Writer.Flush();
            }
        }


        public void SetOutputDir(string outputDir)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamOutputDir);
                Writer.Write(outputDir);
                Writer.Flush();
            }
        }

        public void SetIntermediateDir(string intermediateDir)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamIntermediateDir);
                Writer.Write(intermediateDir);
                Writer.Flush();
            }
        }

        public void SetPlatform(ProxyTargetPlatform platform)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamPlatform);
                Writer.Write((Int32)platform);
                Writer.Flush();
            }
        }
        public void SetConfig(string config)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamConfig);
                Writer.Write(config);
                Writer.Flush();
            }
        }
        public void SetProfile(ProxyGraphicsProfile profile)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamProfile);
                Writer.Write((Int32)profile);
                Writer.Flush();
            }
        }
        public void SetCompression(ContentCompression compression)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamCompression);
                Writer.Write((Int32)compression);
                Writer.Flush();
            }
        }

        public void SetImporter(string importer)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamImporter);
                WriteString(importer);
                Writer.Flush();
            }
        }

        public void SetProcessor(string processor)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamProcessor);
                WriteString(processor);
                Writer.Flush();
            }
        }

        public void AddProcessorParam(string processorParam, string processorParamValue)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamProcessorParam);
                Writer.Write(processorParam);
                Writer.Write(processorParamValue);
                Writer.Flush();
            }
        }

        public Task<ProxyItem> AddItemAsync(IProxyLogger logger, string originalPath, string destinationPath
            , bool isCopy
            )
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskAddItem pipelineAsyncTask = new PipelineAsyncTaskAddItem(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.AddItem);
                WriteGuid(contextGuid);
                Writer.Write(originalPath);
                WriteString(destinationPath);
                Writer.Write(isCopy);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public Task CleanItemsAsync(IProxyLogger logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.CleanItems);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public Task BuildBeginAsync(IProxyLogger logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BuildBegin);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        public Task<bool> BuildAsync(IProxyLogger logger, ProxyItem proxyItem)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskBuild pipelineAsyncTask = new PipelineAsyncTaskBuild(contextGuid, logger, proxyItem);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.Build);
                WriteGuid(contextGuid);
                WriteGuid(proxyItem.Guid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        private void BuildState(Guid guid)
        {
            PipelineAsyncTaskBase task = _tasks[guid];
            BuildState buildSTate = (BuildState)Reader.ReadInt32();

            PipelineAsyncTaskBuild buildTask = (PipelineAsyncTaskBuild)task;
            buildTask.Item.State = buildSTate;
        }

        public Task BuildEndAsync(IProxyLogger logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BuildEnd);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        private void LogError(Guid guid)
        {
            PipelineAsyncTaskBase task = _tasks[guid];
            IProxyLogger logger = task.Logger;

            string filename = ReadString();
            ContentIdentity contentIdentity = ReadContentIdentity();
            string message = ReadString();
            logger.LogError(filename, contentIdentity, message);
        }

        private void LogWarning(Guid guid)
        {
            PipelineAsyncTaskBase task = _tasks[guid];
            IProxyLogger logger = task.Logger;

            string filename = ReadString();
            string helpLink = ReadString();
            ContentIdentity contentIdentity = ReadContentIdentity();
            string message = ReadString();
            logger.LogWarning(filename, helpLink, contentIdentity, message);
        }

        private void LogImportantMessage(Guid guid)
        {
            PipelineAsyncTaskBase task = _tasks[guid];
            IProxyLogger logger = task.Logger;

            string filename = ReadString();
            string message = ReadString();
            logger.LogImportantMessage(filename, message);
        }

        private void LogMessage(Guid guid)
        {
            PipelineAsyncTaskBase task = _tasks[guid];
            IProxyLogger logger = task.Logger;

            string filename = ReadString();
            string message = ReadString();
            logger.LogMessage(filename, message);
        }


        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                // dispose managed state (managed objects)
            }

            Terminate();

            base.Dispose(disposing);
        }

    }
}
