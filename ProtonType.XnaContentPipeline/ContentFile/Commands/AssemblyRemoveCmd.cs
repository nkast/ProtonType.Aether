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

using System.Collections;
using tainicom.ProtonType.Framework.Commands;

namespace tainicom.ProtonType.XnaContentPipeline.ViewModels
{
    internal class AssemblyRemoveCmd : CommandBase
    {
        private int _index;
        private AssemblyViewModel _prevItem;
        
        public AssemblyRemoveCmd(ReferencesViewModel receiver, int index) : base(receiver)
        {
            this._index = index;
        }

        public override void Execute()
        {
            _prevItem = (AssemblyViewModel)((IList)Receiver)[_index];
            ((IList)Receiver).RemoveAt(_index);
        }

        public override void Undo()
        {
            ((IList)Receiver).Insert(_index, _prevItem);
        }
    }
}
