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
using tainicom.ProtonType.Framework.ViewModels;
using tainicom.ProtonType.XnaContentPipeline.Common;

namespace tainicom.ProtonType.XnaContentPipeline.ViewModels
{
    internal class AssemblyViewModel : BaseViewModel
    {
        private PipelineProjectViewModel _project;
        private readonly ReferencesViewModel _references;
        public readonly string OriginalPath;
        public readonly bool IsFromCurrentDomain;

        public string AbsoluteFullPath
        {
            get
            {
                if (_project == null)
                    return OriginalPath;

                var projectRoot = _project.Location;
                return Path.Combine(projectRoot, OriginalPath);
            }
        }

        public string NormalizedAbsoluteFullPath
        {
            get { return PathHelper.Normalize(AbsoluteFullPath); }
        }
        

        public AssemblyViewModel(PipelineProjectViewModel pipelineProject, ReferencesViewModel referencesViewModel, string refPath)
        {
            this._project = pipelineProject;
            this._references = referencesViewModel;
            this.OriginalPath = refPath;
        }

        public AssemblyViewModel(string path, bool isFromCurrentDomain = true)
        {
            if (path == null)
                throw new ArgumentNullException("path");
            if (!Path.IsPathRooted(path))
                throw new InvalidOperationException("path must be rooted");

            this.IsFromCurrentDomain = isFromCurrentDomain;
            this.OriginalPath = path;
        }
        
        public override string ToString()
        {
            return OriginalPath;
        }


    }
}
