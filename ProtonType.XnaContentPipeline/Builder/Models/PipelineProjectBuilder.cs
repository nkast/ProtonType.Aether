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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.Builder.Models
{
    public enum PipelineBuildItemStatus
    {
        Queued,
        Building,

        Build,
        Failed,
    }

    internal class PipelineProjectBuilder
    {

        internal class PipelineBuildItem
        {
            internal ProxyItem ProxyItem;
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

            public PipelineBuildItem(ProxyItem proxyItem, PipelineItem pipelineItem, bool rebuild)
            {
                this.ProxyItem = proxyItem;
                this.PipelineItem = pipelineItem;
                this.Rebuild = rebuild;

                proxyItem.StateChanged += ProxyItem_OnStateChanged;
            }

            private void ProxyItem_OnStateChanged(object sender, EventArgs e)
            {
                ProxyItem proxyItem = (ProxyItem)sender;
                BuildState buildState = proxyItem.State;

                switch (buildState)
                {
                    case BuildState.Building:
                    case BuildState.Importing:
                    case BuildState.Processing:
                    case BuildState.Writing:
                    case BuildState.Copying:
                    case BuildState.Skipping:
                        this.Status = PipelineBuildItemStatus.Building;
                        break;

                    default:
                        break;
                }
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
            public readonly bool Result;

            public PipelineBuildItemCompletedEventArgs(PipelineBuildItem buildItem, bool taskResult) : base(buildItem)
            {
                this.Result = taskResult;
            }
        }

        public event EventHandler<EventArgs> BuildStarted;
        public event EventHandler<PipelineBuildItemEventArgs> BuildQueueItemAdded;
        public event EventHandler<PipelineBuildItemEventArgs> BuildQueueItemRemoved;
        public event EventHandler<PipelineBuildItemCompletedEventArgs> PipelineItemBuildCompleted;
        public event EventHandler<EventArgs> BuildEnded;

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
        protected virtual void OnBuildEnded(EventArgs e)
        {
            var handler = BuildEnded;
            if (handler == null) return;
            handler(this, e);
        }

        internal void BuildItems(string documentFile, List<PipelineItem> pipelineItems, List<PipelineItem> buildPipelineItems, bool rebuild)
        {
            _buildItems.Clear();

            string projectName = Path.GetFileNameWithoutExtension(documentFile);
            string location = documentFile;
            if (string.IsNullOrEmpty(location))
                location = "";
            else
                location = Path.GetDirectoryName(location);

            PipelineProxyClient pipelineProxy = InitProxy(this._project, projectName, location);

            ((IPipelineBuilder)pipelineProxy).SetRebuild();
            ((IPipelineBuilder)pipelineProxy).SetIncremental();

            ProxyLogger logger = new ProxyLogger(_viewLogger);

            {
                List<Task> addPackageTasks = new List<Task>();
                foreach (Package package in _project.PackageReferences)
                {
                    Task task = ((IPipelineBuilder)pipelineProxy).AddPackageAsync(logger, package);
                    addPackageTasks.Add(task);
                }
                Task.WaitAll(addPackageTasks.ToArray());

                Task resolvePackagesTask = ((IPipelineBuilder)pipelineProxy).ResolvePackagesAsync(logger);
                resolvePackagesTask.Wait();

                // load all types from references
                List<Task> addAssemblyTasks = new List<Task>();
                foreach (string assemblyPath in _project.References)
                {
                    Task task = ((IPipelineBuilder)pipelineProxy).AddAssemblyAsync(logger, assemblyPath);
                    addAssemblyTasks.Add(task);
                }
                Task.WaitAll(addAssemblyTasks.ToArray());
            }

            lock (_buildTaskLocker)
            {
                foreach (PipelineItem pipelineItem in pipelineItems)
                {
                    ((IPipelineBuilder)pipelineProxy).SetImporter(pipelineItem.Importer);

                    ((IPipelineBuilder)pipelineProxy).SetProcessor(pipelineItem.Processor);
                    foreach (var processorParamName in pipelineItem.ProcessorParams.Keys)
                    {
                        string processorParamValue = pipelineItem.ProcessorParams[processorParamName];
                        ((IPipelineBuilder)pipelineProxy).AddProcessorParam(processorParamName, processorParamValue);
                    }

                    string originalPath = pipelineItem.OriginalPath;
                    string destinationPath = pipelineItem.DestinationPath;
                    if (destinationPath == originalPath)
                        destinationPath = null;

                    IProxyLogger addItemlogger = new ProxyLogger(_viewLogger);

                    Task<ProxyItem> addItemTask = pipelineProxy.AddItemAsync(addItemlogger, originalPath, destinationPath
                        , isCopy: (pipelineItem.BuildAction == BuildAction.Copy)
                    );

                    addItemTask.Wait();
                    ProxyItem proxyItem = addItemTask.Result;

                    PipelineBuildItem buildItem = new PipelineBuildItem(proxyItem, pipelineItem, rebuild);

                    _buildItems.Add(buildItem);
                    _queuedItems.Enqueue(buildItem);

                    OnBuildQueueItemAdded(new PipelineBuildItemEventArgs(buildItem));
                }

                // Before building the content, register all files to be built.
                // (Necessary to correctly resolve external references.)
                Task buildBeginTask = ((IPipelineBuilder)pipelineProxy).BuildBeginAsync(logger);
                buildBeginTask.Wait();

                if (_buildTask != null)
                    throw new InvalidOperationException("_buildTask is not null");

                _buildTask = Task.Run(() =>
                {
                    if (Thread.CurrentThread.Name == null)
                        Thread.CurrentThread.Name = "ProcessBuildQueueWorker";

                    int maxConcurrentTasks = Environment.ProcessorCount;

                    List<Task> buildTasks = new List<Task>();
                    while (_queuedItems.Count > 0 || buildTasks.Count > 0)
                    {
                        while (_queuedItems.Count > 0 && buildTasks.Count < maxConcurrentTasks)
                        {
                            if (_queuedItems.TryDequeue(out PipelineBuildItem buildItem))
                            {
                                OnBuildQueueItemRemoved(new PipelineBuildItemEventArgs(buildItem));
                                IProxyLogger itemLogger = new ProxyLogger(_viewLogger);
                                Task buildTask = pipelineProxy.BuildAsync(itemLogger, buildItem.ProxyItem);
                                buildTask.ContinueWith((task) =>
                                {
                                    bool result = (buildItem.ProxyItem.State != BuildState.Failed);

                                    switch (result)
                                    {
                                        case true:
                                            buildItem.Status = PipelineBuildItemStatus.Build;
                                            break;
                                        case false:
                                            buildItem.Status = PipelineBuildItemStatus.Failed;
                                            break;
                                    }

                                    OnPipelineItemBuildCompleted(new PipelineBuildItemCompletedEventArgs(buildItem, result));
                                });
                                buildTasks.Add(buildTask);
                            }
                        }
                        Task.WaitAny(buildTasks.ToArray());

                        // Remove completed tasks.
                        for (int i = buildTasks.Count - 1; i >= 0; i--)
                        {
                            Task task = buildTasks[i];
                            if (task.IsCompleted || task.IsCanceled || task.IsFaulted)
                            {
                                buildTasks.RemoveAt(i);
                            }
                        }
                    }
                }).ContinueWith((t) =>
                    {
                        Task buildEndTask = ((IPipelineBuilder)pipelineProxy).BuildEndAsync(logger);
                        buildEndTask.Wait();
                        
                        lock (_buildTaskLocker)
                        {
                            pipelineProxy.Dispose();
                            _buildTask = null;
                            OnBuildEnded(EventArgs.Empty);
                        }
                    });
                OnBuildStarted(EventArgs.Empty);
            }

            return;
        }

        private static PipelineProxyClient InitProxy(PipelineProject project, string projectName, string location)
        {
            PipelineProxyClient pipelineProxy = new PipelineProxyClient();
            pipelineProxy.BeginListening();

            ((IPipelineBuilder)pipelineProxy).SetBaseDirectory(location);
            ((IPipelineBuilder)pipelineProxy).SetProjectName(projectName);

            ContentCompression compression = ContentCompression.Uncompressed;
            if (project.Compress)
                compression = PipelineProject.ToContentCompression(project.Compression);

            // Set Global Settings
            ((IPipelineBuilder)pipelineProxy).SetOutputDir(project.OutputDir);
            ((IPipelineBuilder)pipelineProxy).SetIntermediateDir(project.IntermediateDir);
            ((IPipelineBuilder)pipelineProxy).SetPlatform(project.Platform);
            if (project.Config != null)
                ((IPipelineBuilder)pipelineProxy).SetConfig(project.Config);
            ((IPipelineBuilder)pipelineProxy).SetProfile(project.Profile);
            ((IPipelineBuilder)pipelineProxy).SetCompression(compression);

            return pipelineProxy;
        }

        public void CancelBuild()
        {

                _viewLogger.LogMessage("Build terminated!");
        }

        public void CleanAll(string documentFile)
        {
            string commands = string.Format("/clean /intermediateDir:\"{0}\" /outputDir:\"{1}\"", _project.IntermediateDir, _project.OutputDir);

            string projectName = Path.GetFileNameWithoutExtension(documentFile);
            string location = documentFile;
            if (string.IsNullOrEmpty(location))
                location = "";
            else
                location = Path.GetDirectoryName(location);

            PipelineProxyClient pipelineProxy = InitProxy(this._project, projectName, location);

            //pipelineProxy.SetClean();

            ProxyLogger logger = new ProxyLogger(_viewLogger);

            lock (_buildTaskLocker)
            {
                Task cleanItemsTask = ((IPipelineBuilder)pipelineProxy).CleanItemsAsync(logger);
                cleanItemsTask.ContinueWith((task) =>
                {
                    lock (_buildTaskLocker)
                    {
                        pipelineProxy.Dispose();
                        OnBuildEnded(EventArgs.Empty);
                    }
                });
            }

            return;
        }

        internal void CleanItem(PipelineItem pipelineItem)
        {
            // TODO: implement CleanItem

        }

    }
}
