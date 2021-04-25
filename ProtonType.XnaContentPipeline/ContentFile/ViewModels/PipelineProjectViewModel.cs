// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

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
using System.ComponentModel;
using System.IO;
using System.Linq;
using tainicom.ProtonType.Framework.Modules;
using tainicom.ProtonType.Framework.ViewModels;
using tainicom.ProtonType.XnaContentPipeline.Common;
using tainicom.ProtonType.XnaContentPipeline.ProxyClient;

namespace tainicom.ProtonType.XnaContentPipeline.ViewModels
{
    public class PipelineProjectViewModel : BaseViewModel, IPipelineItemViewModel
    {
        private readonly ISiteViewModel _site;

        internal readonly ReferencesViewModel _references;

        internal readonly PipelineTypes PipelineTypes = new PipelineTypes();

        private readonly List<ContentItemTemplate> _templateItems;
        internal readonly IPipelineLogger _logger;

        
        internal IEnumerable<ContentItemTemplate> Templates { get { return _templateItems; } }

        /*internal*/ public PipelineProject Project { get; private set; }

        public bool IsProjectOpen { get; private set; }
        
        public bool IsProjectDirty { get; private set; }

        public string Location { get { return Project.Location; } }

        public string ProjectName { get { return Path.GetFileNameWithoutExtension(Project.OriginalPath); } }
        


        [Category("Settings")]
        public bool Compress
        {
            get { return Project.Compress; }
            set { Project.Compress = value; }
        }

        [Category("Settings")]
        public ProxyTargetPlatform Platform 
        {
            get { return Project.Platform; }
            set { Project.Platform = value; }
        }

        [Category("Settings")]
        public ProxyGraphicsProfile Profile
        {
            get { return Project.Profile; }
            set { Project.Profile = value; }
        }
                    
        [Category("Settings")]
        public string Config
        {
            get { return Project.Config; }
            set { Project.Config = value; }
        }

        [Category("Settings")]
        public string OutputDir
        {
            get { return Project.OutputDir; }
            set { Project.OutputDir = value; }
        }

        [Category("Settings")]
        public string IntermediateDir
        {
            get { return Project.IntermediateDir; }
            set { Project.IntermediateDir = value; }
        }
        
        [Category("Settings")]
        public ReferencesViewModel References
        {
            get { return _references; }
            set { }
        }


        private readonly ObservableCollection<PipelineItemViewModel> _pipelineItemsVM;
        public readonly IList<PipelineItemViewModel> PipelineItemsVM;

