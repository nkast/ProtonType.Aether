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
using System.ComponentModel;
using Microsoft.Xna.Framework;

namespace tainicom.ProtonType.XnaContentPipeline.Common.Converters
{
	public class StringToColorConverter : TypeConverter
	{
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType != typeof (string))            
                return base.ConvertTo(context, culture, value, destinationType);

            var color = (Color)value;
            return string.Format("{0},{1},{2},{3}", color.R, color.G, color.B, color.A);
        }

		public override bool CanConvertFrom (ITypeDescriptorContext context, Type sourceType)
		{
			if (sourceType == typeof (string))
				return true;

			return base.CanConvertFrom (context, sourceType);
		}

		public override object ConvertFrom (ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
		{
			if (value.GetType () == typeof (string)) {
				string[] values = ((string)value).Split(new char[] {','},StringSplitOptions.None);
                if (values.Length == 4)
                {
                    var r = int.Parse(values[0].Trim());
                    var g = int.Parse(values[1].Trim());
                    var b = int.Parse(values[2].Trim());
                    var a = int.Parse(values[3].Trim());
                    return new Microsoft.Xna.Framework.Color(r, g, b, a);
                }
                else
                {
                    throw new ArgumentException(string.Format("Could not convert from string({0}) to Color, expected format is 'r,g,b,a'", value));                    
                }
			}

			return base.ConvertFrom (context, culture, value);
		}
	}
}
