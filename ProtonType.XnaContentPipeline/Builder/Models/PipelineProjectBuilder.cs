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
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.Builder.Models
{
    internal class PipelineProjectBuilder
    {
        public enum PipelineBuildItemStatus
        {
            Queued,
            Failed,
            Build
        }

        internal class PipelineBuildItem
        {
            public readonly PipelineItem PipelineItem;
            public readonly bool Rebuild;

            private PipelineBuildItemStatus _status = PipelineBuildItemStatus.Queued;
                        
            public event EventHandler<EventArgs> StatusChanged;

            public PipelineBuildItemStatus Status
            {
                get { return _status; }
                set 
                {
                    if (value == _status) return;
                    _status = value;
                    OnStatusChanged(EventArgs.Empty);
                }
            }
            
            protected virtual void OnStatusChanged(EventArgs e)
            {
                var handler = StatusChanged;
                if (handler == null) return;
                handler(this, e);
            }

            public PipelineBuildItem(PipelineItem pipelineItem, bool rebuild)
            {
                this.PipelineItem = pipelineItem;
                this.Rebuild = rebuild;
            }

        }


        private PipelineProject _project;
        private IPipelineLogger _viewLogger;


        ConcurrentQueue<PipelineBuildItem> _queuedItems = new ConcurrentQueue<PipelineBuildItem>();
        List<PipelineBuildItem> _buildItems = new List<PipelineBuildItem>();



        object _buildTaskLocker = new object();
        Task _buildTask = null;

        public PipelineProjectBuilder(PipelineProject project, IPipelineLogger viewLogger)
        {
            this._project = project;
            this._viewLogger = viewLogger;
        }

        [Serializable]
        public class PipelineBuildItemEventArgs : EventArgs
        {
            public readonly PipelineBuildItem BuildItem;

            public PipelineBuildItemEventArgs(PipelineBuildItem buildItem)
            {
                this.BuildItem = buildItem;
            }
        }

        public class PipelineBuildItemCompletedEventArgs : PipelineBuildItemEventArgs
        {
            public readonly TaskResult Result;

            public PipelineBuildItemCompletedEventArgs(PipelineBuildItem buildItem, TaskResult taskResult) : base(buildItem)
            {
                this.Result = taskResult;
            }
        }

        public event EventHandler<EventArgs> BuildStarted;
        public event EventHandler<PipelineBuildItemEventArgs> BuildQueueItemAdded;
        public event EventHandler<PipelineBuildItemEventArgs> BuildQueueItemRemoved;
        public event EventHandler<PipelineBuildItemCompletedEventArgs> PipelineItemBuildCompleted;

        protected virtual void OnBuildStarted(EventArgs e)
        {
            var handler = BuildStarted;
            if (handler == null) return;
            handler(this, e);
        }
        protected virtual void OnBuildQueueItemAdded(PipelineBuildItemEventArgs e)
        {
            var handler = BuildQueueItemAdded;
            if (handler == null) return;
            handler(this, e);
        }
        protected virtual void OnBuildQueueItemRemoved(PipelineBuildItemEventArgs e)
        {
            var handler = BuildQueueItemRemoved;
            if (handler == null) return;
            handler(this, e);
        }

        private void OnPipelineItemBuildCompleted(PipelineBuildItemCompletedEventArgs e)
        {
            var handler = PipelineItemBuildCompleted;
            if (handler == null) return;
            handler(this, e);
        }

        internal void BuildAll(List<PipelineItem> pipelineItems, bool rebuild)
        {
            _buildItems.Clear();

            PipelineProxyClient pipelineProxy = new PipelineProxyClient();
            {
                pipelineProxy.BeginListening();

                InitProxy(pipelineProxy);

                lock (_buildTaskLocker)
                {
                    if (_buildTask == null)
                    {
                        _buildItems.Clear();
                    }
                    foreach (var pipelineItem in pipelineItems)
                    {
                        var buildItem = new PipelineBuildItem(pipelineItem, rebuild);
                        _buildItems.Add(buildItem);
                        _queuedItems.Enqueue(buildItem);
                        OnBuildQueueItemAdded(new PipelineBuildItemEventArgs(buildItem));
                    }
                }

                lock (_buildTaskLocker)
                {
                    if (_buildTask != null)
                        throw new InvalidOperationException("_buildTask is not null");

                    _buildTask = Task.Factory.StartNew(() => 
                    {
                        foreach (var pipelineItem in pipelineItems)
                        {
                            ProcessBuildQueueWorker(pipelineProxy);
                        }
                    });

                    _buildTask.ContinueWith((t) =>
                    {
                        lock (_buildTaskLocker)
                        {
                            pipelineProxy.Dispose();
                            _buildTask = null;
                        }
                    });
                    OnBuildStarted(EventArgs.Empty);
                }
            }
            return;
        }

        internal void BuildItem(PipelineItem pipelineItem, bool rebuild)
        {
            PipelineProxyClient pipelineProxy = new PipelineProxyClient();
            {
                pipelineProxy.BeginListening();

                InitProxy(pipelineProxy);

                {
                    var buildItem = new PipelineBuildItem(pipelineItem, rebuild);

                    lock (_buildTaskLocker)
                    {
                        if (_buildTask == null)
                        {
                            _buildItems.Clear();
                        }
                        _buildItems.Add(buildItem);
                        _queuedItems.Enqueue(buildItem);
                    }
                    OnBuildQueueItemAdded(new PipelineBuildItemEventArgs(buildItem));

                    lock (_buildTaskLocker)
                    {
                        if (_buildTask != null)
                            throw new InvalidOperationException("_buildTask is not null");

                        _buildTask = Task.Factory.StartNew(() =>
                        {
                            ProcessBuildQueueWorker(pipelineProxy);
                        });
                        _buildTask.ContinueWith((t) =>
                        {
                            lock (_buildTaskLocker)
                            {
                                pipelineProxy.Dispose();
                                _buildTask = null;
                            }
                        });
                        OnBuildStarted(EventArgs.Empty);
                    }
                }
            }
            return;
        }

        private void InitProxy(PipelineProxyClient pipelineProxy)
        {
            pipelineProxy.SetBaseDirectory(this._project.Location);
            pipelineProxy.SetProjectFilename(Path.GetFileName(this._project.OriginalPath));

            pipelineProxy.SetRebuild();
            pipelineProxy.SetIncremental();

            ContentCompression compression = ContentCompression.Uncompressed;
            if (_project.Compress)
            {
                switch (_project.Compression)
                {
                    case CompressionMethod.Default:
                        compression = ContentCompression.LegacyLZ4;
                        break;
                    case CompressionMethod.LZ4:
                        compression = ContentCompression.LZ4;
                        break;
                    case CompressionMethod.Brotli:
                        compression = ContentCompression.Brotli;
                        break;
                    default:
                        throw new InvalidOperationException();
                }
            }

            // Set Global Settings
            pipelineProxy.SetOutputDir(_project.OutputDir);
            pipelineProxy.SetIntermediateDir(_project.IntermediateDir);
            pipelineProxy.SetPlatform(_project.Platform);
            pipelineProxy.SetConfig(_project.Config);
            pipelineProxy.SetProfile(_project.Profile);
            pipelineProxy.SetCompression(compression);

            List<PipelineAsyncTask> tasks = new List<PipelineAsyncTask>();

            ProxyLogger logger = new ProxyLogger(_viewLogger);
            foreach (string assemblyPath in _project.References)
            {
                PipelineAsyncTask task = pipelineProxy.AddAssembly(logger, assemblyPath);
                tasks.Add(task);
            }

            foreach (PipelineAsyncTask task in tasks)
                task.AsyncWaitHandle.WaitOne();
        }

        private void ProcessBuildQueueWorker(PipelineProxyClient pipelineProxy)
        {
            if (Thread.CurrentThread.Name == null)
                Thread.CurrentThread.Name = "ProcessBuildQueueWorker";

            PipelineBuildItem buildItem;
            while (_queuedItems.TryDequeue(out buildItem))
            {
                OnBuildQueueItemRemoved(new PipelineBuildItemEventArgs(buildItem));

                PipelineAsyncTask buildTask = ProcessbuildItem(pipelineProxy, _project, buildItem);
                buildTask.Completed += (sender, e) =>
                {
                    PipelineAsyncTask thisBuildTask = (PipelineAsyncTask)sender;
                    var thisBuildItem = thisBuildTask.AsyncState as PipelineBuildItem;

                    switch (thisBuildTask.Result)
                    {
                        case TaskResult.FAILED:
                            thisBuildItem.Status = PipelineBuildItemStatus.Failed;
                            break;
                        case TaskResult.SUCCEEDED:
                            thisBuildItem.Status = PipelineBuildItemStatus.Build;
                            break;
                    }
                    OnPipelineItemBuildCompleted(new PipelineBuildItemCompletedEventArgs(thisBuildItem, thisBuildTask.Result));
                };
                buildTask.AsyncWaitHandle.WaitOne();
            }

            return;
        }

        private PipelineAsyncTask ProcessbuildItem(PipelineProxyClient pipelineProxy, PipelineProject project, PipelineBuildItem buildItem)
        {
            PipelineItem item = buildItem.PipelineItem;

            pipelineProxy.SetImporter(item.Importer);

            pipelineProxy.SetProcessor(item.Processor);
            foreach (var processorParamName in item.ProcessorParams.Keys)
            {
                string processorParamValue = item.ProcessorParams[processorParamName];
                pipelineProxy.AddProcessorParam(processorParamName, processorParamValue);
            }

            string originalPath = item.OriginalPath;
            string destinationPath = item.DestinationPath;
            if (destinationPath == originalPath)
                destinationPath = null;

            IProxyLogger logger = new ProxyLogger(_viewLogger);

            if (item.BuildAction == BuildAction.Copy)
            {
                PipelineAsyncTask buildTask = pipelineProxy.Copy(logger, originalPath, destinationPath, buildItem);
                return buildTask;
            }
            if (item.BuildAction == BuildAction.Build)
            {
                PipelineAsyncTask buildTask = pipelineProxy.Build(logger, originalPath, destinationPath, buildItem);
                return buildTask;
            }

            return null;
        }

        public void CancelBuild()
        {

                _viewLogger.LogMessage("Build terminated!");
        }

        public void CleanAll()
        {            
            // TODO: implement Clean
            var commands = string.Format("/clean /intermediateDir:\"{0}\" /outputDir:\"{1}\"", _project.IntermediateDir, _project.OutputDir);
        }


        internal void CleanItem(PipelineItem pipelineItem)
        {
            string intermediatePath = _project.ReplaceSymbols(_project.IntermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = PathHelper.Normalize(Path.GetFullPath(Path.Combine(_project.Location, intermediatePath)));

            string projectFilename = Path.GetFileName(_project.OriginalPath);
            string sourceName = Path.GetFileNameWithoutExtension(pipelineItem.Filename);
            string intermediateEventPath = Path.Combine(intermediatePath, Path.GetFileNameWithoutExtension(projectFilename), pipelineItem.Location, sourceName + BuildEvent.Extension);
            intermediateEventPath = intermediateEventPath.Replace("\\", "/");
            intermediateEventPath = Path.GetFullPath(intermediateEventPath);
            BuildEvent buildEvent = BuildEvent.LoadBinary(intermediateEventPath);

            if (buildEvent != null)
            {
                // Remove event file (.mgcontent file) from intermediate folder.
                if (File.Exists(buildEvent.DestFile))
                    File.Delete(buildEvent.DestFile);

                //TODO: delete external assets. Make sure external assets are not used by other assets.
                /*
                // Recursively clean additional (nested) assets.
                foreach (var asset in buildEvent.BuildAsset)
                {
                    string assetEventFilepath;
                    PipelineBuildEvent assetCachedBuildEvent = LoadBuildEvent(asset, out assetEventFilepath);

                    if (assetCachedBuildEvent == null)
                    {
                       // Logger.LogMessage("Cleaning {0}", asset);

                        // Remove asset (.xnb file) from output folder.
                        FileHelper.DeleteIfExists(asset);

                        // Remove event file (.mgcontent file) from intermediate folder.
                        FileHelper.DeleteIfExists(assetEventFilepath);
                        continue;
                    }

                    CleanContent(string.Empty, asset);
                }
                */

            }
        }

        /*
        public void CleanContent(string sourceFilepath, string outputFilepath = null)
        {
            // First try to load the event file.
            ResolveOutputFilepath(sourceFilepath, ref outputFilepath);
            string eventFilepath;
            PipelineBuildEvent cachedBuildEvent = LoadBuildEvent(outputFilepath, out eventFilepath);

            if (cachedBuildEvent != null)
            {
                // Recursively clean additional (nested) assets.
                foreach (var asset in cachedBuildEvent.BuildAsset)
                {
                    string assetEventFilepath;
                    PipelineBuildEvent assetCachedBuildEvent = LoadBuildEvent(asset, out assetEventFilepath);

                    if (assetCachedBuildEvent == null)
                    {
                        //Logger.LogMessage("Cleaning {0}", asset);

                        // Remove asset (.xnb file) from output folder.
                        FileHelper.DeleteIfExists(asset);

                        // Remove event file (.mgcontent file) from intermediate folder.
                        FileHelper.DeleteIfExists(assetEventFilepath);
                        continue;
                    }

                    CleanContent(string.Empty, asset);
                }

                // Remove related output files (non-XNB files) that were copied to the output folder.
                foreach (var asset in cachedBuildEvent.BuildOutput)
                {
                    //Logger.LogMessage("Cleaning {0}", asset);
                    FileHelper.DeleteIfExists(asset);
                }
            }

            //Logger.LogMessage("Cleaning {0}", outputFilepath);

            // Remove asset (.xnb file) from output folder.
            FileHelper.DeleteIfExists(outputFilepath);

            // Remove event file (.mgcontent file) from intermediate folder.
            FileHelper.DeleteIfExists(eventFilepath);
        }
        */

    }
}
