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
using System.Linq;
using tainicom.ProtonType.Framework.Helpers;
using tainicom.ProtonType.Framework.Modules;
using tainicom.ProtonType.XnaContentPipeline.Contracts;

namespace tainicom.ProtonType.XnaContentPipeline 
{
    public partial class XnaContentPipelineModule : NotificationObject, IModule, IModuleUI, IXnaContentPipelineModule
    {
        private ISiteViewModel _site;

        #region IModule Members

        public void Initialize(ISiteViewModel site)
        {
            _site = site;
        }

        #endregion IModule Members

        #region IModuleUI Members
        ObservableCollection<Framework.ViewModels.MenuViewModel> _menus = new ObservableCollection<Framework.ViewModels.MenuViewModel>();
        ObservableCollection<Framework.ViewModels.ToolbarViewModel> _toolbars = new ObservableCollection<Framework.ViewModels.ToolbarViewModel>();
        internal ObservableCollection<Framework.ViewModels.StatusBarItemViewModel> _statusbars = new ObservableCollection<tainicom.ProtonType.Framework.ViewModels.StatusBarItemViewModel>();
        IEnumerable<Framework.ViewModels.MenuViewModel> IModuleUI.Menus { get { return _menus; } }
        IEnumerable<Framework.ViewModels.ToolbarViewModel> IModuleUI.Toolbars { get { return _toolbars; } }
        IEnumerable<Framework.ViewModels.StatusBarItemViewModel> IModuleUI.StatusBars { get { return _statusbars; } }
        #endregion IModuleUI Members
        

        internal ISiteViewModel Site { get { return _site; } }  

    }
}
