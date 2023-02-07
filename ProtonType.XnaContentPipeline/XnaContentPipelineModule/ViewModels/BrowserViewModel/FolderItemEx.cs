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
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using tainicom.ProtonType.Framework.Helpers;
using tainicom.ProtonType.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class FolderItemEx : FolderItem
    {
        private FileBrowserEx FileBrowserEx { get { return (FileBrowserEx)base.FileBrowser; } }
        
        private RenamedEventArgs _lastRenamedItem;

        public RelayCommand OpenCommand { get; set; }
        public RelayCommand IncludeCommand { get; set; }
        public RelayCommand ExcludeCommand { get; set; }
        public RelayCommand OpenContainingFolderCommand { get; set; }
        public RelayCommand RebuildCommand { get; set; }


        public virtual ImageSource FileIcon
        {
            get
            {
                string absolutePath = this.FileBrowser.GetAbsolutePath(_relativePath);
                try
                {
                    if (File.GetAttributes(absolutePath).HasFlag(FileAttributes.Directory))
                    {
                        Uri oUri = new Uri("pack://application:,,,/ProtonType.XnaContentPipelineModule;component/Icons/FolderOpen_16x.png");
                        return new BitmapImage(oUri);
                    }
                    return null;
                }
                catch (FileNotFoundException)
                {
                    Uri oUri = new Uri("pack://application:,,,/ProtonType.XnaContentPipelineModule;component/Icons/FileWarning_16x.png");
                    return new BitmapImage(oUri);
                }
            }
        }

        internal FolderItemEx(FileBrowser fileBrowser, string relativePath): base(fileBrowser, relativePath)
        {
            OpenCommand = new RelayCommand(OpenItem, canEx => true);            
            IncludeCommand = new RelayCommand(IncludeItem, canEx => false);
            ExcludeCommand = new RelayCommand(ExcludeItem, canEx => false);
            OpenContainingFolderCommand = new RelayCommand(OpenContainingFolder, canEx => true);
            RebuildCommand = new RelayCommand(RebuildItem, canEx => false);
        }

        void OpenItem(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            string absolutePath = FileBrowserEx.GetAbsolutePath(item.RelativePath);

            if (item is FolderItem)
            {
                System.Diagnostics.Process.Start("explorer.exe", absolutePath);
                return;
            }
            return;
        }
        
        void IncludeItem(object parameter)
        {
        }

        void ExcludeItem(object parameter)
        {
        }

        void OpenContainingFolder(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            string absolutePath = FileBrowserEx.GetAbsolutePath(item.RelativePath);
            System.Diagnostics.Process.Start("explorer.exe", "/select, " + absolutePath);
        }

        void RebuildItem(object parameter)
        {
        }


        protected override ObservableCollection<BrowserItem> InitItems()
        {
            ObservableCollection<BrowserItem> items = base.InitItems();
            
            var pipelineProjectVM = FileBrowserEx.ContentPipelineViewModel.PipelineProjectViewModel;
            var pipelineProject = pipelineProjectVM.Project;

            //associate .mgcb file with pipelineProject
            if (this._relativePath == "")
            {
                // find itemEx
                BrowserItemEx itemEx = null;
                foreach (BrowserItem item in items)
                {
                    string contentFilename = Path.GetFileName(FileBrowserEx.ContentPipelineViewModel.DocumentFile);
                    if (item.Name == contentFilename)
                    {
                        itemEx = item as BrowserItemEx;
                        if (itemEx != null) break;
                    }
                }
                if (itemEx != null)
                {
                    //associate .mgcb file with pipelineProject
                    itemEx.ProjectItemVM = pipelineProjectVM;
                    // make .mgcb file first item in the Browser
                    items.Remove(itemEx);
                    items.Insert(0, itemEx);
                }
            }

            List<PipelineItemViewModel> pipelineItems = new List<PipelineItemViewModel>(pipelineProjectVM.PipelineItemsVM);
            
            //associate items with assets
            for (int i = pipelineItems.Count-1; i >= 0; i--)
            {
                //ignore items not rooted in the project directory
                if (pipelineItems[i].Location != this._relativePath)
                {
                    pipelineItems.RemoveAt(i);
                    continue;
                }

                //associate browser items with pipelineItem
                if (AssociateContentItem(items, pipelineItems[i]))
                {
                    pipelineItems.RemoveAt(i);
                    continue;
                }
            }

            //add virtual items
            for (int i = pipelineItems.Count-1; i >= 0; i--)
            {
                BrowserItemEx itemEx = FileBrowserEx.CreateBrowserItem(pipelineItems[i].OriginalPath) as BrowserItemEx;
                itemEx.Missing = true;
                //associate virtual browser item with missing pipelineItem
                itemEx.ProjectItemVM = pipelineItems[i];
                items.Add(itemEx);
            }

            return items;
        }

        private bool AssociateContentItem(ObservableCollection<BrowserItem> browserItems, PipelineItemViewModel pipelineItemVM)
        {
            //search for pipelineItem in browserItems
            foreach(BrowserItem item in browserItems)
            {
                if (item is FolderItem) continue;
                if (item.Name != pipelineItemVM.Filename) continue;
                string relativePath = Path.Combine(pipelineItemVM.Location, pipelineItemVM.Filename);
                if (item.RelativePath != relativePath) continue;
                BrowserItemEx itemEx = item as BrowserItemEx;
                if(itemEx ==null) continue;
                //associate browser item with pipelineItem
                itemEx.ProjectItemVM = pipelineItemVM;
                return true;
            }

            return false;
        }


        protected override bool FilterFolder(string folderRelativePath)
        {
            if (folderRelativePath.Equals("bin", StringComparison.OrdinalIgnoreCase))
                return true;
            if (folderRelativePath.Equals("obj", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }


        protected override void OnWatcherChanged(FileSystemEventArgs e)
        {
            base.OnWatcherChanged(e);
            
            if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                if (Path.GetExtension(e.Name).ToLowerInvariant() == ".mgcb")
                    return;
                var item = GetItem(e.Name);
                var itemEx = item as BrowserItemEx;
                if (itemEx != null && itemEx.ProjectItemVM != null)
                {
                    //filter multiple firing of '.Changed' event.
                    string absolutePath = item.FileBrowser.GetAbsolutePath(item.RelativePath);
                    DateTime lastWriteTime = File.GetLastWriteTime(absolutePath);
                    if (lastWriteTime != itemEx.LastWriteTime)
                    {
                        itemEx.LastWriteTime = lastWriteTime;
                        itemEx.Rebuild();
                    }
                }
            }
            return;
        }

        protected override void OnWatcherCreated(FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                var item = GetItem(e.Name);
                var itemEx = item as BrowserItemEx;
                if (itemEx != null && itemEx.ProjectItemVM != null)
                {
                    if (itemEx.Missing == true)
                    {
                        itemEx.Missing = false;
                        return;
                    }
                }
            }
            
            base.OnWatcherCreated(e);
        }

        protected override void OnWatcherDeleted(FileSystemEventArgs e)
        {
            var item = GetItem(e.Name);
            var itemEx = item as BrowserItemEx;
            if (item != null && itemEx.ProjectItemVM != null)
            {
                if (_lastRenamedItem != null)
                {
                    if (_lastRenamedItem.Name == e.Name && File.Exists(_lastRenamedItem.OldFullPath))
                    {
                        RenameItem(_lastRenamedItem.Name, _lastRenamedItem.OldName);
                        item = GetItem(_lastRenamedItem.OldName);
                        itemEx = item as BrowserItemEx;
                        itemEx.Rebuild();
                        _lastRenamedItem = null;
                        return;
                    }
                }

                itemEx.Missing = true;
                return;
            }

            base.OnWatcherDeleted(e);
        }

        protected override void OnWatcherRenamed(RenamedEventArgs e)
        {
            var item = GetItem(e.Name);
            var itemEx = item as BrowserItemEx;
            var itemOld = GetItem(e.OldName);
            var itemExOld = itemOld as BrowserItemEx;

            if (itemEx != null && itemEx.ProjectItemVM != null && (itemExOld == null || itemExOld.ProjectItemVM == null))
            {
                if (itemOld!= null)
                    RemoveItem(itemOld.Name);
                itemEx.Missing = false;
                itemEx.Rebuild();
                return;
            }
            
            base.OnWatcherRenamed(e);
            this._lastRenamedItem = e;
        }
    }
}
