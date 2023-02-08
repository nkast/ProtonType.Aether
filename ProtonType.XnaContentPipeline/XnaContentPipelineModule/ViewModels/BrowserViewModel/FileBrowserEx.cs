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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Input;
using nkast.ProtonType.Framework.Attributes;
using nkast.ProtonType.Framework.Helpers;
using nkast.ProtonType.ViewModels;
using tainicom.TreeViewEx;
using tainicom.TreeViewEx.DragNDrop;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    [DefaultView(typeof(Views.FileBrowserView))]
    public partial class FileBrowserEx : FileBrowser
    {
        public RelayCommand SaveCommand { get; set; }
        public RelayCommand BuildAllCommand { get; set; }
        public RelayCommand CleanAllCommand { get; set; }

        internal ContentPipelineViewModel ContentPipelineViewModel { get { return (ContentPipelineViewModel)this.Model; } }
        
        public FileBrowserEx(ContentPipelineViewModel model, string contentFilename):base(model, contentFilename, null)
        {
            SaveCommand = new RelayCommand(Save);
            BuildAllCommand = new RelayCommand(BuildAll);
            CleanAllCommand = new RelayCommand(CleanAll);
            return;
        }

        public override FolderItem CreateFolderItem(string relativePath)
        {
            // create default asset folder 
            if (relativePath == string.Empty)
            {
                string absolutePath = ContentPipelineViewModel.GetAbsolutePath(relativePath);
                if (!Directory.Exists(absolutePath))
                    Directory.CreateDirectory(absolutePath);
            }

            return new FolderItemEx(this, relativePath);
        }

        public override BrowserItem CreateBrowserItem(string relativePath)
        {
            return new BrowserItemEx(this, relativePath);
        }

        public override string GetAbsolutePath(string relativePath)
        {
            return this.ContentPipelineViewModel.GetAbsolutePath(relativePath);
        }
        
        void Save(object parameter)
        {
            this.ContentPipelineViewModel.PipelineProjectViewModel.SaveProject(true);
            return;
        }

        void BuildAll(object parameter)
        {
            this.ContentPipelineViewModel.Build(true);
        }

        void CleanAll(object parameter)
        {
            this.ContentPipelineViewModel.CleanAll();
        }
        
        internal void IncludeItem(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            this.IncludeItem(item);
        }

        internal void ExcludeItem(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            this.ExcludeItem(item);
        }

        internal void RebuildItem(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            this.RebuildItem(item);
        }
        
        void IncludeItem(BrowserItem item)
        {
            List<BrowserItem> items = GetItemsToInclude(item);

            foreach (var browserItem in items)
            {
                if (browserItem is FolderItem)
                {
                    continue;
                }
                else if (browserItem is BrowserItemEx)
                {
                    var itemEx = (BrowserItemEx)browserItem;
                    var controller = this.ContentPipelineViewModel.Module.Site.Controller;
                    controller.EnqueueAndExecute(new IncludeBrowserItemCmd(this, itemEx));
                }
            }
            return;
        }

        void ExcludeItem(BrowserItem item)
        {
            List<BrowserItem> items = GetItemsToExclude(item);
            
            foreach (var browserItem in items)
            {
                if (browserItem is FolderItem)
                {
                    continue;
                }
                else if (browserItem is BrowserItemEx)
                {
                    var itemEx = (BrowserItemEx)browserItem;
                    if (itemEx.ProjectItemVM == null) continue;
                    var controller = this.ContentPipelineViewModel.Module.Site.Controller;
                    controller.EnqueueAndExecute(new ExcludeBrowserItemCmd(this, itemEx));
                }
            }
            return;
        }

        void RebuildItem(BrowserItem item)
        {
            List<BrowserItem> items = GetItemsToRebuild(item);
            
            foreach (var browserItem in items)
            {
                if (browserItem is FolderItem)
                {
                    continue;
                }
                else if (browserItem is BrowserItemEx)
                {
                    var itemEx = browserItem as BrowserItemEx;
                    if (itemEx.ProjectItemVM == null) continue;
                    itemEx.Rebuild();
                }
            }
        }
        
        internal List<BrowserItem> GetItemsToInclude(BrowserItem item)
        {
            List<BrowserItem> items;

            if (this.SelectedItems.Contains(item))
                items = this.SelectedItems.ToList();
            else
                items = new List<BrowserItem>(new[] {item});

            items = items.Where((i) =>
            {
                if (i is BrowserItemEx)
                {
                    var itemEx = (BrowserItemEx)i;
                    return (itemEx.ProjectItemVM == null);
                }
                return false;
            }).ToList();

            return items;
        }

        internal List<BrowserItem> GetItemsToExclude(BrowserItem item)
        {
            List<BrowserItem> items;

            if (this.SelectedItems.Contains(item))
                items = this.SelectedItems.ToList();
            else
                items = new List<BrowserItem>(new[] {item});

            items = items.Where((i) =>
            {
                if (i is BrowserItemEx)
                {
                    var itemEx = (BrowserItemEx)i;
                    return (itemEx.ProjectItemVM != null);
                }
                return false;
            }).ToList();

            return items;
        }

        internal List<BrowserItem> GetItemsToRebuild(BrowserItem item)
        {
            List<BrowserItem> items;

            if (this.SelectedItems.Contains(item))
                items = this.SelectedItems.ToList();
            else
                items = new List<BrowserItem>(new[] {item});

            items = items.Where((i) =>
            {
                if (i is BrowserItemEx)
                { 
                    var itemEx = (BrowserItemEx)i;
                    return (itemEx.ProjectItemVM != null);
                }
                return false;
            }).ToList();

            return items;
        }

        #region Drag & Drop Commands

        RelayCommand _dragCommand = null;
        public ICommand DragCommand
        {
            get
            {
                if (_dragCommand == null)
                    _dragCommand = new RelayCommand(ExecuteDrag, CanExecuteDrag);
                return _dragCommand;
            }
        }
        
        protected virtual bool CanExecuteDrag(object parameter)
        {
            object data = null;

            var dragParameters = (CanDragParameters)parameter;

            // Allow only one asset
            if (dragParameters.Items.Count() != 1) return false;
            TreeViewExItem tvei = dragParameters.Items.First();

            var browserItemEx = tvei.DataContext as BrowserItemEx;
            if (browserItemEx == null) return false;
            var fileBrowserEx = browserItemEx.FileBrowser as FileBrowserEx;
            
            if (browserItemEx != null)
            {
                if (browserItemEx.ProjectItemVM != null && !browserItemEx.Missing)
                {
                    var projectItemVM2 = browserItemEx.ProjectItemVM as PipelineItemViewModel;
                    if (projectItemVM2 != null)
                    {
                        var model = fileBrowserEx.ContentPipelineViewModel;                        
                        data = (model.DragItemConverter).Convert(projectItemVM2, typeof(object), null, CultureInfo.InvariantCulture);
                    }
                }
            }

            if (data != null)
                return true;

            return false;
        }
        
        protected virtual void ExecuteDrag(object parameter) 
        {
            object data = null;

            DragParameters dragParameters = (DragParameters)parameter;
            TreeViewExItem tvei = dragParameters.Items.First();
            
            var browserItemEx = tvei.DataContext as BrowserItemEx;
            var fileBrowserEx = browserItemEx.FileBrowser as FileBrowserEx;
            
            var projectItemVM = browserItemEx.ProjectItemVM;
            if (projectItemVM != null)
            {
                var projectItemVM2 = browserItemEx.ProjectItemVM as PipelineItemViewModel;
                if (projectItemVM2 != null)
                {
                    var model = fileBrowserEx.ContentPipelineViewModel;
                    data = (model.DragItemConverter).Convert(projectItemVM2, typeof(object), null, CultureInfo.InvariantCulture);
                }
            }

            if (data != null)
                dragParameters.Data.SetData(data);
        }

        #endregion

    }
}