        public PipelineProjectViewModel(ISiteViewModel site, IPipelineLogger logger)
        {
            this._site = site;

            this._references = new ReferencesViewModel(_site, this);
            _templateItems = new List<ContentItemTemplate>();
            LoadTemplates(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Templates"));

            _logger = logger;
            this.IsProjectOpen = false;

            _pipelineItemsVM = new ObservableCollection<PipelineItemViewModel>();
            PipelineItemsVM = new ReadOnlyObservableCollection<PipelineItemViewModel>(_pipelineItemsVM);
        }
        
        public void NewProject(string projectFilePath)
        {
            SaveProject();

            // TODO: Do we reuse PipelineProjectViewModel instances?
            if (IsProjectOpen)
                CloseProject();
            
            // Clear existing project data, initialize to a new blank project.
            Project = new PipelineProject();

            this._references.Load();
            Project.PipelineItemPropertyChanged += project_PipelineItemPropertyChanged;
            
            // Save the new project.
            Project.OriginalPath = projectFilePath;
            Project.Profile = ProxyGraphicsProfile.Reach;
            Project.OutputDir = "../Content";
            IsProjectOpen = true;
            IsProjectDirty = true;
        }

        internal void project_PipelineItemPropertyChanged(object sender, PipelineItemPropertyChangedEventArgs e)
        {
            IsProjectDirty = true;
        }

        public void OpenProject(string projectFilePath)
        {
            if (IsProjectOpen)
                CloseProject();
            
            try
            {
                Project = PipelineProjectReader.LoadProject(projectFilePath, _logger);
                this._references.Load();

                CreateItems();

                LoadTemplates(Project.Location);
                
                IsProjectOpen = true;
                IsProjectDirty = false;
            }
            catch (PipelineProjectReaderException pe)
            {
                _logger.LogError(pe.Message, projectFilePath);
            }
            catch (Exception e)
            {
                _logger.LogError("Failed to open project.", projectFilePath);
                return;
            }
            return;
        }
                
        private void CreateItems()
        {
            foreach (var item in Project.PipelineItems)
            {
                var itemVM = new PipelineItemViewModel(this, item);
                _pipelineItemsVM.Add(itemVM);
            }
        }
        
        public void SaveProject(bool force = true)
        {
            IsProjectDirty = (IsProjectDirty || force);
            if (!IsProjectDirty) return;

            // Make sure project directory exist
            //var projectDirectory = _project.Location;
            var projectDirectory = Path.GetDirectoryName(Project.OriginalPath);
            if (!Directory.Exists(projectDirectory))
                Directory.CreateDirectory(projectDirectory);

            PipelineProjectWriter.SaveProject(Project, Project.OriginalPath, null);

            IsProjectDirty = false;
        }

        public void CloseProject()
        {
            SaveProject();

            Project.PipelineItemPropertyChanged -= project_PipelineItemPropertyChanged;

            IsProjectOpen = false;
            IsProjectDirty = false;
            Project = null;
        }
        
        public void Add(PipelineItemViewModel pipelineItemVM)
        {
            int index = FindInsertIndex(pipelineItemVM);
            Project.AddItem(pipelineItemVM.PipelineItem, index);
            _pipelineItemsVM.Add(pipelineItemVM);
            IsProjectDirty = true;

            OnPipelineItemAdded(new PipelineItemViewModelEventArgs(pipelineItemVM));
        }

        static FilePathComparer _filePathComparer = new FilePathComparer();
        class FilePathComparer : IComparer<PipelineItem>
        {
            int IComparer<PipelineItem>.Compare(PipelineItem x, PipelineItem y)
            {
                return x.OriginalPath.CompareTo(y.OriginalPath);
            }
        }

        private int FindInsertIndex(PipelineItemViewModel pipelineItemVM)
        {   
            int index = Project.BinarySearch(pipelineItemVM.PipelineItem, _filePathComparer);
            if (index < 0)
                index  = index ^ (-1);
            return index;
        }

        public void Remove(PipelineItemViewModel pipelineItemVM)
        {
            _pipelineItemsVM.Remove(pipelineItemVM);
            Project.RemoveItem(pipelineItemVM.PipelineItem);
            IsProjectDirty = true;

            OnPipelineItemRemoved(new PipelineItemViewModelEventArgs(pipelineItemVM));
        }

        public PipelineItemViewModel Include(string fileAbsolutePath)
        {   
            // Root the path to the project.
            if (!Path.IsPathRooted(fileAbsolutePath))
                fileAbsolutePath = Path.Combine(Location, fileAbsolutePath);

            if (!File.Exists(fileAbsolutePath))
                throw new Exception("File not found. " + fileAbsolutePath);

            var itemVM = CreateContent(fileAbsolutePath);

            this.Add(itemVM);

            return itemVM;
        }

        private PipelineItemViewModel CreateContent(string sourceFile)
        {
            // Make sure the source file is relative to the project.
            var projectDir = Project.Location + Path.DirectorySeparatorChar;
            sourceFile = PathHelper.GetRelativePath(projectDir, sourceFile);

            // check duplicates.
            var previous = Project.PipelineItems.FirstOrDefault(e => e.OriginalPath.Equals(sourceFile, StringComparison.InvariantCultureIgnoreCase));
            if (previous != null)
                throw new Exception("sourceFile allready added.");

            // Create the item for processing later.
            var item = new PipelineItem()
            {
                BuildAction = BuildAction.Build,
                OriginalPath = sourceFile,
                DestinationPath = sourceFile,
            };
            var itemVM = new PipelineItemViewModel(this, item);
            return itemVM;
        }


        public string ReplaceSymbols(string parameter)
        {
            return Project.ReplaceSymbols(parameter);
        }
        

        public event EventHandler<PipelineItemViewModelEventArgs> PipelineItemAdded;
        public event EventHandler<PipelineItemViewModelEventArgs> PipelineItemRemoved;


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
         


        internal void LoadTemplates(string path)
        {
            if (!Directory.Exists(path))
                return;

            var files = Directory.GetFiles(path, "*.template", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var lines = File.ReadAllLines(f);
                if (lines.Length != 5)
                    throw new Exception("Invalid template");

                var fpath = Path.GetDirectoryName(f);
                var templateFile = lines[4];
                templateFile = Path.GetFullPath(Path.Combine(fpath, templateFile));

                var item = new ContentItemTemplate(
                    label: lines[0],
                    icon: lines[1],
                    importerName: lines[2],
                    processorName: lines[3],
                    templateFile: templateFile
                );

                if (_templateItems.Any(i => i.Label == item.Label))
                    continue;

                _templateItems.Add(item);
            }
        }
        

        internal IPipelineItem GetItem(string originalPath)
        {
            if (Project.OriginalPath.Equals(originalPath, StringComparison.OrdinalIgnoreCase))
                return Project;

            foreach (var i in Project.PipelineItems)
            {
                if (string.Equals(i.OriginalPath, originalPath, StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return null;
        }

        public string GetFullPath(string filePath)
        {
            if (Project == null || Path.IsPathRooted(filePath))
                return filePath;

            filePath = filePath.Replace("/", Path.DirectorySeparatorChar.ToString());
            if (filePath.StartsWith("\\"))
                filePath = filePath.Substring(1);

            if (Path.IsPathRooted(filePath))
                return filePath;

            return Project.Location + Path.DirectorySeparatorChar + filePath;
        }



    }
}
