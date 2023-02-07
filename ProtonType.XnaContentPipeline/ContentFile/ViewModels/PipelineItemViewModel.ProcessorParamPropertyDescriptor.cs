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
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.ViewModels.Converters;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public partial class PipelineItemViewModel
    {
        public class ProcessorParamPropertyDescriptor : PropertyDescriptor
        {
            private readonly PipelineItemViewModel _pipelineItemVM;
            private readonly ProcessorParamDescription _processorParamDescription;
            private readonly Type _propertyType;
            private readonly Type _componentType;
            private readonly string _propertyName;
            private readonly ProcessorParamsViewModel _processorParams;

            internal ProcessorParamPropertyDescriptor(
                PipelineItemViewModel pipelineItemVM,
                ProcessorParamDescription processorParamDescription,
                ProcessorParamsViewModel target)
                : base(processorParamDescription.Name, new Attribute[] { new CategoryAttribute("ProcessorParams") })
            {
                Type paramType = processorParamDescription.GetParamType();
                if (paramType == null)
                    paramType = typeof(string);

                _pipelineItemVM = pipelineItemVM;
                _processorParamDescription = processorParamDescription;
                _propertyName = processorParamDescription.Name;
                _propertyType = paramType;
                _componentType = typeof(IDictionary<string, object>);
                _processorParams = target;
            }

            public override object GetValue(object component)
            {
                if (_processorParams != null)
                {
                    if (!_processorParams.ContainsKey(_propertyName))
                        return string.Empty;

                    return _processorParams.GetObject(_propertyName);
                }
                else
                {
                    IDictionary<string, object> data = (component as IDictionary<string, object>);
                    if (!data.ContainsKey(_propertyName))
                        return string.Empty;

                    return data[_propertyName];
                }
            }

            public override void SetValue(object component, object value)
            {
                if (_processorParams != null)
                {
                    _processorParams.SetObject(_propertyName, value, PropertyType);
                }
                else
                {
                    IDictionary<string, object> data = (component as IDictionary<string, object>);
                    data[_propertyName] = value;
                }
            }

            public override TypeConverter Converter
            {
                get
                {
                    TypeConverter converter = null;

                    Type paramType = _processorParamDescription.GetParamType();
                    var isUnknownPropertyType = (paramType == null);

                    if (isUnknownPropertyType && _processorParamDescription.StandardValues != null)
                    {
                        converter = new UnkownParamStandardValuesConverter(_processorParamDescription.StandardValues);
                    }
                    else
                    {
                        var pipelineProject = _pipelineItemVM.PipelineProject;
                        converter = pipelineProject._references.FindConverter(PropertyType);
                    }

                    return converter;
                }
            }

            public override bool CanResetValue(object component) { return true; }
            public override Type ComponentType { get { return _componentType; } }
            public override bool IsReadOnly { get { return false; } }
            public override Type PropertyType { get { return _propertyType; } }
            public override void ResetValue(object component) { SetValue(component, null); }
            public override bool ShouldSerializeValue(object component) { return true; }
        }
    }
}