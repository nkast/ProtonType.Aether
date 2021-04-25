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
using tainicom.ProtonType.Framework.ViewModels;

namespace tainicom.ProtonType.XnaContentPipeline.ViewModels
{
    class ProcessorParamsViewModel : BaseViewModel
    {
        private readonly PipelineProjectViewModel _pipelineProject;
        private readonly PipelineItemViewModel _pipelineItem;

        private readonly IDictionary<string, object> _paramsObjects = new Dictionary<string, object>();
        

        public ProcessorParamsViewModel(PipelineProjectViewModel pipelineProject, PipelineItemViewModel pipelineItem)
        {
            this._pipelineProject = pipelineProject;
            this._pipelineItem = pipelineItem;

            if (_pipelineItem.Processor != null)
            {
                ConvertProcessorParamsToObjects();
            }
        }

        internal void ConvertProcessorParamsToObjects()
        {
            var processor = _pipelineItem.Processor;

            foreach (var pp in processor.ProcessorParams)
            {
                Type paramType = pp.GetParamType();
                if (paramType == null)
                    paramType = typeof(string);

                if (!ContainsKey(pp.Name))
                {
                    SetObject(pp.Name, pp.DefaultValue, paramType);
                }
                else
                {
                    string str = GetValue(pp.Name);
                    if (str == null) continue;
                    SetObject(pp.Name, str, paramType);
                }
            }

            return;
        }
        
        internal void Clear()
        {
            _pipelineItem.PipelineItem.ProcessorParams.Clear();
            _paramsObjects.Clear();
        }

        internal bool ContainsKey(string name)
        {
            return _pipelineItem.PipelineItem.ProcessorParams.ContainsKey(name);
        }
        
        internal string GetValue(string name)
        {
            return _pipelineItem.PipelineItem.ProcessorParams[name];
        }
        

        internal void AddObject(string name, string str, Type paramType)
        {
            var converter = _pipelineProject._references.FindConverter(paramType);
            object obj = converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, str);
            
            _pipelineItem.PipelineItem.ProcessorParams.Add(name, str);
            _paramsObjects.Add(name, obj);
        }

        internal void SetObject(string name, string str, Type paramType)
        {
            var converter = _pipelineProject._references.FindConverter(paramType);
            object obj = converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, str);

            _pipelineItem.PipelineItem.ProcessorParams[name] = str;
            _paramsObjects[name] = obj;
        }

        internal void SetObject(string name, object obj, Type paramType)
        {
            var converter = _pipelineProject._references.FindConverter(paramType);
            string str = (string)converter.ConvertTo(null, System.Globalization.CultureInfo.InvariantCulture, obj, typeof(string));

            _pipelineItem.PipelineItem.ProcessorParams[name] = str;
            _paramsObjects[name] = obj;
        }

        internal object GetObject(string name)
        {
            return _paramsObjects[name];
        }

    }
}
