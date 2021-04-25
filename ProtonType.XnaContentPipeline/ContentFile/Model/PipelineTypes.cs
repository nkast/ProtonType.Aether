// MonoGame - Copyright (C) The MonoGame Team
// This file is subject to the terms and conditions defined in
// file 'LICENSE.txt', which is part of this source code package.

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

namespace tainicom.ProtonType.XnaContentPipeline.Common
{
    internal class PipelineTypes
    {
        public static ImporterDescription NullImporter { get; private set; }
        public static ProcessorDescription NullProcessor { get; private set; }

        public static ImporterDescription MissingImporter { get; private set; }
        public static ProcessorDescription MissingProcessor { get; private set; }

        static PipelineTypes()
        {
            MissingImporter = new ImporterDescription(
                            assemblyPath: null,
                            typeName: null,
                            typeFullName: null,
                            outputTypeName: null,
                            outputTypeFullName: null,
                            outputBaseTypesFullName: new string[] { },
                            displayName: "Invalid / Missing Importer",
                            defaultProcessor: null,
                            fileExtensions: null
                            );

            MissingProcessor = new ProcessorDescription(
                            assemblyPath: null,
                            typeName: null,
                            typeFullName: null,
                            inputTypeFullName: null,
                            inputBaseTypesFullName: new string[] {},
                            outputTypeFullName: null,
                            processorParams: new ProcessorParamDescription[0],
                            displayName: "Invalid / Missing Processor"
                            );

            NullImporter = new ImporterDescription(
                            assemblyPath: null,
                            typeName: null,
                            typeFullName: null,
                            outputTypeName: null,
                            outputTypeFullName: null,
                            outputBaseTypesFullName: new string[] { },
                            displayName: "",
                            defaultProcessor: null,
                            fileExtensions: null
                            );

            NullProcessor = new ProcessorDescription(
                            assemblyPath: null,
                            typeName: null,
                            typeFullName: null,
                            inputTypeFullName: null,
                            inputBaseTypesFullName: new string[] { },
                            outputTypeFullName: null,
                            processorParams: new ProcessorParamDescription[0],
                            displayName: ""
                            );
        }

    }
        
}
