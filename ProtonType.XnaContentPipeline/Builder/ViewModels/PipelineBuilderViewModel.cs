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

        private readonly ObservableCollection<PipelineBuildItemViewModel> _queuedItems =  new ObservableCollection<PipelineBuildItemViewModel>();

        public IEnumerable<PipelineBuildItemViewModel> BuildItems
        {
            get
            {
                IEnumerable<PipelineBuildItemViewModel> filtered 
                    = _buildItems.Where((item) => (IsQueuedSelected && item.Status    == PipelineBuildItemStatus.Queued)
                                               || (IsBuildingSelected && item.Status  == PipelineBuildItemStatus.Building)
                                               || (IsProcessedSelected && item.Status == PipelineBuildItemStatus.Build)
                                               || (IsFailedSelected && item.Status    == PipelineBuildItemStatus.Failed)
                                               );
                return filtered;
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

        bool _isQueuedSelected;
        bool _isBuildingSelected;
        bool _isProcessedSelected;
        bool _isFailedSelected;

        public bool IsQueuedSelected
        {
            get { return _isQueuedSelected; }
            set
            {
                if (_isQueuedSelected != value)
                {
                    _isQueuedSelected = value;
                    RaisePropertyChanged(() => IsQueuedSelected);
                    RaisePropertyChanged(() => BuildItems);
                }
            }
        }
        public bool IsBuildingSelected
        {
            get { return _isBuildingSelected; }
            set
            {
                if (_isBuildingSelected != value)
                {
                    _isBuildingSelected = value;
                    RaisePropertyChanged(() => IsBuildingSelected);
                    RaisePropertyChanged(() => BuildItems);
                }
            }
        }
        public bool IsProcessedSelected
        {
            get { return _isProcessedSelected; }
            set
            {
                if (_isProcessedSelected != value)
                {
                    _isProcessedSelected = value;
                    RaisePropertyChanged(() => IsProcessedSelected);
                    RaisePropertyChanged(() => BuildItems);
                }
            }
        }
        public bool IsFailedSelected
        {
            get { return _isFailedSelected; }
            set
            {
                if (_isFailedSelected != value)
                {
                    _isFailedSelected = value;
                    RaisePropertyChanged(() => IsFailedSelected);
                    RaisePropertyChanged(() => BuildItems);
                }
            }
        }

        int _queuedCount;
        int _buildingCount;
        int _processedCount;
        int _failedCount;

        public int QueuedCount
        {
            get { return _queuedCount; }
            set
            {
                if (_queuedCount != value)
                {
                    _queuedCount = value;
                    RaisePropertyChanged(() => QueuedCount);
                }
            }
        }
        public int BuildingCount
        {
            get { return _buildingCount; }
            set
            {
                if (_buildingCount != value)
                {
                    _buildingCount = value;
                    RaisePropertyChanged(() => BuildingCount);
                }
            }
        }
        public int ProcessedCount
        {
            get { return _processedCount; }
            set
            {
                if (_processedCount != value)
                {
                    _processedCount = value;
                    RaisePropertyChanged(() => ProcessedCount);
                }
            }
        }
        public int FailedCount
        {
            get { return _failedCount; }
            set
            {
                if (_failedCount != value)
                {
                    _failedCount = value;
                    RaisePropertyChanged(() => FailedCount);
                }
            }
        }



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
            _pipelineBuilder.BuildEnded += _pipelineBuilder_BuildEnded;

            IsQueuedSelected = true;
            IsBuildingSelected = true;
            IsProcessedSelected = true;
            IsFailedSelected = true;
        }

        void _pipelineBuilder_BuildStarted(object sender, EventArgs e)
        {
            QueuedCount = _queuedItems.Count;
            BuildingCount = 0;
            ProcessedCount = 0;
            FailedCount = 0;
        }

        void _pipelineBuilder_BuildQueueAdded(object sender, PipelineProjectBuilder.PipelineBuildItemEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var builditemVM = new PipelineBuildItemViewModel(e.BuildItem);
                _buildItems.Add(builditemVM);
                _queuedItems.Add(builditemVM);
                QueuedCount = _queuedItems.Count;
                RaisePropertyChanged(() => BuildItems);
            }));
        }

        void _pipelineBuilder_BuildQueueRemoved(object sender, PipelineProjectBuilder.PipelineBuildItemEventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                var buildItemVM = _queuedItems.FirstOrDefault<PipelineBuildItemViewModel>((i) => { return (i.Builditem == e.BuildItem); });
                _queuedItems.Remove(buildItemVM);
                QueuedCount = _queuedItems.Count;
                BuildingCount++;
                RaisePropertyChanged(() => BuildItems);

            }));
        }

        void _pipelineBuilder_PipelineItemBuildCompleted(object sender, PipelineProjectBuilder.PipelineBuildItemCompletedEventArgs e)
        {
            var buildItem = e.BuildItem;

            switch (buildItem.Status)
            {
                case PipelineBuildItemStatus.Build:
                    this.Dispatcher.Invoke(() =>
                    {
                        QueuedCount = _queuedItems.Count;
                        BuildingCount--;
                        ProcessedCount++;
                        RaisePropertyChanged(() => BuildItems);
                    });
                    break;

                case PipelineBuildItemStatus.Failed:
                    this.Dispatcher.Invoke(() =>
                    {
                        QueuedCount = _queuedItems.Count;
                        BuildingCount--;
                        FailedCount++;
                        RaisePropertyChanged(() => BuildItems);
                    });
                    break;

                default:
                    break;
            }

            var pipelineItemVM = FindPipelineItemVM(buildItem.PipelineItem);
            OnPipelineItemBuildCompleted(new PipelineItemViewModelBuildCompletedEventArgs(pipelineItemVM, e.Result));
        }

        void _pipelineBuilder_BuildEnded(object sender, EventArgs e)
        {
            OnBuildEnded(EventArgs.Empty);
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

            List<PipelineItem> buildItems = items;

            _pipelineBuilder.BuildItems(items, buildItems, rebuild);
        }

        public void BuildItem(PipelineItemViewModel buildPipelineItemVM, bool rebuild)
        {
            List<PipelineItem> items = new List<PipelineItem>();
            foreach (var pipelineItemVM in _projectVM.PipelineItemsVM)
                items.Add(pipelineItemVM.PipelineItem);

            List<PipelineItem> buildItems = new List<PipelineItem>();
            buildItems.Add(buildPipelineItemVM.PipelineItem);

            _pipelineBuilder.BuildItems(items, buildItems, rebuild);
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
        public event EventHandler<EventArgs> BuildEnded;

        private void OnPipelineItemBuildCompleted(PipelineItemViewModelBuildCompletedEventArgs e)
        {
            var handler = PipelineItemBuildCompleted;
            if (handler != null)
                handler(this, e);
        }
        private void OnBuildEnded(EventArgs e)
        {
            var handler = BuildEnded;
            if (handler != null)
                handler(this, e);
        }
    }
}
