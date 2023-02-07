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

using tainicom.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.Builder.ViewModels
{
    public class PipelineBuildItemViewModel : BaseViewModel
    {
        private Models.PipelineProjectBuilder.PipelineBuildItem _builditem;

        public TaskResult TaskResult;

        internal Models.PipelineProjectBuilder.PipelineBuildItem Builditem { get { return _builditem; } }

        public string Name
        { 
            get 
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(_builditem.PipelineItem.DestinationPath);                
                return name;
            } 
        }

        public string Status { get { return _builditem.Status.ToString(); } }


        internal PipelineBuildItemViewModel(Models.PipelineProjectBuilder.PipelineBuildItem builditem)
        {
            this._builditem = builditem;
            this._builditem.StatusChanged += _builditem_StatusChanged;
        }

        void _builditem_StatusChanged(object sender, System.EventArgs e)
        {
            RaisePropertyChanged(() => Status);
        }
    }
}
