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

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    public enum ProxyMsgType : int
    {
        Undefined,
        BaseDirectory,
        ProjectFilename,
        AddPackage,
        ResolvePackages,
        AddAssembly,
        GetImporters,
        GetProcessors,
        Importer,
        Processor,
        End,
        TaskEnd,
        Terminate,

        // Logger
        LogMessage,
        LogImportantMessage,
        LogWarning,
        LogError,

        // Global Parameters
        ParamRebuild,
        ParamIncremental,

        ParamOutputDir,
        ParamIntermediateDir,
        ParamPlatform,
        ParamConfig,
        ParamProfile,
        ParamCompression,

        // Item parameters
        ParamImporter,
        ParamProcessor,
        ParamProcessorParam,
        AddItem,

        // Build action
        Copy,
        Build,
    }
}
