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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    public interface IPipelineBuilder
    {
        void SetBaseDirectory(string location);
        void SetProjectName(string projectName);

        void SetOutputDir(string outputDir);
        void SetIntermediateDir(string intermediateDir);
        void SetPlatform(TargetPlatform platform);
        void SetConfig(string config);
        void SetProfile(GraphicsProfile profile);
        void SetCompression(ContentCompression compression);

        void SetRebuild();
        void SetIncremental();

        Task AddPackageAsync(IProxyLoggerBase logger, Package package);
        Task ResolvePackagesAsync(IProxyLoggerBase logger);
        Task AddAssemblyAsync(IProxyLoggerBase logger, string assemblyPath);

        Task<List<ImporterDescription>> GetImportersAsync(IProxyLoggerBase logger);
        Task<List<ProcessorDescription>> GetProcessorsAsync(IProxyLoggerBase logger);

        void SetImporter(string importer);
        void SetProcessor(string processor);
        void AddProcessorParam(string processorParamName, string processorParamValue);

        Task CleanItemsAsync(IProxyLoggerBase logger);

        Task BuildEndAsync(IProxyLoggerBase logger);
        Task BuildBeginAsync(IProxyLoggerBase logger);

    }
}
