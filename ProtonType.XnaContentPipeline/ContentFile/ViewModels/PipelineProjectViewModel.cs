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
using System.Threading.Tasks;
using nkast.ProtonType.Framework.Modules;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.Common.Converters;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class PipelineProjectViewModel : BaseViewModel, IPipelineItemViewModel
    {
        private readonly ISiteViewModel _site;

        internal readonly ReferencesViewModel _references;
        internal readonly PackagesViewModel _packages;

        internal readonly PipelineTypes PipelineTypes = new PipelineTypes();

        private readonly List<ContentItemTemplate> _templateItems;
        internal readonly IPipelineLogger _logger;


        internal IEnumerable<ContentItemTemplate> Templates { get { return _templateItems; } }

        /*internal*/
        public PipelineProject Project { get; private set; }

        public bool IsProjectOpen { get; private set; }

        public bool IsProjectDirty { get; private set; }

        public string Location { get { return Project.Location; } }

        public string ProjectName { get { return Path.GetFileNameWithoutExtension(Project.OriginalPath); } }



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
        public bool Compress
        {
            get { return Project.Compress; }
            set { Project.Compress = value; }
        }

        [Category("Settings")]
        public CompressionMethod Compression
        {
            get { return Project.Compression; }
            set { Project.Compression = value; }
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
        public PackagesViewModel Packages
        {
            get { return _packages; }
            set { }
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
            this._packages = new PackagesViewModel(_site, this);

            Importers = new ReadOnlyObservableCollection<ImporterDescription>(_importers);
            Processors = new ReadOnlyObservableCollection<ProcessorDescription>(_processors);

            _templateItems = new List<ContentItemTemplate>();
            LoadTemplates(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "Templates"));

            _logger = logger;
            this.IsProjectOpen = false;

            _pipelineItemsVM = new ObservableCollection<PipelineItemViewModel>();
            PipelineItemsVM = new ReadOnlyObservableCollection<PipelineItemViewModel>(_pipelineItemsVM);
        }

        public void NewProject(string projectFilePath)
        {
            // Clear existing project data, initialize to a new blank project.
            Project = new PipelineProject();

            this.LoadProject();

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
                PipelineProjectReader reader = new PipelineProjectReader();
                Project = reader.LoadProject(projectFilePath, _logger);

                this.LoadProject();

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

        static PipelineItemPathComparer _pipelineItemPathComparer = new PipelineItemPathComparer();       

        private int FindInsertIndex(PipelineItemViewModel pipelineItemVM)
        {
            int index = Project.BinarySearch(pipelineItemVM.PipelineItem, _pipelineItemPathComparer);
            if (index < 0)
                index = index ^ (-1);
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


        private readonly ObservableCollection<ImporterDescription> _importers = new ObservableCollection<ImporterDescription>();
        private readonly ObservableCollection<ProcessorDescription> _processors = new ObservableCollection<ProcessorDescription>();

        internal readonly IList<ImporterDescription> Importers;
        internal readonly IList<ProcessorDescription> Processors;


        internal TypeConverter FindConverter(Type type)
        {
            if (type == typeof(Microsoft.Xna.Framework.Color))
                return new StringToColorConverter();

            return TypeDescriptor.GetConverter(type);
        }

        internal void LoadProject()
        {
            // load packages
            foreach (Package package in this.Project.PackageReferences)
            {
                PackageViewModel packageVM = this.CreatePackage(package);
                if (packageVM == null)
                    continue;

                _packages._packages.Add(packageVM);
            }

            using (PipelineProxyClient pipelineProxy = new PipelineProxyClient())
            {
                pipelineProxy.BeginListening();

                pipelineProxy.SetBaseDirectory(this.Location);
                pipelineProxy.SetProjectFilename(Path.GetFileName(this.Project.OriginalPath));

                // Set Global Settings
                pipelineProxy.SetOutputDir(this.OutputDir);
                pipelineProxy.SetIntermediateDir(this.IntermediateDir);
                pipelineProxy.SetPlatform(this.Platform);
                pipelineProxy.SetConfig(this.Config);
                pipelineProxy.SetProfile(this.Profile);


                IProxyLogger logger = new ProxyLogger(this._logger);

                List<Task> addPackageTasks = new List<Task>();
                foreach (Package package in this.Project.PackageReferences)
                {
                    Task task = pipelineProxy.AddPackageAsync(logger, package);
                    addPackageTasks.Add(task);
                }
                Task.WaitAll(addPackageTasks.ToArray());

                Task resolvePackagesTask = pipelineProxy.ResolvePackagesAsync(logger);
                resolvePackagesTask.Wait();

                // load all types from references
                List<Task> addAssemblyTasks = new List<Task>();
                foreach (string refPath in this.Project.References)
                {
                    AssemblyViewModel assembly = this.CreateAssembly(refPath);
                    if (assembly == null)
                        continue;

                    string assemblyPath = assembly.NormalizedAbsoluteFullPath;

                    Task task = pipelineProxy.AddAssemblyAsync(logger, assemblyPath);
                    addAssemblyTasks.Add(task);

                    _references._assemblies.Add(assembly);
                }
                Task.WaitAll(addAssemblyTasks.ToArray());

                IProxyLogger logger2 = new ProxyLogger(this._logger);
                Task<List<ImporterDescription>> importersTask = pipelineProxy.GetImportersAsync(logger2);
                importersTask.Wait();
                List<ImporterDescription> importers = importersTask.Result;
                foreach (ImporterDescription importerDesc in importers)
                    _importers.Add(importerDesc);

                Task<List<ProcessorDescription>> processorsTask = pipelineProxy.GetProcessorsAsync(logger2);
                processorsTask.Wait();
                List<ProcessorDescription> processors = processorsTask.Result;
                foreach (ProcessorDescription processorDesc in processors)
                    _processors.Add(processorDesc);
            }
        }

        internal PackageViewModel CreatePackage(Package package)
        {
            string packageName = package.Name;

            PackageViewModel packageVM = new PackageViewModel(this, _packages, package);

            // check if assembly is allready added
            foreach (PackageViewModel otherPackage in _packages)
            {
                if (otherPackage.PackageName == packageVM.PackageName
                && otherPackage.PackageVersion == packageVM.PackageVersion)
                    return null;
            }

            if (string.IsNullOrEmpty(packageVM.PackageName))
            {
                //throw new InvalidOperationException("Package.Name is null or empty.");
                //TODO: log error
                return null;
            }

            return packageVM;
        }

        internal AssemblyViewModel CreateAssembly(string refPath)
        {
            AssemblyViewModel assembly = new AssemblyViewModel(this, _references, refPath);

            // check if assembly is allready added
            foreach (AssemblyViewModel otherAssembly in _references.Assemblies)
            {
                if (otherAssembly.NormalizedAbsoluteFullPath == assembly.NormalizedAbsoluteFullPath)
                    return null;
            }

            if (string.IsNullOrEmpty(assembly.AbsoluteFullPath))
            {
                //throw new InvalidOperationException("assembly.AbsoluteFullPath is null or empty.");
                //TODO: log error
                return null;
            }
            if (!Path.IsPathRooted(assembly.AbsoluteFullPath))
            {
                //throw new InvalidOperationException("assemblyFilePath is not rooted path.");
                //TODO: log error
                return null;
            }

            return assembly;
        }

        public IEnumerable<ImporterDescription> FindImporters(string fileExtension)
        {
            List<ImporterDescription> importers = new List<ImporterDescription>();

            foreach (ImporterDescription importerDesc in Importers)
            {
                if (importerDesc.FileExtensions.Contains(fileExtension, StringComparer.InvariantCultureIgnoreCase))
                    importers.Add(importerDesc);
            }

            return importers;
        }

        public ImporterDescription FindImporter(string importerName, string fileExtension)
        {
            if (!string.IsNullOrEmpty(importerName))
            {
                foreach (ImporterDescription importerDesc in Importers)
                {
                    if (importerDesc.TypeName.Equals(importerName))
                        return importerDesc;
                }

                foreach (ImporterDescription importerDesc in Importers)
                {
                    if (importerDesc.DisplayName.Equals(importerName))
                        return importerDesc;
                }

                //Debug.Fail(string.Format("Importer not found! name={0}, ext={1}", name, fileExtension));
                return null;
            }

            foreach (ImporterDescription importerDesc in Importers)
            {
                if (importerDesc.FileExtensions.Contains(fileExtension, StringComparer.InvariantCultureIgnoreCase))
                    return importerDesc;
            }

            //Debug.Fail(string.Format("Importer not found! name={0}, ext={1}", name, fileExtension));
            return null;
        }

        public List<ProcessorDescription> FindProcessors(ImporterDescription importerDesc)
        {
            if (importerDesc != null)
            {
                List<ProcessorDescription> processors = new List<ProcessorDescription>();

                foreach (ProcessorDescription processorDesc in this.Processors)
                {
                    if (IsProcessorValid(importerDesc, processorDesc))
                    {
                        processors.Add(processorDesc);
                    }
                }

                return processors;
            }

            //Debug.Fail(string.Format("Processor not found! name={0}, importer={1}", name, importer));
            return null;

        }

        private bool IsProcessorValid(ImporterDescription importer, ProcessorDescription processor)
        {
            if (processor.TypeName.Equals(importer.DefaultProcessor))
                return true;
            if (processor.InputTypeFullName == importer.OutputTypeFullName)
                return true;
            if (processor.InputBaseTypesFullName.Contains(importer.OutputTypeFullName))
                return true;
            if (importer.OutputBaseTypesFullName.Contains(processor.InputTypeFullName))
                return true;

            return false;


        }

        public ProcessorDescription FindProcessor(string processorName, ImporterDescription importerDesc)
        {
            if (!string.IsNullOrEmpty(processorName))
            {
                foreach (ProcessorDescription processorDesc in Processors)
                {
                    if (processorDesc.TypeName.Equals(processorName))
                        return processorDesc;
                }

                //Debug.Fail(string.Format("Processor not found! name={0}, importer={1}", name, importer));
                return null;
            }

            if (importerDesc != null)
            {
                foreach (ProcessorDescription processorDesc in Processors)
                {
                    if (processorDesc.TypeName.Equals(importerDesc.DefaultProcessor))
                        return processorDesc;
                }
            }

            //Debug.Fail(string.Format("Processor not found! name={0}, importer={1}", name, importer));
            return null;
        }

        internal void AddReference(AssemblyViewModel assembly)
        {
            using (PipelineProxyClient pipelineProxy = new PipelineProxyClient())
            {
                pipelineProxy.BeginListening();

                pipelineProxy.SetBaseDirectory(this.Location);
                pipelineProxy.SetProjectFilename(Path.GetFileName(this.Project.OriginalPath));

                IProxyLogger logger = new ProxyLogger(this._logger);
                Task task = pipelineProxy.AddAssemblyAsync(logger, assembly.NormalizedAbsoluteFullPath);
                Task.WaitAll(new[] { task });

                _references._assemblies.Add(assembly);
                this.Project.References.Add(assembly.OriginalPath); // update model

                Task<List<ImporterDescription>> importersTask = pipelineProxy.GetImportersAsync(logger);
                importersTask.Wait();
                List<ImporterDescription> importers = importersTask.Result;
                foreach (ImporterDescription importerDesc in importers)
                    if (importerDesc.AssemblyPath == assembly.NormalizedAbsoluteFullPath)
                        _importers.Add(importerDesc);

                Task<List<ProcessorDescription>> processorsTask = pipelineProxy.GetProcessorsAsync(logger);
                processorsTask.Wait();
                List<ProcessorDescription> processors = processorsTask.Result;
                foreach (ProcessorDescription processorDesc in processors)
                    if (processorDesc.AssemblyPath == assembly.NormalizedAbsoluteFullPath)
                        _processors.Add(processorDesc);
            }
        }

        internal void AddPackage(PackageViewModel packageVM)
        {
            using (PipelineProxyClient pipelineProxy = new PipelineProxyClient())
            {
                pipelineProxy.BeginListening();

                pipelineProxy.SetBaseDirectory(this.Location);
                pipelineProxy.SetProjectFilename(Path.GetFileName(this.Project.OriginalPath));

                IProxyLogger logger = new ProxyLogger(this._logger);
                Task task = pipelineProxy.AddPackageAsync(logger, packageVM.Package);
                Task.WaitAll(new[] { task });

                Task resolvePackagesTask = pipelineProxy.ResolvePackagesAsync(logger);
                resolvePackagesTask.Wait();

                _packages._packages.Add(packageVM);
                this.Project.PackageReferences.Add(packageVM.Package); // update model

                Task<List<ImporterDescription>> importersTask = pipelineProxy.GetImportersAsync(logger);
                importersTask.Wait();
                List<ImporterDescription> importers = importersTask.Result;
                foreach (ImporterDescription importerDesc in importers)
                {
                    if (!_importers.Select((i)=> i.TypeFullName).Contains(importerDesc.TypeFullName))
                        _importers.Add(importerDesc);
                }

                Task<List<ProcessorDescription>> processorsTask = pipelineProxy.GetProcessorsAsync(logger);
                processorsTask.Wait();
                List<ProcessorDescription> processors = processorsTask.Result;
                foreach (ProcessorDescription processorDesc in processors)
                {
                    if (!_processors.Select((i) => i.TypeFullName).Contains(processorDesc.TypeFullName))
                        _processors.Add(processorDesc);
                }
            }
        }
    }
}
