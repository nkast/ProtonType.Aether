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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    internal class AssertListener : DefaultTraceListener
    {
        public override void Fail(string message)
        {
            if (message == null)
                message = "";

            throw new Exception("Debug assertion failed: " + message);
        }

        public override void Fail(string message, string detailMessage)
        {
            if (message == null)
                message = "";
            if (detailMessage == null)
                detailMessage = "";

            throw new Exception("Debug assertion failed: " + message + "\n" + detailMessage);
        }
    }
}
