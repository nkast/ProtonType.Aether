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
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    public class ProxyLogger : IProxyLogger
    {
        private IPipelineLogger _viewLogger;

        public ProxyLogger(IPipelineLogger viewLogger)
        {
            this._viewLogger = viewLogger;
        }

        void IProxyLogger.LogImportantMessage(string filename, string message)
        {
            _viewLogger.LogMessage(message);
        }

        void IProxyLogger.LogMessage(string filename, string message)
        {
            _viewLogger.LogMessage(message);
        }

        void IProxyLogger.LogWarning(string filename, string helpLink, ContentIdentity contentIdentity, string message)
        {
            _viewLogger.LogWarning(message, filename, contentIdentity);
        }

        void IProxyLogger.LogError(string filename, ContentIdentity contentIdentity, string message)
        {
            _viewLogger.LogError(message, filename, contentIdentity);
        }
        
    }
}
