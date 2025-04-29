#region License
//   Copyright 2025 Kastellanos Nikolaos
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
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    internal class PipelineItemPathComparer : IComparer<PipelineItem>
    {
        private readonly PathComparer _pathComparer = new PathComparer();

        public int Compare(PipelineItem x, PipelineItem y)
        {
            return _pathComparer.Compare(x.OriginalPath, y.OriginalPath);
        }
    }
}