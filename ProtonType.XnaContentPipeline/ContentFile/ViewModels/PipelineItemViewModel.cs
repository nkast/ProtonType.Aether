﻿// MonoGame - Copyright (C) The MonoGame Team
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
using System.IO;
using System.Linq;
using nkast.ProtonType.Framework.ViewModels;
using nkast.ProtonType.XnaContentPipeline.Common;

namespace nkast.ProtonType.XnaContentPipeline.ViewModels
{
    public partial class PipelineItemViewModel : BaseViewModel, IPipelineItemViewModel
    {
        internal readonly PipelineProjectViewModel PipelineProject;
        /*internal*/ public readonly PipelineItem PipelineItem;
        private readonly ProcessorParamsViewModel _processorParams;

        public string Filename { get { return PipelineItem.Filename; } }
        public string Location { get { return PipelineItem.Location; } }
        public string OriginalPath { get { return PipelineItem.OriginalPath; } }
        
        [Category("Build Settings")]
        [tainicom.WpfPropertyGrid.PropertyOrder(-4)]
        [DisplayName("Name")]
        [Description("The destinamtion name.")]
        public string Name
        {
            get { return Path.GetFileNameWithoutExtension(PipelineItem.DestinationPath); }
            set
            {
                if (Path.IsPathRooted(value))
                    return;

                // currently we only allow renames. Target must be at the same folder as the file.
                var filename = value + Path.GetExtension(PipelineItem.OriginalPath);
                var destPath = Path.GetDirectoryName(PipelineItem.DestinationPath);
                var destinationPath = Path.Combine(destPath, filename);

                if (PipelineItem.DestinationPath != destinationPath)
                {
                    PipelineItem.DestinationPath = destinationPath;
                    RaisePropertyChanged(() => Name);
                }
            }
        }

        [Category("Build Settings")]
        [tainicom.WpfPropertyGrid.PropertyOrder(-3)]
        [DisplayName("Build Action")]
        [Description("The way to process this content item.")]
        public BuildAction BuildAction
        {
            get { return PipelineItem.BuildAction; }
            set
            {
                if (PipelineItem.BuildAction != value)
                {
                    PipelineItem.BuildAction = value;
                    RaisePropertyChanged(() => BuildAction);
                }
            }
        }
        
        private ImporterDescription _importerDesc;
        [Category("Build Settings")]
        [tainicom.WpfPropertyGrid.PropertyOrder(-2)]
        [Description("The importer used to load the content file.")]
        public ImporterDescription Importer
        {
            get { return _importerDesc; }
            set
            {
                if (_importerDesc == value) return;

                _importerDesc = value;
                PipelineItem.Importer = _importerDesc.TypeName; // update Model
                RaisePropertyChanged(() => Importer);


                // Validate that our processor can accept input content of the type output by the new importer.
                // If it cannot, set the default processor.
                if (_importerDesc != PipelineTypes.MissingImporter)
                {
                    if (_processor == null || !_importerDesc.OutputBaseTypesFullName.Contains(_processor.InputTypeFullName))
                    {
                        Processor = PipelineProject.FindProcessor(_importerDesc.DefaultProcessor, _importerDesc);
                    }
                }
            }
        }


        private ProcessorDescription _processor;
        [Category("Build Settings")]
        [tainicom.WpfPropertyGrid.PropertyOrder(-1)]
        [Description("The processor used to load the content file.")]
        public ProcessorDescription Processor
        {
            get { return _processor; }
            set
            {
                if (_processor == value) return;

                _processor = value;
                PipelineItem.Processor = _processor.TypeName; // update Model
                RaisePropertyChanged(() => Processor);
                

                // When the processor changes reset our parameters
                // to the default for the processor type.
                _processorParams.Clear();
                foreach (ProcessorParamDescription pp in _processor.ProcessorParams)
                {
                    Type paramType = pp.GetParamType();
                    if (paramType == null)
                        paramType = typeof(string);

                    _processorParams.AddObject(pp.Name, pp.DefaultValue, paramType);
                }
            }
        }

        internal PipelineItemViewModel(PipelineProjectViewModel pipelineProject, PipelineItem item)
        {
            this.PipelineProject = pipelineProject;
            this.PipelineItem = item;

            _importerDesc = GetImporterImporterDescription(PipelineItem.Importer);
            _processor = GetProcessorDescription(PipelineItem.Processor);

            // update Model
            PipelineItem.Importer = _importerDesc.TypeName;
            PipelineItem.Processor = _processor.TypeName;
            
            // resolve processor params
            _processorParams = new ProcessorParamsViewModel(pipelineProject, this);
        }

        ImporterDescription GetImporterImporterDescription(string importerName)
        {
            //if (string.IsNullOrEmpty(importerName))
            //    return null;

            //if (BuildAction == BuildAction.Copy)
            //    return null;

            var importerDesc = PipelineProject.FindImporter(importerName, Path.GetExtension(OriginalPath));
            if (importerDesc == null)
            {
                // TODO: create a virtual importer
                importerDesc = PipelineTypes.MissingImporter;
            }
            
            return importerDesc;
        }

        private ProcessorDescription GetProcessorDescription(string processorName)
        {
            //if (string.IsNullOrEmpty(processorName))
            //    return null;

            //if (BuildAction == BuildAction.Copy)
            //    return null;

            var processorDesc = PipelineProject.FindProcessor(processorName, _importerDesc);
            if (processorDesc == null)
            {
                // TODO: create a virtual importer
                processorDesc = PipelineTypes.MissingProcessor;
            }

            return processorDesc;
        }
        
        public override string ToString()
        {
            return String.Format("{{Name:{0} BuildAction:{1} Importer:{2} Processor:{3}}}",
                Filename, BuildAction, Importer, Processor);
        }


    }
}
