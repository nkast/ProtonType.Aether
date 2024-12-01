#region License
//   Copyright 2024 Kastellanos Nikolaos
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

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    public struct Package : IComparable<Package>
    {
        public string Name;
        public string Version;

        public static Package Parse(string packageReference)
        {
            packageReference.Trim();

            Package package;
            package.Name = packageReference;
            package.Version = String.Empty;

            string[] split = packageReference.Split(' ');
            if (split.Length == 2)
            {
                package.Name = split[0].Trim();
                package.Version = split[1].Trim();
            }

            return package;
        }

        public override string ToString()
        {
            string result = this.Name;
            if (this.Version != String.Empty)
                result += " " + this.Version;

            return result;
        }

        int IComparable<Package>.CompareTo(Package other)
        {
            int compName = this.Name.CompareTo(other.Name);
            if (compName != 0)
                return compName;

            int compVersion = this.Version.CompareTo(other.Version);
            if (compVersion != 0)
                return compVersion;

            return 0;
        }
    }
}