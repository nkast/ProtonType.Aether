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
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    /*internal*/ public class PipelineProject : IPipelineItem
    {
        IList<PipelineItem> _pipelineItems = new List<PipelineItem>();


        public IList<PipelineItem> PipelineItems { get; private set; }

        public string OutputDir { get; set; }

        public string IntermediateDir { get; set; }

        public List<string> References { get; set; }

        public List<Package> PackageReferences { get; set; }

        public ProxyTargetPlatform Platform { get; set; }

        public ProxyGraphicsProfile Profile { get; set; }

        public string Config { get; set; }

        public bool Compress { get; set; }

        /// <summary>
        /// Gets or sets the compression method.
        /// </summary>
        public CompressionMethod Compression { get; set; }

        #region IPipelineItem

        public string Filename
        {
            get
            {
                if (string.IsNullOrEmpty(OriginalPath))
                    return "";

                return System.IO.Path.GetFileNameWithoutExtension(OriginalPath);
            }
        }

        public string Location
        {
            get
            {
                string location = OriginalPath;
                if (string.IsNullOrEmpty(location))
                {
                    location = "";
                    return location;
                }
                else
                {
                    int idx = location.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, location.Length - 1);
                    return location.Remove(idx);
                }
            }
        }

        public string OriginalPath 
        {
            get;
            set;
        }

        #endregion

        public PipelineProject()
        {
            PipelineItems = new ReadOnlyCollection<PipelineItem>(_pipelineItems);
            
            References = new List<string>();
            PackageReferences = new List<Package>();

            OutputDir = "../Content";
            IntermediateDir = "obj";
        }

        internal void AddItem(PipelineItem item, int index = -1)
        {
            System.Diagnostics.Debug.Assert(item.Project == null);

            if (index == -1)
                _pipelineItems.Add(item);
            else
                _pipelineItems.Insert(index, item);

            item.Project = this;
            item.PropertyChanged += item_PropertyChanged;
        }

        internal void RemoveItem(PipelineItem item)
        {
            System.Diagnostics.Debug.Assert(item.Project == this);

            item.Project = null;
            _pipelineItems.Remove(item);
            item.PropertyChanged -= item_PropertyChanged;
        }

        internal void ClearItems()
        {
            foreach (var item in _pipelineItems)
                item.PropertyChanged -= item_PropertyChanged;

            _pipelineItems.Clear();
        }
        
        /*internal*/ public string ReplaceSymbols(string parameter)
        {
            if (string.IsNullOrWhiteSpace(parameter))
                return parameter;
            return parameter
                .Replace("$(Platform)", Platform.ToString())
                .Replace("$(Configuration)", Config)
                .Replace("$(Config)", Config)
                .Replace("$(Profile)", Profile.ToString());
        }

        void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pipelineItem = (PipelineItem)sender;
            OnPipelineItemPropertyChanged(new PipelineItemPropertyChangedEventArgs(pipelineItem, e.PropertyName));
        }
        
        #region observable / INotifyPropertyChanged
        public event EventHandler<PipelineItemPropertyChangedEventArgs> PipelineItemPropertyChanged;
        protected virtual void OnPipelineItemPropertyChanged(PipelineItemPropertyChangedEventArgs args)
        {
            var handler = PipelineItemPropertyChanged;
            if (handler == null) return;
            handler(this, args);
        }

        #endregion


        internal int BinarySearch(PipelineItem pipelineItem, IComparer<PipelineItem> pipelineItemComparer)
        {
            int index = ((List<PipelineItem>)_pipelineItems).BinarySearch(pipelineItem, pipelineItemComparer);
            return index;
        }
    }
}
