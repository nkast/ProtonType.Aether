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

using System.ComponentModel;
using nkast.ProtonType.ContentLib.ViewModels.Converters;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ViewModels.Converters;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public partial class PipelineItemViewModel : ICustomTypeDescriptor
    {
        
        #region ICustomTypeDescriptor
         
        string ICustomTypeDescriptor.GetClassName()
        {
            return TypeDescriptor.GetClassName(this, true);
        }

        AttributeCollection ICustomTypeDescriptor.GetAttributes()
        {
            return TypeDescriptor.GetAttributes(this,true);
        }

        string ICustomTypeDescriptor.GetComponentName()
        {
            return TypeDescriptor.GetComponentName(this, true);
        }

        TypeConverter ICustomTypeDescriptor.GetConverter()
        {
            return TypeDescriptor.GetConverter(this, true);
        }
        EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
        {
            return TypeDescriptor.GetDefaultEvent(this, true);
        }

        PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
        {
            return TypeDescriptor.GetDefaultProperty(this, true);
        }

        object ICustomTypeDescriptor.GetEditor(System.Type editorBaseType)
        {
            return TypeDescriptor.GetEditor(this, editorBaseType, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents(System.Attribute[] attributes)
        {
            return TypeDescriptor.GetEvents(this, attributes, true);
        }

        EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
        {
            return TypeDescriptor.GetEvents(this, true);
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(System.Attribute[] attributes)
        {
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(this, attributes, true);

            // https://www.codeproject.com/Articles/4448/Customized-display-of-collection-data-in-a-Propert
            var pds = new PropertyDescriptorCollection(null);
            // copy properties
            for (int i = 0; i < properties.Count; i++)
            {
                var property = properties[i];

                // always include those properties
                if (property.Name == "BuildAction")
                {
                    pds.Add(property);
                    continue;
                }

                // properties of Build Action
                if(this.BuildAction == Common.BuildAction.Build)
                {
                    if (property.Name == "Importer")
                    {
                        var importerTypeConverter = new ImporterDescriptionConverter(this.PipelineProject, this);
                        var importerPropertyDescriptor = new ImporterPropertyDescriptor(property, importerTypeConverter);
                        pds.Add(importerPropertyDescriptor);
                        continue;
                    }

                    if (property.Name == "Processor")
                    {
                        var processorTypeConverter = new ProcessorDescriptionConverter(this.PipelineProject, this);
                        var processorPropertyDescriptor = new ProcessorPropertyDescriptor(property, processorTypeConverter);
                        pds.Add(processorPropertyDescriptor);
                        continue;
                    }

                    // add other properties
                    pds.Add(property);
                }
            }

            // create processorParam properties
            if (BuildAction == Common.BuildAction.Build)
            {
                var pipelineItem = PipelineItem;
                var processor = this._processor;
                if (processor != null)
                {
                    foreach (ProcessorParamDescription processorParamDesc in processor.ProcessorParams)
                    {
                        var processorParamPropertyDescriptor = new PipelineItemViewModel.ProcessorParamPropertyDescriptor(
                                            this,
                                            processorParamDesc,
                                            _processorParams);
                        pds.Add(processorParamPropertyDescriptor);
                    }
                }
            }


            properties = pds;
            return properties;
        }

        PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
        {
            return TypeDescriptor.GetProperties(this, true);
        }

        object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
        {
            return this;
        }
        
        #endregion

    }
}
