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
using System.IO;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    internal class PackageViewModel : BaseViewModel
    {
        private PipelineProjectViewModel _project;
        private readonly PackagesViewModel _packages;
        private readonly Package _package;

        public Package Package { get { return _package; } }

        public string PackageName { get { return _package.Name; } }
        public string PackageVersion { get { return _package.Version; } }

        public string NormalizedAbsolutePackageName { get { return PackageName; } }
        

        public PackageViewModel(PipelineProjectViewModel pipelineProject, PackagesViewModel packagesViewModel, Package package)
        {
            this._project = pipelineProject;
            this._packages = packagesViewModel;
            this._package = package;
        }

        public PackageViewModel(Package package)
        {
            if (package.Name == null)
                throw new ArgumentNullException("package.Name");

            this._package = package;
        }
        
        public override string ToString()
        {
            return Package.ToString();
        }


    }
}
