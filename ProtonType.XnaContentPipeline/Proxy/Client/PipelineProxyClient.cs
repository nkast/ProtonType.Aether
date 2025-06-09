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
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaContentPipeline.ProxyClient
{
    public partial class PipelineProxyClient : IPCClient
    {
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


        public Task<ProxyItem> AddItemAsync(IProxyLoggerBase logger, string originalPath, string destinationPath
            , bool isCopy
            )
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskAddItem pipelineAsyncTask = new PipelineAsyncTaskAddItem(contextGuid, (IProxyLogger)logger);
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


        public Task BuildAsync(IProxyLoggerBase logger, ProxyItem proxyItem)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskBuild pipelineAsyncTask = new PipelineAsyncTaskBuild(contextGuid, (IProxyLogger)logger, proxyItem);
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

            string message = ReadString();
            logger.LogImportantMessage(message);
        }

        private void LogMessage(Guid guid)
        {
            PipelineAsyncTaskBase task = _tasks[guid];
            IProxyLogger logger = task.Logger;

            string message = ReadString();
            logger.LogMessage(message);
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
