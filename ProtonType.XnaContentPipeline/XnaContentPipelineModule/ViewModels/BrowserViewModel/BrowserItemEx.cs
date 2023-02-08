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
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using nkast.ProtonType.Framework.Helpers;
using nkast.ProtonType.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Components.Common;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class BrowserItemEx: BrowserItem
    {   
        private FileBrowserEx FileBrowserEx {get {return (FileBrowserEx)base.FileBrowser; } }

        IPipelineItemViewModel _projectItemVM;
        internal IPipelineItemViewModel ProjectItemVM
        {
            get { return _projectItemVM; }
            set
            {
                if (_projectItemVM != value)
                {
                    _projectItemVM = value;
                    
                    RaisePropertyChanged(() => ProjectItemVM);
                    RaisePropertyChanged(() => Included);
                }
            }
        }

        public RelayCommand OpenCommand { get; set; }
        public RelayCommand IncludeCommand { get; set; }
        public RelayCommand ExcludeCommand { get; set; }
        public RelayCommand OpenContainingFolderCommand { get; set; }
        public RelayCommand RebuildCommand { get; set; }

        bool _missing;
        public bool Missing 
        {
            get { return _missing; }
            set 
            {
                if(_missing != value)
                {
                    _missing = value;
                    RaisePropertyChanged(() => Missing);
                    RaisePropertyChanged(() => FileIcon);
                }
            }
        }

        public bool Included { get { return (_projectItemVM != null); } }
        
        //used for filtering multiple firing of FileWatcher.Changed event.
        internal System.DateTime LastWriteTime;

        public virtual ImageSource FileIcon
        {
            get
            {
                string absolutePath = this.FileBrowser.GetAbsolutePath(_relativePath);
                try
                {
                    return FileIconInterop.GetIconFromFile(absolutePath, true);
                }
                catch (FileNotFoundException)
                {
                    Uri oUri = new Uri("pack://application:,,,/ProtonType.XnaContentPipelineModule;component/Icons/FileWarning_16x.png");
                    return new BitmapImage(oUri);
                }
            }
        }

        internal BrowserItemEx(FileBrowserEx fileBrowser, string relativePath):base(fileBrowser, relativePath) 
        {
            OpenCommand = new RelayCommand(OpenItem, canEx => true);
            OpenContainingFolderCommand = new RelayCommand(OpenContainingFolder, canEx => true);
            IncludeCommand = new RelayCommand(this.FileBrowserEx.IncludeItem, canEx => this.FileBrowserEx.GetItemsToInclude(this).Count > 0);
            ExcludeCommand = new RelayCommand(this.FileBrowserEx.ExcludeItem, canEx => this.FileBrowserEx.GetItemsToExclude(this).Count > 0);
            RebuildCommand = new RelayCommand(this.FileBrowserEx.RebuildItem, canEx => this.FileBrowserEx.GetItemsToRebuild(this).Count > 0);
        }

        void OpenItem(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            string absolutePath = FileBrowserEx.GetAbsolutePath(item.RelativePath);
            if (item is BrowserItem)            
            {
                System.Diagnostics.Process.Start(absolutePath);
            }
            return;
        }

        void OpenContainingFolder(object parameter)
        {
            BrowserItem item = (BrowserItem)parameter;
            string absolutePath = FileBrowserEx.GetAbsolutePath(item.RelativePath);
            System.Diagnostics.Process.Start("explorer.exe", "/select, " + absolutePath);
        }
        
        internal void Rebuild()
        {
            bool rebuild = true;
            if (ProjectItemVM == null) return;

            PipelineItemViewModel pipelineItemVM = ProjectItemVM as PipelineItemViewModel;
            if (pipelineItemVM == null) return;
            
            FileBrowserEx.ContentPipelineViewModel.RebuildItem(pipelineItemVM, rebuild);
        }
    }
}
