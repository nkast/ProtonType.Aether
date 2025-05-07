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
using nkast.ProtonType.XnaContentPipeline.Builder.Models;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.Builder.ViewModels
{
    public class PipelineBuildItemViewModel : BaseViewModel
    {
        private Models.PipelineProjectBuilder.PipelineBuildItem _builditem;
        private string _name;

        internal Models.PipelineProjectBuilder.PipelineBuildItem Builditem { get { return _builditem; } }

        public string Name
        { 
            get { return _name; }
            private set 
            {
                if (value != _name)
                {
                    _name = value;
                    RaisePropertyChanged(() => Name);
                }
            }
        }

        public PipelineBuildItemStatus Status { get { return _builditem.Status; } }
        public string StatusString { get { return _builditem.Status.ToString(); } }


        internal PipelineBuildItemViewModel(Models.PipelineProjectBuilder.PipelineBuildItem builditem)
        {
            this._builditem = builditem;
            this._builditem.StatusChanged += _builditem_StatusChanged;
            this._name = System.IO.Path.GetFileNameWithoutExtension(_builditem.PipelineItem.DestinationPath);

            Builditem.ProxyItem.StateChanged += ProxyItem_StateChanged;
        }

        void _builditem_StatusChanged(object sender, System.EventArgs e)
        {
            var builditem = (Models.PipelineProjectBuilder.PipelineBuildItem)sender;

            RaisePropertyChanged(() => Status);

            // restore name
            string name = System.IO.Path.GetFileNameWithoutExtension(builditem.PipelineItem.DestinationPath);
            this.Name = name;
        }

        private void ProxyItem_StateChanged(object sender, System.EventArgs e)
        {
            ProxyClient.ProxyItem proxyItem = (ProxyClient.ProxyItem)sender;
            BuildState buildState = proxyItem.State;

            // update name with ProxyItem substates
            string name = System.IO.Path.GetFileNameWithoutExtension(_builditem.PipelineItem.DestinationPath);
            if (Status == PipelineBuildItemStatus.Building)
            {
                switch (buildState)
                {
                    case BuildState.Building:
                    // Building substates
                    case BuildState.Importing:
                        name = name + " [" + buildState.ToString() + "]";
                        break;
                    case BuildState.Processing:
                    case BuildState.Writing:
                    case BuildState.Copying:
                    case BuildState.Skipping:
                        name = name + " [" + buildState.ToString() + "]";
                        break;
                }
            }
            this.Name = name;
        }
    }
}
