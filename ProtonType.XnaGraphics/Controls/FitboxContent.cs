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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace tainicom.ProtonType.XnaGraphics.Controls
{
    public class FitboxContent : ContentControl
    {
        public readonly static DependencyProperty ScreenTypeProperty =
            DependencyProperty.Register("ScreenType", typeof(ScreenTypeDesc), typeof(FitboxContent),
            new FrameworkPropertyMetadata((object)ScreenTypeDesc.Fill,
                FrameworkPropertyMetadataOptions.AffectsMeasure));

        public ScreenTypeDesc ScreenType
        {
            get { return (ScreenTypeDesc)base.GetValue(ScreenTypeProperty); }
            set { base.SetValue(ScreenTypeProperty, value); }
        }

        public FitboxContent()
        {
            HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            VerticalAlignment = System.Windows.VerticalAlignment.Stretch;

            // Background must be set for DragEnter/Drop to work.
            Background = Brushes.Transparent;
        }

        protected override Size ArrangeOverride(Size arrangeSize)
        {
            var fitSize = FitSize(arrangeSize);
            var result = base.ArrangeOverride(fitSize);
            return result;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var fitSize = FitSize(constraint);
            var result = base.MeasureOverride(fitSize);
            return result;
        }

        private Size FitSize(Size constraint)
        {
            if (ScreenType.Width == 0 && ScreenType.Height == 0)
                return constraint;

            var ration = (float)ScreenType.Width / (float)ScreenType.Height;
            var rationW = constraint.Width / ScreenType.Width;
            var rationH = constraint.Height / ScreenType.Height;
            var scale = Math.Min(rationW, rationH);
            if (!ScreenType.ZoomToFit)
                scale = Math.Min(scale, 1);
            constraint.Width = ScreenType.Width * scale;
            constraint.Height = ScreenType.Height * scale;
            return constraint;
        }

    }
}
