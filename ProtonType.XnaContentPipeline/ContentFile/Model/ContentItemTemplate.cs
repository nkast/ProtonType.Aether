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

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    internal class ContentItemTemplate
    {
        public readonly string Label;
        public readonly string Icon;
        public readonly string ImporterName;
        public readonly string ProcessorName;
        public readonly string TemplateFile;

        public ContentItemTemplate(string label, string icon, string importerName, string processorName, string templateFile)
        {
            this.Label = label;
            this.Icon = icon;
            this.ImporterName = importerName;
            this.ProcessorName = processorName;
            this.TemplateFile = templateFile;
        }

        public override string ToString()
        {
            return Label;
        }
    }
}