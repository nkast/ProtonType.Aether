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

        internal PipelineBuildItem BuildItem(PipelineItem pipelineItem, bool rebuild)
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
         
            ProcessBuildQueue();

            return buildItem;
        }

        internal void ProcessBuildQueue()
        {
            lock (_buildTaskLocker)
            {
                if (_buildTask == null)
                    BeginBuildTask();
            }

            return;
        }

        private void BeginBuildTask()
        {
            if (_buildTask != null)
                throw new InvalidOperationException("_buildTask is not null");

            _buildTask = Task.Factory.StartNew(ProcessBuildQueueWorker);
            _buildTask.ContinueWith((t) => StopBuildTask() );
            OnBuildStarted(EventArgs.Empty);
        }

        private void StopBuildTask()
        {
            lock (_buildTaskLocker)
            {
                _buildTask = null;
            }
        }

        private void ProcessBuildQueueWorker()
        {
            Thread.CurrentThread.Name = "ProcessBuildQueueWorker";

            var pipelineProxy = new PipelineProxyClient();
            var logger = new ProxyLogger(_viewLogger);

            pipelineProxy.SetBaseDirectory(this._project.Location);
                                
            pipelineProxy.SetRebuild();
            pipelineProxy.SetIncremental();

            // Set Global Settings
            pipelineProxy.SetOutputDir(_project.OutputDir);
            pipelineProxy.SetIntermediateDir(_project.IntermediateDir);
            pipelineProxy.SetPlatform(_project.Platform);
            pipelineProxy.SetConfig(_project.Config);
            pipelineProxy.SetProfile(_project.Profile);
            pipelineProxy.SetCompress(_project.Compress);

            List<PipelineAsyncTask> tasks = new List<PipelineAsyncTask>();

            foreach (string assemblyPath in _project.References)
            {
                var task = pipelineProxy.AddAssembly(logger, assemblyPath);
                tasks.Add(task);
            }

            foreach (var task in tasks)
                task.AsyncWaitHandle.WaitOne();
            

            PipelineBuildItem buildItem;
            while (_queuedItems.TryDequeue(out buildItem))
            {
                OnBuildQueueItemRemoved(new PipelineBuildItemEventArgs(buildItem));

                var buildTask = ProcessbuildItem(pipelineProxy, _project, buildItem);
                buildTask.AsyncWaitHandle.WaitOne();

                switch (buildTask.Result)
                {
                    case TaskResult.FAILED:
                        buildItem.Status = PipelineBuildItemStatus.Failed;
                        break;
                    case TaskResult.SUCCEEDED:
                        buildItem.Status = PipelineBuildItemStatus.Build;
                        break;
                }
                
                OnPipelineItemBuildCompleted(new PipelineBuildItemCompletedEventArgs(buildItem, buildTask.Result));
            }

            pipelineProxy.Dispose();

            return;
        }

        private PipelineAsyncTask ProcessbuildItem(PipelineProxyClient pipelineProxy, PipelineProject project, PipelineBuildItem buildItem)
        {
            var item = buildItem.PipelineItem;

            pipelineProxy.SetImporter(item.Importer);

            pipelineProxy.SetProcessor(item.Processor);
            foreach (var processorParamName in item.ProcessorParams.Keys)
            {
                string processorParamValue = item.ProcessorParams[processorParamName];
                pipelineProxy.AddProcessorParam(processorParamName, processorParamValue);
            }
            
            var originalPath = item.OriginalPath;
            var destinationPath = item.DestinationPath;
            if (destinationPath == originalPath)
                destinationPath = null;

            var logger = new ProxyLogger(_viewLogger);

            if (item.BuildAction == BuildAction.Copy)
            {
                var buildTask = pipelineProxy.Copy(logger, originalPath, destinationPath);
                return buildTask;
            }
            if (item.BuildAction == BuildAction.Build)
            {
                var buildTask = pipelineProxy.Build(logger, originalPath, destinationPath);
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
            var intermediatePath = _project.ReplaceSymbols(_project.IntermediateDir);
            if (!Path.IsPathRooted(intermediatePath))
                intermediatePath = PathHelper.Normalize(Path.GetFullPath(Path.Combine(_project.Location, intermediatePath)));

            var sourceName = Path.GetFileNameWithoutExtension(pipelineItem.Filename);
            var contentFile = Path.Combine(intermediatePath, pipelineItem.Location, sourceName + PipelineBuildEvent.Extension);
            contentFile = contentFile.Replace("\\", "/");
            var pipelineBuildEvent = PipelineBuildEvent.Load(contentFile);

            if (pipelineBuildEvent != null)
            {
                // Remove event file (.mgcontent file) from intermediate folder.
                if (File.Exists(pipelineBuildEvent.DestFile))
                    File.Delete(pipelineBuildEvent.DestFile);

                //TODO: delete external assets. Make sure external assets are not used by other assets.
                /*
                // Recursively clean additional (nested) assets.
                foreach (var asset in pipelineBuildEvent.BuildAsset)
                {
                    string assetEventFilepath;
                    var assetCachedEvent = LoadBuildEvent(asset, out assetEventFilepath);

                    if (assetCachedEvent == null)
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
                foreach (var asset in cachedEvent.BuildOutput)
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

        private PipelineBuildEvent LoadBuildEvent(string destFile, out string eventFilepath)
        {
            var contentPath = Path.ChangeExtension(PathHelper.GetRelativePath(_project.OutputDir, destFile), PipelineBuildEvent.Extension);
            eventFilepath = Path.Combine(_project.IntermediateDir, contentPath);
            return PipelineBuildEvent.Load(eventFilepath);
        }
    }
}
