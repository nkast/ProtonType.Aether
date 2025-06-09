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
using System.Diagnostics;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    public class BuildLogger : ContentBuildLogger, IProxyLoggerBase
    {
        private readonly PipelineProxyServer _proxyServer;
        private readonly Guid ContextGuid;

        internal BuildLogger(PipelineProxyServer proxyServer, Guid contextGuid)
        {
            this._proxyServer = proxyServer;
            this.ContextGuid = contextGuid;
        }

        public override void LogMessage(string message, params object[] messageArgs)
        {
            string msg = string.Format(message, messageArgs);
            Trace.WriteLine(msg);
            _proxyServer.LogMessage(ContextGuid, msg);
        }

        public override void LogImportantMessage(string message, params object[] messageArgs)
        {
            string msg = string.Format(message, messageArgs);
            Trace.WriteLine(msg);
            _proxyServer.LogImportantMessage(ContextGuid, msg);
        }

        public override void LogWarning(string helpLink, ContentIdentity contentIdentity, string message, params object[] messageArgs)
        {
            string currentFilename = GetCurrentFilename(contentIdentity);
            string msg = string.Format(message, messageArgs);
            Trace.WriteLine(msg);
            _proxyServer.LogWarning(ContextGuid, currentFilename, helpLink, contentIdentity, msg);
        }
    }
}