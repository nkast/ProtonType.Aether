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
using System.Collections.Generic;
using Microsoft.Xna.Framework.Content.Pipeline;
using tainicom.ProtonType.Logger.Contracts;
using tainicom.ProtonType.XnaContentPipeline.Common;

namespace tainicom.ProtonType.XnaContentPipeline.ViewModels
{
    public partial class ContentPipelineViewModel : IPipelineLogger
    {
        void IPipelineLogger.LogMessage(string message)
        {
            foreach (IModuleLogger logger in Module.Site.GetModules<IModuleLogger>())
            {
                logger.LogMessage(Module, message);
            }
        }

        void IPipelineLogger.LogWarning(string message, string filename, ContentIdentity contentIdentity)
        {
            // extract errorCode from message
            var errorCode = String.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(message, @"([A-Z]+[0-9]+):(.+)");
            if (match.Success || match.Groups.Count == 2)
            {
                errorCode = match.Groups[1].Value + " ";
                message = match.Groups[2].Value;
            }

            string fragmentIdentifier = null;
            if (contentIdentity != null)
                fragmentIdentifier = contentIdentity.FragmentIdentifier;

            foreach (IModuleLogger logger in Module.Site.GetModules<IModuleLogger>())
            {
                logger.LogWarning(Module, errorCode, message, filename, fragmentIdentifier);
            }
        }

        void IPipelineLogger.LogError(string message, string filename, ContentIdentity contentIdentity)
        {
            // extract errorCode from message
            var errorCode = String.Empty;
            var match = System.Text.RegularExpressions.Regex.Match(message, @"([A-Z]+[0-9]+):(.+)");
            if (match.Success || match.Groups.Count == 2)
            {
                errorCode = match.Groups[1].Value + " ";
                message = match.Groups[2].Value;
            }

            string fragmentIdentifier = null;
            if (contentIdentity != null)
                fragmentIdentifier = contentIdentity.FragmentIdentifier;

            foreach (IModuleLogger logger in Module.Site.GetModules<IModuleLogger>())
            {
                logger.LogError(Module, errorCode, message, filename, fragmentIdentifier);
            }
        }

    }
}
