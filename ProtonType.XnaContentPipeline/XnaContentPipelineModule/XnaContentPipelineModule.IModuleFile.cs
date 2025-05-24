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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using nkast.ProtonType.Framework.Commands;
using nkast.ProtonType.Framework.Modules;
using nkast.ProtonType.XnaContentPipeline.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline 
{
    public partial class XnaContentPipelineModule: IModuleFile, IModuleFileNew, IModuleFileSave
    {
        public List<FileExtension> _fileExtensions = new List<FileExtension>();

        IEnumerable<FileExtension> IModuleFile.FileExtensions
        {
            get { return _fileExtensions; }
        }


        private void InitializeIModuleFile()
        {   
            // define supported file extensions
            _fileExtensions.Add(new FileExtension(".mgcb","content pipeline"));

        }

        IFileViewModel IModuleFile.FileOpen(string filepath)
        {
            var contentPipeline = new ContentPipelineViewModel(this);
            contentPipeline.LoadContentProject(filepath);
            _fileViewModels.Add(contentPipeline);
            OpenFileBrowserEx(contentPipeline);

            return contentPipeline;
        }

        IFileViewModel IModuleFileNew.FileNew()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "MGCB files (*.mgcb)|*.mgcb";
            saveFileDialog.DefaultExt = ".mgcb";
            saveFileDialog.AddExtension = true;
            saveFileDialog.FileName = "Content";
            saveFileDialog.Title = "Create New Content Pipeline Project";

            if (saveFileDialog.ShowDialog() == false)
                return null;

            string filepath = saveFileDialog.FileName;

            var contentPipeline = new ContentPipelineViewModel(this);
            contentPipeline.LoadContentProject(filepath);
            _fileViewModels.Add(contentPipeline);
            OpenFileBrowserEx(contentPipeline);

            return contentPipeline;
        }

        private void OpenFileBrowserEx(ContentPipelineViewModel contentPipeline)
        {
            var contentFile = contentPipeline.DocumentFile;
            string contentFilename = Path.GetFileName(contentFile);
            FileBrowserEx civm = new FileBrowserEx(contentPipeline, contentFilename);
            var addPaneCmd = new AddPaneCmd(this.Site, civm);
            this.Site.Controller.EnqueueAndExecute(addPaneCmd);
        }

        void IModuleFile.FileClose(IFileViewModel fileViewModel)
        {
            if (fileViewModel is ContentPipelineViewModel)
                ((ContentPipelineViewModel)fileViewModel).Close();
        }

        ObservableCollection<IFileViewModel> _fileViewModels = new ObservableCollection<IFileViewModel>();
        ReadOnlyObservableCollection<IFileViewModel> _readOnlyFileViewModels;

        IEnumerable<IFileViewModel> IModuleFile.FileViewModels
        {
            get 
            {
                if (_readOnlyFileViewModels == null)
                    _readOnlyFileViewModels = new ReadOnlyObservableCollection<IFileViewModel>(_fileViewModels);

                return _readOnlyFileViewModels;
            }
        }
        
        void IModuleFileSave.FileSave(IFileViewModel fileViewModel)
        {
            var contentLibraryViewModel = fileViewModel as ContentPipelineViewModel;
            if (contentLibraryViewModel != null)
            {
                contentLibraryViewModel.PipelineProjectViewModel.SaveProject(true);
            }
        }

    }
}
