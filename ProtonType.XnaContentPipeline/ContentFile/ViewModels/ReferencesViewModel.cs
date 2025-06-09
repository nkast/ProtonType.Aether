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
using System.Threading;
using System.Threading.Tasks;
using nkast.ProtonType.Framework.Commands;
using nkast.ProtonType.Framework.Helpers;
using nkast.ProtonType.Framework.Modules;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;
using Win32 = Microsoft.Win32;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public class ReferencesViewModel : BaseViewModel, IList, IReceiver, INotifyCollectionChanged
    {
        private readonly ISiteViewModel _site;
        private PipelineProjectViewModel _project;

        internal readonly ObservableCollection<AssemblyViewModel> _assemblies = new ObservableCollection<AssemblyViewModel>();
         
        internal readonly IList<AssemblyViewModel> Assemblies;
        
        public RelayCommand AddCollectionItemCommand { get; private set; }
        public RelayCommand RemoveCollectionItemCommand { get; private set; }

        public ReferencesViewModel(ISiteViewModel site, PipelineProjectViewModel pipelineProject)
        {
            this._site = site;
            this._project = pipelineProject;

            Assemblies = new ReadOnlyObservableCollection<AssemblyViewModel>(_assemblies);
        
            AddCollectionItemCommand = new RelayCommand(AddCollectionItem);
            RemoveCollectionItemCommand = new RelayCommand(RemoveCollectionItem);
        }
        
        private void AddCollectionItem(object parameter)
        {
            System.Windows.Controls.ListBox listBox = parameter as System.Windows.Controls.ListBox;

            if (listBox.ItemsSource is ReferencesViewModel)
            {
                // TODO: is referencesViewModel1 == this ?
                ReferencesViewModel referencesViewModel1 = listBox.ItemsSource as ReferencesViewModel;

                string location = this._project.DocumentFile;
                if (string.IsNullOrEmpty(location))
                    location = "";
                else
                    location = Path.GetDirectoryName(location);

                // select file and create an AssemblyViewModel
                Win32.OpenFileDialog ofd = new Win32.OpenFileDialog();
                ofd.Filter = ".Net assembly files (*.dll)|*.dll|All files (*.*)|*.*";
                ofd.FilterIndex = 1;
                ofd.AddExtension = true;
                ofd.FileName = String.Empty;
                ofd.InitialDirectory = location;
                if (ofd.ShowDialog() != true)
                    return;
                string fdFilenameResult = ofd.FileName;

                string refPath = fdFilenameResult;

                AssemblyViewModel assembly = _project.CreateAssembly(refPath);
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

            _project.AddReference(assembly);

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


        #region implement INotifyCollectionChanged
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion
        
    }
}
