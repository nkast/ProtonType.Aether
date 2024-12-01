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

using System.Collections;
using nkast.ProtonType.Framework.Commands;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    internal class PackageRemoveCmd : CommandBase
    {
        private int _index;
        private PackageViewModel _prevItem;
        
        public PackageRemoveCmd(PackagesViewModel receiver, int index) : base(receiver)
        {
            this._index = index;
        }

        public override void Execute()
        {
            _prevItem = (PackageViewModel)((IList)Receiver)[_index];
            ((IList)Receiver).RemoveAt(_index);
        }

        public override void Undo()
        {
            ((IList)Receiver).Insert(_index, _prevItem);
        }
    }
}
