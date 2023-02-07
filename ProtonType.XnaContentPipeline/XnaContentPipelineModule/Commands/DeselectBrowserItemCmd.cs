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

using tainicom.ProtonType.Framework.Commands;
using tainicom.ProtonType.ViewModels;
using nkast.ProtonType.XnaContentPipeline.ViewModels;

namespace nkast.ProtonType.XnaContentPipeline.Commands
{
    internal class DeselectBrowserItemCmd : CommandBase<FileBrowserEx>
    {
        BrowserItem item;

        public DeselectBrowserItemCmd(FileBrowserEx receiver, BrowserItem item): base(receiver)
        {
            this.item = item;
        }

        public override void Execute()
        {
            Receiver.__DeselectBrowserItem(item);
        }

        public override void Undo()
        {
            Receiver.__SelectBrowserItem(item);
        }
    }
}
