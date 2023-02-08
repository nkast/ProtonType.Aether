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
using nkast.ProtonType.Framework.Commands;
using nkast.ProtonType.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public partial class FileBrowserEx : IReceiver
    {
        ObservableCollection<BrowserItem> _selectedItems = new ObservableCollection<BrowserItem>();
        ReadOnlyObservableCollection<BrowserItem> _readonlySelectedItems = null;
        public ReadOnlyObservableCollection<BrowserItem> SelectedItems
        {
            get
            {
                if (_readonlySelectedItems == null) _readonlySelectedItems = new ReadOnlyObservableCollection<BrowserItem>(this._selectedItems);
                return _readonlySelectedItems;
            }
        }

        internal void __SelectBrowserItem(BrowserItem item)
        {
            if (!_selectedItems.Contains(item))
            {
                _selectedItems.Add(item);
                OnSelectionChanged(EventArgs.Empty);
            }
        }

        internal void __DeselectBrowserItem(BrowserItem item)
        {
            if (_selectedItems.Contains(item))
            {
                _selectedItems.Remove(item);
                OnSelectionChanged(EventArgs.Empty);
            }
        }

        internal void __IncludeBrowserItem(BrowserItemEx item, PipelineItemViewModel pipelineItemVM)
        {
            if (pipelineItemVM == null)
            {
                // create new pipelineItem
                string fileAbsolutePath = ContentPipelineViewModel.GetAbsolutePath(item.RelativePath);
                pipelineItemVM = ContentPipelineViewModel.PipelineProjectViewModel.Include(fileAbsolutePath);
                this.ContentPipelineViewModel.AddItem(pipelineItemVM);
            }
            else
            {
                // set pipelineItem
                this.ContentPipelineViewModel.PipelineProjectViewModel.Add(pipelineItemVM);
            }

            // attach pipelineItem to browserItem
            item.ProjectItemVM = pipelineItemVM;

            // rebuild item
            bool rebuild = true;
            this.ContentPipelineViewModel.RebuildItem(pipelineItemVM, rebuild);
            OnSelectionChanged(EventArgs.Empty);
        }

        internal PipelineItemViewModel __ExcludeBrowserItem(BrowserItemEx item)
        {
            var pipelineItemVM = item.ProjectItemVM as PipelineItemViewModel;
            if (pipelineItemVM == null) return null;

            this.ContentPipelineViewModel.PipelineProjectViewModel.Remove(pipelineItemVM);
            item.ProjectItemVM = null;

            // clean item
            this.ContentPipelineViewModel.CleanItem(pipelineItemVM);

            OnSelectionChanged(EventArgs.Empty);

            return pipelineItemVM;
        }
                
        #region Events
        public event EventHandler<EventArgs> SelectionChanged;
        protected virtual void OnSelectionChanged(EventArgs e)
        {
            var handler = SelectionChanged;
                if (handler == null) return;
            handler(this, e);
        }
        #endregion
        
    }
}
