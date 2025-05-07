#region License
//   Copyright 2025 Kastellanos Nikolaos
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
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using nkast.ProtonType.XnaContentPipeline.Builder.Models;

namespace nkast.ProtonType.XnaContentPipeline.Builder.Converters          
{
    public class StatusToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PipelineBuildItemStatus status = (PipelineBuildItemStatus)value;
            switch (status)
            {
                case PipelineBuildItemStatus.Queued:
                    return CreateImageSource("/ProtonType.XnaContentPipeline.Builder;component/Icons/" + "build_queued.png");
                case PipelineBuildItemStatus.Building:
                    return CreateImageSource("/ProtonType.XnaContentPipeline.Builder;component/Icons/" + "build_processing.png");
                case PipelineBuildItemStatus.Build:
                    return CreateImageSource("/ProtonType.XnaContentPipeline.Builder;component/Icons/" + "build_succeed.png");
                case PipelineBuildItemStatus.Failed:
                    return CreateImageSource("/ProtonType.XnaContentPipeline.Builder;component/Icons/" + "build_fail.png");

                default:
                    return null;
            }
        }

        private BitmapImage CreateImageSource(string resourcePath)
        {
            System.Diagnostics.Debug.Assert(System.Threading.Thread.CurrentThread == Application.Current.Dispatcher.Thread,
                "ImageSource must be created in UIThread.");

            BitmapImage bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(resourcePath, UriKind.Relative);
            bmp.EndInit();
            return bmp;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
