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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using nkast.ProtonType.XnaContentPipeline.Common;
using nkast.ProtonType.XnaContentPipeline.Common.Converters;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer.Assemblies
{
    partial class AssembliesMgr
    {
        private readonly List<string> _assemblies = new List<string>();
        private ConcurrentBag<ImporterInfo> _importers = new ConcurrentBag<ImporterInfo>();
        private ConcurrentBag<ProcessorInfo> _processors = new ConcurrentBag<ProcessorInfo>();


        public AssembliesMgr()
        {
        }

        internal void AddAssembly(ContentBuildLogger logger, string baseDirectory, string assemblyPath)
        {
            string rootedAssemblyPath = assemblyPath;
            if (!Path.IsPathRooted(rootedAssemblyPath))
                rootedAssemblyPath = Path.GetFullPath(Path.Combine(baseDirectory, assemblyPath));

            if (!Path.IsPathRooted(rootedAssemblyPath))
                throw new ArgumentException("assemblyFilePath must be absolute!");

            // Make sure we're not adding the same assembly twice.
            rootedAssemblyPath = LegacyPathHelper.Normalize(rootedAssemblyPath);

            if (!_assemblies.Contains(rootedAssemblyPath))
            {
                _assemblies.Add(rootedAssemblyPath);
                LoadAssembly(logger, rootedAssemblyPath);
            }
        }
        
        internal void LoadAssembly(ContentBuildLogger logger, string assemblyPath)
        {
            try
            {
                Assembly asm = Assembly.LoadFrom(assemblyPath);
                LoadAssemblyTypes(asm, assemblyPath);
            }
            catch (BadImageFormatException e)
            {
                logger.LogWarning(null, null, "Assembly is either corrupt or built using a different " +
                    "target platform than this process. Reference another target architecture (x86, x64, " +
                    "AnyCPU, etc.) of this assembly. '{0}': {1}", assemblyPath, e.Message);
                // The assembly failed to load... nothing
                // we can do but ignore it.
                return;
            }
            catch (FileNotFoundException e)
            {
                ContentIdentity contentIdentity = new ContentIdentity(assemblyPath);
                logger.LogWarning(null, contentIdentity, e.Message);
                return;
            }   
            catch (Exception e)
            {
                ContentIdentity contentIdentity = new ContentIdentity(assemblyPath);
                logger.LogWarning(null, contentIdentity, "Failed to load assembly '{0}': {1}", assemblyPath, e.Message);
                return;
            }
        }

        private void LoadAssemblyTypes(Assembly asm, string assemblyPath)
        {
            Type[] types = asm.GetExportedTypes();

            foreach (Type type in types)
            {
                if (type.IsAbstract)
                    continue;

                DateTime assemblyTimestamp = File.GetLastWriteTime(asm.Location);

                ImporterInfo importerInfo = ProcessImporter(type, assemblyPath, assemblyTimestamp);
                if (importerInfo != null)
                    _importers.Add(importerInfo);

                ProcessorInfo processorInfo = ProcessProcessor(type, assemblyPath, assemblyTimestamp);
                if (processorInfo != null)
                    _processors.Add(processorInfo);
            }
        }
        
        internal IEnumerable<string> GetAssemblies()
        {
            return _assemblies;
        }

        internal IEnumerator<ImporterDescription> GetImporters()
        {
            return _importers.Select((item)=>item.Description).GetEnumerator();
        }

        internal IEnumerator<ProcessorDescription> GetProcessors()
        {
            return _processors.Select((item)=>item.Description).GetEnumerator();
        }

        private ImporterInfo ProcessImporter(Type importerType, string assemblyPath, DateTime assemblyTimestamp)
        {
            // Process IContentImporter
            if (importerType.GetInterface(@"IContentImporter") != typeof(IContentImporter))
                return null;

            // Find the abstract base class ContentImporter<T>.
            var importerBaseType = importerType.BaseType;
            while (!importerBaseType.IsAbstract)
                importerBaseType = importerBaseType.BaseType;
            var outputType = importerBaseType.GetGenericArguments()[0];

            // find all output base type
            List<string> outputBaseTypesFullName = new List<string>();
            for (Type baseType = outputType.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                // ignore base class of content items
                if (baseType == typeof(Microsoft.Xna.Framework.Content.Pipeline.ContentItem))
                    break;

                outputBaseTypesFullName.Add(baseType.FullName);
            }

            ImporterInfo importerInfo = null;

            // find ContentImporterAttribute
            var attributes = importerType.GetCustomAttributes(typeof(ContentImporterAttribute), false);
            ContentImporterAttribute importerAttribute = null;
            if (attributes.Length != 0)
            {
                importerAttribute = attributes[0] as ContentImporterAttribute;
            }
            else
            {
                // If no attribute specify use default ContentImporterAttribute
                importerAttribute = new ContentImporterAttribute(".*");
                importerAttribute.DefaultProcessor = "";
                importerAttribute.DisplayName = importerType.Name;
            }

            var importerDesc = new ImporterDescription(
                assemblyPath,
                importerType.Name,
                importerType.FullName,
                outputType.Name,
                outputType.FullName,
                outputBaseTypesFullName.ToArray(),
                // ContentImporterAttribute
                importerAttribute.DisplayName,
                importerAttribute.DefaultProcessor,
                importerAttribute.FileExtensions
            );

            importerInfo = new ImporterInfo(importerType, assemblyTimestamp, importerDesc);

            return importerInfo;
        }

        private ProcessorInfo ProcessProcessor(Type processorType, string assemblyPath, DateTime assemblyTimestamp)
        {
            if (processorType.GetInterface(@"IContentProcessor") != typeof(IContentProcessor))
                return null;

            IContentProcessor processorInstance = (IContentProcessor)Activator.CreateInstance(processorType);
            Type inputType = processorInstance.InputType;
            Type outputType = processorInstance.OutputType;

            // find all output base type
            List<string> inputBaseTypesFullName = new List<string>();
            for (Type baseType = inputType.BaseType; baseType != null; baseType = baseType.BaseType)
            {
                // ignore base class of content items
                if (baseType == typeof(Microsoft.Xna.Framework.Content.Pipeline.ContentItem))
                    break;

                inputBaseTypesFullName.Add(baseType.FullName);
            }

            // get params
            const BindingFlags bindings = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
            PropertyInfo[] typeProperties = processorType.GetProperties(bindings);
            List<ProcessorParamDescription> processorParams = new List<ProcessorParamDescription>();
            foreach (PropertyInfo pi in typeProperties)
            {
                // TODO: get ContentPipelineIgnore attribute
                //p.GetCustomAttribute(typeof(ContentPipelineIgnore))

                object defaultObjValue = pi.GetValue(processorInstance, null);
                TypeConverter converter = FindConverter(pi.PropertyType);
                string defaultValue = (string)converter.ConvertTo(null, System.Globalization.CultureInfo.InvariantCulture, defaultObjValue, typeof(string));

                ProcessorParamDescription processorParam = new ProcessorParamDescription(
                    name: pi.Name,
                    typeName: pi.PropertyType.Name,
                    typeFullName: pi.PropertyType.FullName,
                    typeAssemblyQualifiedName: pi.PropertyType.AssemblyQualifiedName,
                    defaultValue: defaultValue,
                    standardValues: GetStandardValues(pi.PropertyType)
                    );
                processorParams.Add(processorParam);
            }

            ProcessorInfo processorInfo = null;

            // find ContentProcessorAttribute
            object[] attributes = processorType.GetCustomAttributes(typeof(ContentProcessorAttribute), false);
            if (attributes.Length != 0)
            {
                ContentProcessorAttribute processorAttribute = attributes[0] as ContentProcessorAttribute;
                var processorDesc = new ProcessorDescription(
                    assemblyPath,
                    processorType.Name,
                    processorType.FullName,
                    inputType.FullName,
                    inputBaseTypesFullName.ToArray(),
                    outputType.FullName,
                    processorParams.ToArray(),
                    // ContentProcessorAttribute
                    processorAttribute.DisplayName
                );

                OpaqueDataDictionary defaultProcessorValues = CreateProcessorDefaultValues(processorInstance);
                processorInfo = new ProcessorInfo(processorType, assemblyTimestamp, processorDesc, defaultProcessorValues);
            }

            return processorInfo;
        }
        
        string[] GetStandardValues(Type type)
        {
            TypeConverter typeDescriptor = TypeDescriptor.GetConverter(type);
            if (typeDescriptor.GetStandardValuesSupported())
            {
                List<string> standardValues = new List<string>();
                foreach (object standardValue in typeDescriptor.GetStandardValues())
                {
                    TypeConverter converter = FindConverter(standardValue.GetType());
                    string strValue = converter.ConvertToInvariantString(standardValue);
                    standardValues.Add(strValue);
                }
                return standardValues.ToArray();
            }
            else
            {
                return null;
            }
        }

        internal TypeConverter FindConverter(Type type)
        {
            if (type == typeof(Color))
                return new StringToColorConverter();

            return TypeDescriptor.GetConverter(type);
        }
        

        public ImporterInfo GetImporterInfo(string importerName)
        {
            // Search for the importer.
            foreach (ImporterInfo importerInfo in _importers)
            {
                if (importerInfo.Description.TypeName.Equals(importerName))
                    return importerInfo;
            }

            return null;
        }

        public ProcessorInfo GetProcessorInfo(string processorName)
        {
            // Search for the importer.
            foreach (ProcessorInfo processorInfo in _processors)
            {
                if (processorInfo.Description.TypeName.Equals(processorName))
                    return processorInfo;
            }

            return null;
        }

        public IContentImporter CreateImporter(ImporterInfo importer)
        {
            return Activator.CreateInstance(importer.Type) as IContentImporter;
        }

        public IContentProcessor CreateProcessor(ProcessorInfo processor, OpaqueDataDictionary processorParameters)
        {
            Type processorType = processor.Type;

            // Create the processor.
            IContentProcessor processorInstance = (IContentProcessor)Activator.CreateInstance(processorType);

            // Convert and set the parameters on the processor.
            foreach (var param in processorParameters)
            {
                PropertyInfo property = processorType.GetProperty(param.Key, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                if (property == null || property.GetSetMethod(false) == null)
                    continue;

                // If the property value is already of the correct type then set it.
                if (property.PropertyType.IsInstanceOfType(param.Value))
                    property.SetValue(processorInstance, param.Value, null);
                else
                {
                    // Find a type converter for this property.
                    TypeConverter typeConverter = TypeDescriptor.GetConverter(property.PropertyType);
                    if (typeConverter.CanConvertFrom(param.Value.GetType()))
                    {
                        object propertyValue = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, param.Value);
                        property.SetValue(processorInstance, propertyValue, null);
                    }
                }
            }

            return processorInstance;
        }

        public void ResolveImporterAndProcessor(string sourceFilepath, ref string importerName, ref string processorName)
        {
            // Resolve the importer name.
            if (string.IsNullOrEmpty(importerName))
            {
                string ext = Path.GetExtension(sourceFilepath);
                foreach (ImporterInfo importerInfo in _importers)
                {
                    if (importerInfo.Description.FileExtensions.Contains(ext, StringComparer.InvariantCultureIgnoreCase))
                        importerName = importerInfo.Description.TypeName;
                }
            }

            // Resolve the processor name.
            if (string.IsNullOrEmpty(processorName))
            {
                foreach (ImporterInfo importerInfo in _importers)
                {
                    if (importerInfo.Description.TypeName.Equals(importerName))
                        if (importerInfo.Description.DefaultProcessor != null)
                            processorName = importerInfo.Description.DefaultProcessor;
                }
            }

            if (string.IsNullOrEmpty(importerName))
                throw new Exception(string.Format("Couldn't find a default importer for '{0}'.", sourceFilepath));
            if (string.IsNullOrEmpty(processorName))
                throw new Exception(string.Format("Couldn't find a default processor for importer '{0}'.", importerName));
        }

        /// <summary>
        /// Create the content processor instance and read the default values.
        /// </summary>
        /// <param name="processorInfo"></param>
        /// <returns></returns>
        private static OpaqueDataDictionary CreateProcessorDefaultValues(IContentProcessor processorInstance)
        {
            OpaqueDataDictionary defaultValues = new OpaqueDataDictionary();
            try
            {
                PropertyInfo[] properties = processorInstance.GetType().GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                foreach (PropertyInfo property in properties)
                {
                    string propertyName = property.Name;
                    object propertyValue = property.GetValue(processorInstance, null);
                    defaultValues.Add(propertyName, propertyValue);
                }
            }
            catch
            {
                // Ignore exception. Will be handled in ProcessContent.
            }

            return defaultValues;
        }

    }
}
