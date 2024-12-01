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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using nkast.ProtonType.Framework.Commands;
using nkast.ProtonType.Framework.Helpers;
using nkast.ProtonType.Framework.Modules;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;
using Win32 = Microsoft.Win32;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class PackagesViewModel : BaseViewModel, IList, IReceiver, INotifyCollectionChanged
    {
        private readonly ISiteViewModel _site;
        private PipelineProjectViewModel _project;

        private readonly ObservableCollection<PackageViewModel> _packages;
        
        internal readonly IList<PackageViewModel> Packages;
        
        public RelayCommand AddCollectionItemCommand { get; private set; }
        public RelayCommand RemoveCollectionItemCommand { get; private set; }

        public PackagesViewModel(ISiteViewModel site, PipelineProjectViewModel pipelineProject)
        {
            this._site = site;
            this._project = pipelineProject;

            _packages = new ObservableCollection<PackageViewModel>();
            Packages = new ReadOnlyObservableCollection<PackageViewModel>(_packages);

            AddCollectionItemCommand = new RelayCommand(AddCollectionItem);
            RemoveCollectionItemCommand = new RelayCommand(RemoveCollectionItem);
        }

        internal void Load()
        {
            foreach (Package package in _project.Project.PackageReferences)
            {
                PackageViewModel packageVM = CreatePackage(package);
                if (packageVM == null)
                    continue;

                _packages.Add(packageVM);
            }
        }

        private PackageViewModel CreatePackage(Package package)
        {
            string packageName = package.Name;

            PackageViewModel packageVM = new PackageViewModel(_project, this, package);

            // check if assembly is allready added
            foreach (PackageViewModel otherPackage in _packages)
            {
                if (otherPackage.PackageName == packageVM.PackageName
                &&  otherPackage.PackageVersion == packageVM.PackageVersion)
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
        
        private void AddCollectionItem(object parameter)
        {
            System.Windows.Controls.ListBox listBox = parameter as System.Windows.Controls.ListBox;

            if (listBox.ItemsSource is PackagesViewModel)
            {
                // TODO: is packagesViewModel1 == this ?
                PackagesViewModel packagesViewModel1 = listBox.ItemsSource as PackagesViewModel;

                // input package and create an PackageViewModel
                string packageInput = Microsoft.VisualBasic.Interaction.InputBox("Package [version]", "Package", "");
                if (string.IsNullOrWhiteSpace(packageInput))
                    return;

                Package package = default;
                package.Name = packageInput;
                package.Version = String.Empty;

                Match match = Regex.Match(package.Name,
                    @"(dotnet add package )?(?<Name>[^\s]+)(\s+--version)?(\s+(?<VersionNumber>[^\s]+))");
                if (match.Success)
                {
                    package.Name = match.Groups["Name"].Value;
                    if (package.Version == String.Empty && match.Groups["VersionNumber"].Success)
                        package.Version = match.Groups["VersionNumber"].Value;
                }

                PackageViewModel packageVM = CreatePackage(package);
                if (packageVM == null)
                    return;

                _site.Controller.EnqueueAndExecute(new PackageAddCmd(this, packageVM));
            }

            return;
        }

        private void RemoveCollectionItem(object parameter)
        {
            System.Windows.Controls.ListBox listBox = parameter as System.Windows.Controls.ListBox;

            if (listBox.ItemsSource is PackagesViewModel)
            {
                // TODO: is packagesViewModel1 == this ?
                PackagesViewModel packagesViewModel1 = listBox.ItemsSource as PackagesViewModel;

                if (listBox.SelectedIndex == -1) return;
                if (listBox.SelectedIndex >= _packages.Count) return;

                _site.Controller.EnqueueAndExecute(new PackageRemoveCmd(this, listBox.SelectedIndex));

                return;
            }

            return;
        }


        #region implement IList

        int IList.Add(object value)
        {
            PackageViewModel packageVM = (PackageViewModel)value;
            int newIndex = _packages.Count;

            _packages.Add(packageVM);
            _project.Project.PackageReferences.Add(packageVM.Package); // update model

            var handler = CollectionChanged;
            if (handler!=null)
                handler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, packageVM, newIndex));
            
            return newIndex;
        }

        void IList.Clear()
        {
            throw new NotImplementedException();
        }

        bool IList.Contains(object value)
        {
            return _packages.Contains(value);
        }

        int IList.IndexOf(object value)
        {
            return _packages.IndexOf((PackageViewModel)value);
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
            PackageViewModel oldasm = _packages[index];
            _packages.RemoveAt(index);
            _project.Project.PackageReferences.RemoveAt(index); // update model

            var handler = CollectionChanged;
            if (handler != null)
                handler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldasm, index));
            
            return;
        }

        object IList.this[int index]
        {
            get { return _packages[index]; }
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
            get { return _packages.Count; }
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
            return _packages.GetEnumerator();
        }
        #endregion


        #region implement IList
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion
        
    }
}
