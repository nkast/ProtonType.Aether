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

using nkast.ProtonType.Framework.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.Builder.ViewModels
{
    public class PipelineBuilderStatusBarItemViewModel : StatusBarItemViewModel
    {
        private PipelineBuilderViewModel _pipelineBuilder;
        
        public int QueuedCount
        {
            get { return _pipelineBuilder.QueuedCount; }
        }

        public PipelineBuilderStatusBarItemViewModel(PipelineBuilderViewModel pipelineBuilder)
            : base("PipelineBuilder")
        {
            this._pipelineBuilder = pipelineBuilder;

            pipelineBuilder.PropertyChanged += pipelineBuilder_PropertyChanged;
                     
        }

        void pipelineBuilder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "QueuedCount")
            {
                RaisePropertyChanged(()=>QueuedCount);
            }
        }

    }
}
