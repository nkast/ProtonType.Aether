﻿#region License
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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using nkast.ProtonType.Framework.Commands;
using nkast.ProtonType.Framework.Helpers;
using nkast.ProtonType.Framework.Modules;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.Common.Converters;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;
using Win32 = Microsoft.Win32;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class ReferencesViewModel : BaseViewModel, IList, IReceiver, INotifyCollectionChanged
    {
        private readonly ISiteViewModel _site;
        private PipelineProjectViewModel _project;

        private readonly ObservableCollection<AssemblyViewModel> _assemblies;
        private readonly ObservableCollection<ImporterDescription> _importers;
        private readonly ObservableCollection<ProcessorDescription> _processors;
        
        internal readonly IList<AssemblyViewModel> Assemblies;
        internal readonly IList<ImporterDescription> Importers;
        internal readonly IList<ProcessorDescription> Processors;
        
        public RelayCommand AddCollectionItemCommand { get; private set; }
        public RelayCommand RemoveCollectionItemCommand { get; private set; }

        public ReferencesViewModel(ISiteViewModel site, PipelineProjectViewModel pipelineProject)
        {
            this._site = site;
            this._project = pipelineProject;

            _assemblies = new ObservableCollection<AssemblyViewModel>();
            _importers  = new ObservableCollection<ImporterDescription>();
            _processors = new ObservableCollection<ProcessorDescription>();
            Assemblies = new ReadOnlyObservableCollection<AssemblyViewModel>(_assemblies);
            Importers  = new ReadOnlyObservableCollection<ImporterDescription>(_importers);
            Processors = new ReadOnlyObservableCollection<ProcessorDescription>(_processors);

            AddCollectionItemCommand = new RelayCommand(AddCollectionItem);
            RemoveCollectionItemCommand = new RelayCommand(RemoveCollectionItem);
        }

        internal void Load()
        {
            PipelineProxyClient pipelineProxy = new PipelineProxyClient();
            pipelineProxy.BeginListening();

            pipelineProxy.SetBaseDirectory(this._project.Location);
            pipelineProxy.SetProjectFilename(Path.GetFileName(this._project.Project.OriginalPath));

            // Set Global Settings
            pipelineProxy.SetOutputDir(_project.OutputDir);
            pipelineProxy.SetIntermediateDir(_project.IntermediateDir);
            pipelineProxy.SetPlatform(_project.Platform);
            pipelineProxy.SetConfig(_project.Config); 
            pipelineProxy.SetProfile(_project.Profile);


            IProxyLogger logger = new ProxyLogger(this._project._logger);

            List<PipelineAsyncTask> addPackageTasks = new List<PipelineAsyncTask>();
            foreach (Package package in _project.Project.PackageReferences)
            {
                PipelineAsyncTask task = pipelineProxy.AddPackage(logger, package);
                addPackageTasks.Add(task);
            }
            foreach (PipelineAsyncTask task in addPackageTasks)
                task.AsyncWaitHandle.WaitOne();
            PipelineAsyncTask resolvePackagesTask = pipelineProxy.ResolvePackages(logger);
            resolvePackagesTask.AsyncWaitHandle.WaitOne();

            // load all types from references
            List<PipelineAsyncTask> addAssemblyTasks = new List<PipelineAsyncTask>();
            foreach (string refPath in _project.Project.References)
            {
                AssemblyViewModel assembly = CreateAssembly(refPath);
                if (assembly == null)
                    continue;

                string assemblyPath = assembly.NormalizedAbsoluteFullPath;

                PipelineAsyncTask task = pipelineProxy.AddAssembly(logger, assemblyPath);
                addAssemblyTasks.Add(task);

                _assemblies.Add(assembly);
            }

            foreach (PipelineAsyncTask task in addAssemblyTasks)
                task.AsyncWaitHandle.WaitOne();

            IProxyLogger logger2 = new ProxyLogger(this._project._logger);
            PipelineAsyncTask importersTask = pipelineProxy.GetImporters(logger2);
            importersTask.AsyncWaitHandle.WaitOne();
            IEnumerable<ImporterDescription> importers = importersTask.AsyncState as IEnumerable<ImporterDescription>;

            foreach (ImporterDescription importerDesc in importers)
                _importers.Add(importerDesc);

            PipelineAsyncTask processorsTask = pipelineProxy.GetProcessors(logger2);
            processorsTask.AsyncWaitHandle.WaitOne();
            IEnumerable<ProcessorDescription> processors = processorsTask.AsyncState as IEnumerable<ProcessorDescription>;

            foreach (ProcessorDescription processorDesc in processors)
                _processors.Add(processorDesc);

            pipelineProxy.Dispose();
        }
        
        private AssemblyViewModel CreateAssembly(string refPath)
        {
            AssemblyViewModel assembly = new AssemblyViewModel(_project, this, refPath);

            // check if assembly is allready added
            foreach (AssemblyViewModel otherAssembly in _assemblies)
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
        
        internal TypeConverter FindConverter(Type type)
        {
            if (type == typeof(Microsoft.Xna.Framework.Color))
                return new StringToColorConverter();

            return TypeDescriptor.GetConverter(type);
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

                foreach (ProcessorDescription processorDesc in Processors)
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


        private void AddCollectionItem(object parameter)
        {
            System.Windows.Controls.ListBox listBox = parameter as System.Windows.Controls.ListBox;

            if (listBox.ItemsSource is ReferencesViewModel)
            {
                // TODO: is referencesViewModel1 == this ?
                ReferencesViewModel referencesViewModel1 = listBox.ItemsSource as ReferencesViewModel;

                // select file and create an AssemblyViewModel
                Win32.OpenFileDialog ofd = new Win32.OpenFileDialog();
                ofd.Filter = ".Net assembly files (*.dll)|*.dll|All files (*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.AddExtension = true;
                ofd.FileName = String.Empty;
                ofd.InitialDirectory = this._project.Location;
                if (ofd.ShowDialog() != true)
                    return;
                string fdFilenameResult = ofd.FileName;

                string refPath = fdFilenameResult;

                AssemblyViewModel assembly = CreateAssembly(refPath);
                if (assembly == null)
                    return;

                _site.Controller.EnqueueAndExecute(new AssemblyAddCmd(this, assembly));
            }

            return;
        }

        private void RemoveCollectionItem(object parameter)
        {
            System.Windows.Controls.ListBox listBox = parameter as System.Windows.Controls.ListBox;

            if (listBox.ItemsSource is ReferencesViewModel)
            {
                // TODO: is referencesViewModel1 == this ?
                ReferencesViewModel referencesViewModel1 = listBox.ItemsSource as ReferencesViewModel;

                if (listBox.SelectedIndex == -1) return;
                if (listBox.SelectedIndex >= _assemblies.Count) return;

                _site.Controller.EnqueueAndExecute(new AssemblyRemoveCmd(this, listBox.SelectedIndex));

                return;
            }

            return;
        }


        #region implement IList

        int IList.Add(object value)
        {
            AssemblyViewModel assembly = (AssemblyViewModel)value;
            int newIndex = _assemblies.Count;

            using (PipelineProxyClient pipelineProxy = new PipelineProxyClient())
            {
                pipelineProxy.BeginListening();

                pipelineProxy.SetBaseDirectory(this._project.Location);
                pipelineProxy.SetProjectFilename(Path.GetFileName(this._project.Project.OriginalPath));

                IProxyLogger logger = new ProxyLogger(this._project._logger);
                PipelineAsyncTask task = pipelineProxy.AddAssembly(logger, assembly.NormalizedAbsoluteFullPath);
                WaitHandle.WaitAll(new[] { task.AsyncWaitHandle });

                _assemblies.Add(assembly);
                _project.Project.References.Add(assembly.OriginalPath); // update model

                PipelineAsyncTask importersTask = pipelineProxy.GetImporters(logger);
                importersTask.AsyncWaitHandle.WaitOne();
                IEnumerable<ImporterDescription> importers = importersTask.AsyncState as IEnumerable<ImporterDescription>;

                foreach (ImporterDescription importerDesc in importers)
                    if (importerDesc.AssemblyPath == assembly.NormalizedAbsoluteFullPath)
                        _importers.Add(importerDesc);

                PipelineAsyncTask processorsTask = pipelineProxy.GetProcessors(logger);
                processorsTask.AsyncWaitHandle.WaitOne();
                IEnumerable<ProcessorDescription> processors = processorsTask.AsyncState as IEnumerable<ProcessorDescription>;

                    foreach (ProcessorDescription processorDesc in processors)
                    if (processorDesc.AssemblyPath == assembly.NormalizedAbsoluteFullPath)
                        _processors.Add(processorDesc);
            }

            var handler = CollectionChanged;
            if (handler!=null)
                handler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, assembly, newIndex));
            
            return newIndex;
        }

        void IList.Clear()
        {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value)
        {
            return _assemblies.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _assemblies.IndexOf((AssemblyViewModel)value);
        }

        void IList.Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        bool IList.IsFixedSize
        {
            get { throw new NotImplementedException(); }
        }

        bool IList.IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        void IList.Remove(object value)
        {
            throw new NotImplementedException();
        }

        void IList.RemoveAt(int index)
        {
            AssemblyViewModel oldasm = _assemblies[index];
            _assemblies.RemoveAt(index);
            _project.Project.References.RemoveAt(index); // update model

            var handler = CollectionChanged;
            if (handler != null)
                handler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldasm, index));
            
            return;
        }

        object IList.this[int index]
        {
            get { return _assemblies[index]; }
            set
            {
                throw new NotImplementedException();
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            throw new NotImplementedException();
        }

        int ICollection.Count
        {
            get { return _assemblies.Count; }
        }

        bool ICollection.IsSynchronized
        {
            get { throw new NotImplementedException(); }
        }

        object ICollection.SyncRoot
        {
            get { throw new NotImplementedException(); }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _assemblies.GetEnumerator();
        }
        #endregion


        #region implement IList
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion
        
    }
}
