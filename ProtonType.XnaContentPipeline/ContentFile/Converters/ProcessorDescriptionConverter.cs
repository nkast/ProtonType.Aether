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
using System.ComponentModel;
using System.Globalization;
using tainicom.ProtonType.XnaContentPipeline.Common;

namespace tainicom.ProtonType.XnaContentPipeline.ViewModels.Converters
{
    internal class ProcessorDescriptionConverter : TypeConverter
    {
        private PipelineProjectViewModel _pipelineProject;
        private PipelineItemViewModel _pipelineItem;

        public ProcessorDescriptionConverter(PipelineProjectViewModel pipelineProject, PipelineItemViewModel pipelineItem)
        {
            this._pipelineProject = pipelineProject;
            this._pipelineItem = pipelineItem;
        }


        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is ProcessorDescription)
                return ((ProcessorDescription)value).TypeName;

            var result = base.ConvertFrom(context, culture, value);
            return result;
        }


        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool IsValid(ITypeDescriptorContext context, object value)
        {
            return base.IsValid(context, value);
        }


        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            var result = base.GetStandardValuesSupported(context);
            return true;
        }
        public override bool GetStandardValuesExclusive(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            var result = base.GetStandardValues(context);

            var pipelineItem = (PipelineItemViewModel)context.Instance;
            //var importer = pipelineItem.Importer;
            
            //var processors = _pipelineProject.PipelineTypes.Processors;
            // TODO: we need to include the existing processor even if it doesn't match
            var processors = _pipelineProject._references.FindProcessors(_pipelineItem.Importer);
            


            var values = new List<ProcessorDescription>();
            foreach(var processor in processors)
            {
                values.Add(processor);
            }

            return new StandardValuesCollection(values);
        }

    }
}
