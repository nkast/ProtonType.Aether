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
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.ProxyClient
{
    public class PipelineProxyClient : IPCClient
    {
        ConcurrentDictionary<Guid, PipelineAsyncTask> _tasks = new ConcurrentDictionary<Guid, PipelineAsyncTask>();

        public PipelineProxyClient() : base("ProtonType.XnaContentPipeline.ProxyServer.exe")
        {
            return;
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

        public PipelineAsyncTask AddAssembly(IProxyLogger logger, string assemblyPath)
        {
            var contextGuid = Guid.NewGuid();
            _tasks[contextGuid] = new PipelineAsyncTask(contextGuid, logger);

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.AddAssembly);
                WriteGuid(contextGuid);
                Writer.Write(assemblyPath);
                Writer.Flush();
            }

            for (ProxyMsgType msg = ReadMsg(); ; msg = ReadMsg())
            {
                switch (msg)
                {
                    case ProxyMsgType.LogMessage:
                        {
                            var guid = ReadGuid();
                            LogMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogImportantMessage:
                        {
                            var guid = ReadGuid();
                            LogImportantMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogWarning:
                        {
                            var guid = ReadGuid();
                            LogWarning(guid);
                        }
                        break;

                    case ProxyMsgType.LogError:
                        {
                            var guid = ReadGuid();
                            LogError(guid);
                        }
                        break;

                    case ProxyMsgType.TaskEnd:
                        {
                            var guid = ReadGuid();
                            var taskResult = ReadTaskResult();
                            var task = _tasks[guid];
                            _tasks.TryRemove(guid, out task);
                            task.OnCompleted(taskResult);
                            return task;
                        }

                    default:
                        throw new Exception("Unknown Message");
                }
            }
        }
        
        public IEnumerable<ImporterDescription> GetImporters()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.GetImporters);
                Writer.Flush();
            }

            List<ImporterDescription> importers = new List<ImporterDescription>();
            for (ImporterDescription importer; (importer = ReadImporter()) != null; )
            {
                importers.Add(importer);
            }
            return importers;
        }

        public IEnumerable<ProcessorDescription> GetProcessors()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.GetProcessors);
                Writer.Flush();
            }

            List<ProcessorDescription> processors = new List<ProcessorDescription>();
            for (ProcessorDescription processor; (processor = ReadProcessor()) != null; )
            {
                processors.Add(processor);
            }
            return processors;
        }

        private ImporterDescription ReadImporter()
        {
            var msg = ReadMsg();
            if (msg == ProxyMsgType.End)
                return null;
            if (msg != ProxyMsgType.Importer)
                throw new InvalidOperationException();

            ImporterDescription importer = new ImporterDescription(Reader);
            return importer;
        }

        private ProcessorDescription ReadProcessor()
        {
            var msg = ReadMsg();
            if (msg == ProxyMsgType.End)
                return null;
            if (msg != ProxyMsgType.Processor)
                throw new InvalidOperationException();

            var processor = new ProcessorDescription(Reader);
            return processor;
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
        public void SetCompress(bool compress)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamCompress);
                Writer.Write(compress);
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

        public PipelineAsyncTask Copy(IProxyLogger logger, string originalPath, string destinationPath)
        {
            var contextGuid = Guid.NewGuid();
            _tasks[contextGuid] = new PipelineAsyncTask(contextGuid, logger);

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.Copy);
                WriteGuid(contextGuid);
                Writer.Write(originalPath);
                WriteString(destinationPath);
                Writer.Flush();
            }

            for (ProxyMsgType msg = ReadMsg(); ; msg = ReadMsg())
            {
                switch (msg)
                {
                    case ProxyMsgType.LogMessage:
                        {
                            var guid = ReadGuid();
                            LogMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogImportantMessage:
                        {
                            var guid = ReadGuid();
                            LogImportantMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogWarning:
                        {
                            var guid = ReadGuid();
                            LogWarning(guid);
                        }
                        break;

                    case ProxyMsgType.LogError:
                        {
                            var guid = ReadGuid();
                            LogError(guid);
                        }
                        break;

                    case ProxyMsgType.TaskEnd:
                        {
                            var guid = ReadGuid();
                            var taskResult = ReadTaskResult();
                            var task = _tasks[guid];
                            _tasks.TryRemove(guid, out task);
                            task.OnCompleted(taskResult);
                            return task;
                        }

                    default:
                        throw new Exception("Unknown Message");
                }
            }
        }

        public PipelineAsyncTask Build(IProxyLogger logger, string originalPath, string destinationPath)
        {
            var contextGuid = Guid.NewGuid();
            _tasks[contextGuid] = new PipelineAsyncTask(contextGuid, logger);

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.Build);
                WriteGuid(contextGuid);
                Writer.Write(originalPath);
                WriteString(destinationPath);
                Writer.Flush();
            }

            for (ProxyMsgType msg = ReadMsg(); ; msg = ReadMsg())
            {
                switch (msg)
                {
                    case ProxyMsgType.LogMessage:
                        {
                            var guid = ReadGuid();
                            LogMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogImportantMessage:
                        {
                            var guid = ReadGuid();
                            LogImportantMessage(guid);
                        }
                        break;

                    case ProxyMsgType.LogWarning:
                        {
                            var guid = ReadGuid();
                            LogWarning(guid);
                        }
                        break;

                    case ProxyMsgType.LogError:
                        {
                            var guid = ReadGuid();
                            LogError(guid);
                        }
                        break;

                    case ProxyMsgType.TaskEnd:
                        {
                            var guid = ReadGuid();
                            var taskResult = ReadTaskResult();
                            var task = _tasks[guid];
                            _tasks.TryRemove(guid, out task);
                            task.OnCompleted(taskResult);
                            return task;
                        }

                    default:
                        throw new Exception("Unknown Message");
                }
            }
        }

        private void LogError(Guid guid)
        {
            var task = _tasks[guid];
            var logger = task.Logger;

            var filename = ReadString();
            var contentIdentity = ReadContentIdentity();
            var message = ReadString();
            logger.LogError(filename, contentIdentity, message);
        }

        private void LogWarning(Guid guid)
        {
            var task = _tasks[guid];
            var logger = task.Logger;

            var filename = ReadString();
            var helpLink = ReadString();
            var contentIdentity = ReadContentIdentity();
            var message = ReadString();
            logger.LogWarning(filename, helpLink, contentIdentity, message);
        }

        private void LogImportantMessage(Guid guid)
        {
            var task = _tasks[guid];
            var logger = task.Logger;

            var filename = ReadString();
            var message = ReadString();
            logger.LogImportantMessage(filename, message);
        }

        private void LogMessage(Guid guid)
        {
            var task = _tasks[guid];
            var logger = task.Logger;

            var filename = ReadString();
            var message = ReadString();
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
