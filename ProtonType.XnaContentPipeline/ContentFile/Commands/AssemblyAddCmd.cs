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
using nkast.ProtonType.Framework.Commands;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    internal class AssemblyAddCmd : CommandBase
    {
        private AssemblyViewModel _item;
        private int _prevIndex;
        
        public AssemblyAddCmd(ReferencesViewModel receiver, AssemblyViewModel item) : base(receiver)
        {
            this._item = item;
        }

        public override void Execute()
        {
            _prevIndex = ((IList)Receiver).Add(_item);
        }

        public override void Undo()
        {
            var prevItem = ((IList)Receiver)[_prevIndex];
            System.Diagnostics.Debug.Assert(prevItem == _item);
            ((IList)Receiver).RemoveAt(_prevIndex);
        }
    }
}
