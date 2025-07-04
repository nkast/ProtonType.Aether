﻿#region License
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

using Microsoft.Xna.Framework.Content.Pipeline;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    public interface IProxyLoggerBase
    {
    }

    public interface IProxyLogger : IProxyLoggerBase
    {
        void LogImportantMessage(string message);
        void LogMessage(string message);
        void LogWarning(string filename, string helpLink, ContentIdentity contentIdentity, string message);
        void LogError(string filename, ContentIdentity contentIdentity, string message);
    }
}
