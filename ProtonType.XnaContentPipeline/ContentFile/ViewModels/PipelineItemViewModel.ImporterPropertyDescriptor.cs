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
using System.ComponentModel;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public partial class PipelineItemViewModel
    {
        class ImporterPropertyDescriptor : PropertyDescriptor
        {
            private PropertyDescriptor _property;
            private TypeConverter _converter;
            
            public ImporterPropertyDescriptor(PropertyDescriptor property, TypeConverter importerTypeConverter) : base(property)
            {
                this._property = property;
                this._converter = importerTypeConverter;
            }

            public override TypeConverter Converter
            {
                get { return _converter; }
            }

            public override bool CanResetValue(object component)
            {
                return _property.CanResetValue(component);
            }

            public override Type ComponentType
            {
                get { return _property.ComponentType; }
            }

            public override object GetValue(object component)
            {
                return _property.GetValue(component);
            }

            public override bool IsReadOnly
            {
                get { return _property.IsReadOnly; }
            }

            public override Type PropertyType
            {
                get { return _property.PropertyType; }
            }

            public override void ResetValue(object component)
            {
                _property.ResetValue(component);
            }

            public override void SetValue(object component, object value)
            {
                _property.SetValue(component, value);
            }

            public override bool ShouldSerializeValue(object component)
            {
                return _property.ShouldSerializeValue(component);
            }
        }
    }
}
