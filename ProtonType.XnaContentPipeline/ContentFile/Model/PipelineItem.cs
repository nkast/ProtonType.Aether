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

using System.Collections.Generic;
using System.ComponentModel;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    /*internal*/ public partial class PipelineItem : INotifyPropertyChanged
    {
        internal PipelineProject Project;

        public readonly IDictionary<string, string> ProcessorParams = new Dictionary<string, string>();
        private BuildAction _buildAction;

        public string Importer;
        public string Processor;

        #region IProjectItem

        [Category("Common")]
        [Description("The file name of this item.")]
        public string Filename { get { return System.IO.Path.GetFileName(OriginalPath); } }

        [Category("Common")]
        [Description("The folder where this item is located.")]
        public string Location { get { return System.IO.Path.GetDirectoryName(OriginalPath); } }
        
        [Browsable(false)]
        public string OriginalPath { get; set; }

        [Browsable(false)]
        public string DestinationPath { get; set; }

        #endregion

        [Category("Settings")]
        [DisplayName("Build Action")]
        [Description("The way to process this content item.")]
        public BuildAction BuildAction
        {
            get { return _buildAction; }
            set
            {
                if (_buildAction == value)
                    return;

                _buildAction = value;

                OnPropertyChanged(new PropertyChangedEventArgs("BuildAction"));
            }
        }

        internal PipelineItem()
        {

        } 
        
        public override string ToString()
        {
            return System.IO.Path.GetFileName(OriginalPath);
        }
        
        #region observable / INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs args)
        {
            var handler = PropertyChanged;
            if (handler == null) return;
            handler(this, args);
        }

        #endregion


    }
}
