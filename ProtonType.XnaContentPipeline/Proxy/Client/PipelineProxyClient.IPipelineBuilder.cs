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
    public partial class PipelineProxyClient : IPipelineBuilder
    {
        ConcurrentDictionary<Guid, PipelineAsyncTaskBase> _tasks = new ConcurrentDictionary<Guid, PipelineAsyncTaskBase>();

        void IPipelineBuilder.SetBaseDirectory(string baseDirectory)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BaseDirectory);
                Writer.Write(baseDirectory);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.SetProjectName(string projectName)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ProjectName);
                Writer.Write(projectName);
                Writer.Flush();
            }
        }

        Task IPipelineBuilder.AddPackageAsync(IProxyLoggerBase logger, Package package)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, (IProxyLogger)logger);
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

        Task IPipelineBuilder.ResolvePackagesAsync(IProxyLoggerBase logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, (IProxyLogger)logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ResolvePackages);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        Task IPipelineBuilder.AddAssemblyAsync(IProxyLoggerBase logger, string assemblyPath)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, (IProxyLogger)logger);
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
        
        Task<List<ImporterDescription>> IPipelineBuilder.GetImportersAsync(IProxyLoggerBase logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskImporters pipelineAsyncTask = new PipelineAsyncTaskImporters(contextGuid, (IProxyLogger)logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.GetImporters);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        Task<List<ProcessorDescription>> IPipelineBuilder.GetProcessorsAsync(IProxyLoggerBase logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTaskProcessors pipelineAsyncTask = new PipelineAsyncTaskProcessors(contextGuid, (IProxyLogger)logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.GetProcessors);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        void IPipelineBuilder.SetRebuild()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamRebuild);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.SetIncremental()
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamIncremental);
                Writer.Flush();
            }
        }


        void IPipelineBuilder.SetOutputDir(string outputDir)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamOutputDir);
                Writer.Write(outputDir);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.SetIntermediateDir(string intermediateDir)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamIntermediateDir);
                Writer.Write(intermediateDir);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.SetPlatform(TargetPlatform platform)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamPlatform);
                Writer.Write((Int32)platform);
                Writer.Flush();
            }
        }
        void IPipelineBuilder.SetConfig(string config)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamConfig);
                Writer.Write(config);
                Writer.Flush();
            }
        }
        void IPipelineBuilder.SetProfile(GraphicsProfile profile)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamProfile);
                Writer.Write((Int32)profile);
                Writer.Flush();
            }
        }
        void IPipelineBuilder.SetCompression(ContentCompression compression)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamCompression);
                Writer.Write((Int32)compression);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.SetImporter(string importer)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamImporter);
                WriteString(importer);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.SetProcessor(string processor)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamProcessor);
                WriteString(processor);
                Writer.Flush();
            }
        }

        void IPipelineBuilder.AddProcessorParam(string processorParam, string processorParamValue)
        {
            lock (Writer)
            {
                WriteMsg(ProxyMsgType.ParamProcessorParam);
                Writer.Write(processorParam);
                Writer.Write(processorParamValue);
                Writer.Flush();
            }
        }

        Task IPipelineBuilder.CleanItemsAsync(IProxyLoggerBase logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, (IProxyLogger)logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.CleanItems);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        Task IPipelineBuilder.BuildBeginAsync(IProxyLoggerBase logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, (IProxyLogger)logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BuildBegin);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

        Task IPipelineBuilder.BuildEndAsync(IProxyLoggerBase logger)
        {
            Guid contextGuid = Guid.NewGuid();
            PipelineAsyncTask pipelineAsyncTask = new PipelineAsyncTask(contextGuid, (IProxyLogger)logger);
            _tasks[contextGuid] = pipelineAsyncTask;

            lock (Writer)
            {
                WriteMsg(ProxyMsgType.BuildEnd);
                WriteGuid(contextGuid);
                Writer.Flush();
            }

            return pipelineAsyncTask.Task;
        }

    }
}
