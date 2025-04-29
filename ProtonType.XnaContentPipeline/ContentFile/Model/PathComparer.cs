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

using System;
using System.Collections.Generic;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    internal class PathComparer : IComparer<string>
    {
        private readonly StringComparer _stringComparer = StringComparer.Ordinal;

        public int Compare(string x, string y)
        {
            var sx = x.Split('/', '\\');
            var sy = y.Split('/', '\\');

            int minLength = Math.Min(sx.Length, sy.Length);
            for (int i = 0; i < minLength-1; i++)
            {
                int cmp = _stringComparer.Compare(sx[i], sy[i]);
                if (cmp != 0)
                    return cmp;
            }

            if (sx.Length > sy.Length)
                    return -1;
            if (sy.Length > sx.Length)
                    return 1;

            return _stringComparer.Compare(sx[minLength - 1], sy[minLength - 1]);
        }
    }
}