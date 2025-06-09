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
using System.IO;
using System.Windows.Data;
using nkast.ProtonType.XnaContentPipeline.Builder.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class PipelineItemViewModelAddedEventArgs : EventArgs
    {
        public readonly PipelineProjectViewModel PipelineProjectVM;
        public readonly PipelineItemViewModel PipelineItemVM;

        public PipelineItemViewModelAddedEventArgs(PipelineProjectViewModel pipelineProjectViewModel, PipelineItemViewModel pipelineItemVM)
        {
            this.PipelineProjectVM = pipelineProjectViewModel;
            this.PipelineItemVM = pipelineItemVM;
        }
    }


    public partial class ContentPipelineViewModel
    {
        internal readonly XnaContentPipelineModule Module;

        private PipelineBuilderViewModel _pipelineBuilder;


        public IValueConverter DragItemConverter { get; set; }
        public PipelineProjectViewModel PipelineProjectViewModel { get; private set; }

        public event EventHandler<PipelineItemViewModelAddedEventArgs> ItemAdded;
        public event EventHandler<PipelineItemViewModelEventArgs> PipelineItemAdded;
        public event EventHandler<PipelineItemViewModelEventArgs> PipelineItemRemoved;
        public event EventHandler<PipelineItemViewModelBuildCompletedEventArgs> PipelineItemBuildCompleted;
        public event EventHandler<EventArgs> BuildEnded;
        

        public ContentPipelineViewModel(XnaContentPipelineModule module)
        {
            this.Module = module;
        }

        ~ContentPipelineViewModel()
        {
        }

        internal void LoadContentProject(string documentFile)
        {
            System.Diagnostics.Debug.Assert(Path.GetExtension(documentFile) == ".mgcb");
            System.Diagnostics.Debug.Assert(Path.IsPathRooted(documentFile));

            PipelineProjectViewModel pipelineProjectVM = new PipelineProjectViewModel(this.Module.Site, this);

            pipelineProjectVM.OpenProject(documentFile);

            this.PipelineProjectViewModel = pipelineProjectVM;

            // UpdateTreeItems()
            if (pipelineProjectVM.Project != null && !string.IsNullOrEmpty(documentFile))
            {
                foreach (var item in pipelineProjectVM.PipelineItemsVM)
                {
                    this.AddItem(item);
                }
            }

            // attach events
            pipelineProjectVM.PipelineItemAdded += pipelineProjectViewModel_PipelineItemAdded;
            pipelineProjectVM.PipelineItemRemoved += pipelineProjectViewModel_PipelineItemRemoved;
        }

        internal void CreateContentProject(string documentFile)
        {
            System.Diagnostics.Debug.Assert(Path.GetExtension(documentFile) == ".mgcb");
            System.Diagnostics.Debug.Assert(Path.IsPathRooted(documentFile));

            PipelineProjectViewModel pipelineProjectVM = new PipelineProjectViewModel(this.Module.Site, this);

            pipelineProjectVM.NewProject(documentFile);
            pipelineProjectVM.SaveProject(true);

            this.PipelineProjectViewModel = pipelineProjectVM;

            // attach events
            pipelineProjectVM.PipelineItemAdded += pipelineProjectViewModel_PipelineItemAdded;
            pipelineProjectVM.PipelineItemRemoved += pipelineProjectViewModel_PipelineItemRemoved;

        }

        internal void CreateBuilder(PipelineProjectViewModel pipelineProjectVM)
        {
            _pipelineBuilder = new PipelineBuilderViewModel(pipelineProjectVM, this);
            _pipelineBuilder.PipelineItemBuildCompleted += _pipelineBuilder_PipelineItemBuildCompleted;
            _pipelineBuilder.BuildEnded += _pipelineBuilder_BuildEnded;
            _pipelineBuilder.PropertyChanged += _pipelineBuilder_PropertyChanged;
        }

        internal void AddItem(PipelineItemViewModel item)
        {
            OnItemAdded(new PipelineItemViewModelAddedEventArgs(PipelineProjectViewModel, item));
        }

        void pipelineProjectViewModel_PipelineItemAdded(object sender, PipelineItemViewModelEventArgs e)
        {
            OnPipelineItemAdded(e);
        }

        void pipelineProjectViewModel_PipelineItemRemoved(object sender, PipelineItemViewModelEventArgs e)
        {
            OnPipelineItemRemoved(e);
        }

        private void _pipelineBuilder_PipelineItemBuildCompleted(object sender, PipelineItemViewModelBuildCompletedEventArgs e)
        {
            OnPipelineItemBuildCompleted(e);
        }

        private void _pipelineBuilder_BuildEnded(object sender, EventArgs e)
        {
            OnBuildEnded(e);
        }

        void _pipelineBuilder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "QueuedCount":
                    {
                        if (_pipelineBuilder.QueuedCount > 0)
                        {
                            // add pipeline builder statusbar view
                            if (!this.Module._statusbars.Contains(_pipelineBuilder.PipelineBuilderStatusBarItemViewModel))
                                this.Module._statusbars.Add(_pipelineBuilder.PipelineBuilderStatusBarItemViewModel);
                        }
                        else
                        {
                            // add pipeline builder statusbar view
                            if (this.Module._statusbars.Contains(_pipelineBuilder.PipelineBuilderStatusBarItemViewModel))
                                this.Module._statusbars.Remove(_pipelineBuilder.PipelineBuilderStatusBarItemViewModel);

                        }
                        
                    }
                    break;
            }
        }
        
        private void OnItemAdded(PipelineItemViewModelAddedEventArgs e)
        {
            var handler = ItemAdded;
            if (handler != null)
                handler(this, e);
        }

        private void OnPipelineItemAdded(PipelineItemViewModelEventArgs e)
        {
            var handler = PipelineItemAdded;
            if (handler != null)
                handler(this, e);
        }

        private void OnPipelineItemRemoved(PipelineItemViewModelEventArgs e)
        {
            var handler = PipelineItemRemoved;
            if (handler != null)
                handler(this, e);
        }

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

        internal void Close()
        {
            if (PipelineProjectViewModel == null)
                return;

            if (PipelineProjectViewModel.IsProjectOpen)
                PipelineProjectViewModel.CloseProject();            

            // detach events
            PipelineProjectViewModel.PipelineItemAdded -= pipelineProjectViewModel_PipelineItemAdded;
            PipelineProjectViewModel.PipelineItemRemoved -= pipelineProjectViewModel_PipelineItemRemoved;
            _pipelineBuilder.PipelineItemBuildCompleted -= _pipelineBuilder_PipelineItemBuildCompleted;
            _pipelineBuilder.BuildEnded -= _pipelineBuilder_BuildEnded;
        }

        internal string GetAbsolutePath(string relativePath)
        {
            string documentDirectory = Path.GetDirectoryName(this.PipelineProjectViewModel.DocumentFile);
            return Path.Combine(documentDirectory, relativePath);
        }

        internal void Build(bool rebuild)
        {
            PipelineProjectViewModel.SaveProject();
            
            // TODO: open Builder View by clicking on the status bar and/or add a new button.
            // add pipelineBuilder view
            var site = this.Module.Site;
            var addPaneCmd = new nkast.ProtonType.Framework.Commands.AddPaneCmd(site, _pipelineBuilder);
            site.Controller.EnqueueAndExecute(addPaneCmd);


            // add pipeline builder statusbar view
            if (!this.Module._statusbars.Contains(_pipelineBuilder.PipelineBuilderStatusBarItemViewModel))
                this.Module._statusbars.Add(_pipelineBuilder.PipelineBuilderStatusBarItemViewModel);

            _pipelineBuilder.BuildAll(rebuild);
        }
                
        internal void RebuildItem(PipelineItemViewModel pipelineItemVM, bool rebuild)
        {
            PipelineProjectViewModel.SaveProject();

            System.Diagnostics.Debug.Assert(PipelineProjectViewModel.Project != null, "Warning: trying to build null project");
            if (PipelineProjectViewModel.Project == null) return;
            
            _pipelineBuilder.BuildItem(pipelineItemVM, rebuild);
        }

        internal void CleanAll()
        {
            PipelineProjectViewModel.SaveProject();

            _pipelineBuilder.CleanAll();
        }
        
        internal void CleanItem(PipelineItemViewModel pipelineItemVM)
        {
            _pipelineBuilder.CleanItem(pipelineItemVM);
        }
        
    }
}
