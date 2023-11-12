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
using System.Collections.ObjectModel;
using System.Linq;
using nkast.ProtonType.Framework.Attributes;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Builder.Models;
using nkast.ProtonType.XnaContentPipeline.Builder.Views;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.Builder.ViewModels
{
    [DefaultView(typeof(PipelineBuilderView), "Pipeline Builder")]
    public class PipelineBuilderViewModel : DocumentViewModel
    {
        private PipelineProjectBuilder _pipelineBuilder;
        private PipelineBuilderStatusBarItemViewModel _pipelineBuilderStatusBarItemViewModel;
        public PipelineBuilderStatusBarItemViewModel PipelineBuilderStatusBarItemViewModel { get { return _pipelineBuilderStatusBarItemViewModel; } }

        readonly private PipelineProjectViewModel _projectVM;
        private IPipelineLogger _viewLogger;


        private readonly ObservableCollection<PipelineBuildItemViewModel> _buildItems =  new ObservableCollection<PipelineBuildItemViewModel>();
        public ReadOnlyObservableCollection<PipelineBuildItemViewModel> _readOnlyBuildItems;
        public IList<PipelineBuildItemViewModel> BuildItems
        {
            get
            {
                if (_readOnlyBuildItems == null)
                    _readOnlyBuildItems = new ReadOnlyObservableCollection<PipelineBuildItemViewModel>(_buildItems);
                return _readOnlyBuildItems;
            }
        }

        public string FilterName { get; set; }

        public string ProjectName 
        { 
            get { return _projectVM.ProjectName; }
        }
        public string OriginalPath
        {
            get { return _projectVM.Project.OriginalPath; }
        }

        public int QueuedCount
        {
            get { return _buildItems.Count; }
        }

        public int ProcessedCount { get; private set; }
        public int FailedCount { get; private set; }


        public PipelineBuilderViewModel(PipelineProjectViewModel projectVM, IPipelineLogger viewLogger)
            : base(null, "Pipeline Builder")
        {
            this._projectVM = projectVM;
            this._viewLogger = viewLogger;
            this._pipelineBuilder = new PipelineProjectBuilder(projectVM.Project, _viewLogger);
            
            //create statusbar
            _pipelineBuilderStatusBarItemViewModel = new PipelineBuilderStatusBarItemViewModel(this);
            object content = new PipelineBuilderStatusBarItemView();
            _pipelineBuilderStatusBarItemViewModel.Content = content;

            _pipelineBuilder.BuildStarted += _pipelineBuilder_BuildStarted;
            _pipelineBuilder.BuildQueueItemAdded += _pipelineBuilder_BuildQueueAdded;
            _pipelineBuilder.BuildQueueItemRemoved += _pipelineBuilder_BuildQueueRemoved;
            _pipelineBuilder.PipelineItemBuildCompleted += _pipelineBuilder_PipelineItemBuildCompleted;
        }

        void _pipelineBuilder_BuildStarted(object sender, EventArgs e)
        {
            ProcessedCount = 0;
            FailedCount = 0;
            RaisePropertyChanged(()=>ProcessedCount);
            RaisePropertyChanged(() => FailedCount);
        }

        void _pipelineBuilder_BuildQueueAdded(object sender, PipelineProjectBuilder.PipelineBuildItemEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var builditemVM = new PipelineBuildItemViewModel(e.BuildItem);
                _buildItems.Add(builditemVM);
                RaisePropertyChanged(() => QueuedCount);
            }));
        }

        void _pipelineBuilder_BuildQueueRemoved(object sender, PipelineProjectBuilder.PipelineBuildItemEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var buildItemVM = _buildItems.FirstOrDefault<PipelineBuildItemViewModel>((i) => { return (i.Builditem == e.BuildItem); });
                _buildItems.Remove(buildItemVM);
                RaisePropertyChanged(() => QueuedCount);
            }));
        }

        void _pipelineBuilder_PipelineItemBuildCompleted(object sender, PipelineProjectBuilder.PipelineBuildItemCompletedEventArgs e)
        {
            var buildItem = e.BuildItem;

            // Find item view model
            var pipelineItemVM = FindPipelineItemVM(buildItem.PipelineItem);


            if (e.Result == Common.TaskResult.SUCCEEDED)
            {
                ProcessedCount++;
                RaisePropertyChanged(() => ProcessedCount);
            }
            if (e.Result == Common.TaskResult.FAILED)
            {
                FailedCount++;
                RaisePropertyChanged( ()=> FailedCount);
            }

            OnPipelineItemBuildCompleted(new PipelineItemViewModelBuildCompletedEventArgs(pipelineItemVM, e.Result));
        }

        private PipelineItemViewModel FindPipelineItemVM(nkast.ProtonType.XnaContentPipeline.Common.PipelineItem pipelineItem)
        {
            foreach (var plItemVM in _projectVM.PipelineItemsVM)
            {
                if (pipelineItem == plItemVM.PipelineItem)
                {
                    return plItemVM;
                }
            }

            return null;
        }

        public void BuildAll(bool rebuild)
        {
            List<PipelineItem> items = new List<PipelineItem>();
            foreach (var pipelineItemVM in _projectVM.PipelineItemsVM)
                items.Add(pipelineItemVM.PipelineItem);

            _pipelineBuilder.BuildAll(items, rebuild);

        }

        public void BuildItem(PipelineItemViewModel pipelineItemVM, bool rebuild)
        {
            _pipelineBuilder.BuildItem(pipelineItemVM.PipelineItem, rebuild);
        }

        public void CleanAll()
        {
            _pipelineBuilder.CleanAll();
        }

        public void CleanItem(PipelineItemViewModel pipelineItemVM)
        {
            _pipelineBuilder.CleanItem(pipelineItemVM.PipelineItem);
        }


        public event EventHandler<PipelineItemViewModelBuildCompletedEventArgs> PipelineItemBuildCompleted;

        private void OnPipelineItemBuildCompleted(PipelineItemViewModelBuildCompletedEventArgs e)
        {
            var handler = PipelineItemBuildCompleted;
            if (handler != null)
                handler(this, e);
        }
    }
}
