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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using tainicom.ProtonType.XnaContentPipeline.Common;
using tainicom.ProtonType.XnaContentPipeline.Common.Converters;
using Microsoft.Xna.Framework.Content.Pipeline;

namespace tainicom.ProtonType.XnaContentPipeline.ProxyServer.Assemblies
{
    partial class AssembliesMgr
    {
        private readonly List<string> _assemblies = new List<string>();
        private ConcurrentBag<ImporterInfo> _importers = new ConcurrentBag<ImporterInfo>();
        private ConcurrentBag<ProcessorInfo> _processors = new ConcurrentBag<ProcessorInfo>();


        public AssembliesMgr()
        {
        }

        internal void AddAssembly(ContentBuildLogger logger, string ProjectDirectory, string assemblyPath)
        {
            var rootedAssemblyPath = assemblyPath;
            if (!Path.IsPathRooted(rootedAssemblyPath))
                rootedAssemblyPath = Path.GetFullPath(Path.Combine(ProjectDirectory, assemblyPath));

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
                var asm = Assembly.LoadFrom(assemblyPath);
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
                var contentIdentity = new ContentIdentity(assemblyPath);
                logger.LogWarning(null, contentIdentity, e.Message);
                return;
            }   
            catch (Exception e)
            {
                var contentIdentity = new ContentIdentity(assemblyPath);
                logger.LogWarning(null, contentIdentity, "Failed to load assembly '{0}': {1}", assemblyPath, e.Message);
                return;
            }
        }

        private void LoadAssemblyTypes(Assembly asm, string assemblyPath)
        {
            Type[] types = asm.GetExportedTypes();

            foreach (var type in types)
            {
                if (type.IsAbstract)
                    continue;

                DateTime assemblyTimestamp = File.GetLastWriteTime(asm.Location);

                var legacyImporterInfo = ProcessImporter(type, assemblyPath, assemblyTimestamp);
                if (legacyImporterInfo != null)
                    _importers.Add(legacyImporterInfo);

                var legacyProcessorInfo = ProcessProcessor(type, assemblyPath, assemblyTimestamp);
                if (legacyProcessorInfo != null)
                    _processors.Add(legacyProcessorInfo);
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

            var processorInstance = (IContentProcessor)Activator.CreateInstance(processorType);
            var inputType = processorInstance.InputType;
            var outputType = processorInstance.OutputType;

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
            var typeProperties = processorType.GetProperties(bindings);
            var properties = new List<ProcessorParamDescription>();
            foreach (PropertyInfo pi in typeProperties)
            {
                // TODO: get ContentPipelineIgnore attribute
                //p.GetCustomAttribute(typeof(ContentPipelineIgnore))

                var defaultObjValue = pi.GetValue(processorInstance, null);
                var converter = FindConverter(pi.PropertyType);
                string defaultValue = (string)converter.ConvertTo(null, System.Globalization.CultureInfo.InvariantCulture, defaultObjValue, typeof(string));
                
                var p = new ProcessorParamDescription(
                    name: pi.Name,
                    typeName: pi.PropertyType.Name,
                    typeFullName: pi.PropertyType.FullName,
                    typeAssemblyQualifiedName: pi.PropertyType.AssemblyQualifiedName,
                    defaultValue: defaultValue,
                    standardValues: GetStandardValues(pi.PropertyType)
                    );
                properties.Add(p);
            }
            var processorParams = properties.ToArray();

            ProcessorInfo processorInfo = null;

            // find ContentProcessorAttribute
            var attributes = processorType.GetCustomAttributes(typeof(ContentProcessorAttribute), false);
            if (attributes.Length != 0)
            {
                var processorAttribute = attributes[0] as ContentProcessorAttribute;
                var processorDesc = new ProcessorDescription(
                    assemblyPath,
                    processorType.Name,
                    processorType.FullName,
                    inputType.FullName,
                    inputBaseTypesFullName.ToArray(),
                    outputType.FullName,
                    processorParams,
                    // ContentProcessorAttribute
                    processorAttribute.DisplayName
                );

                var defaultProcessorValues = CreateProcessorDefaultValues(processorInstance);
                processorInfo = new ProcessorInfo(processorType, assemblyTimestamp, processorDesc, defaultProcessorValues);
            }

            return processorInfo;
        }
        
        string[] GetStandardValues(Type type)
        {
            var typeDescriptor = TypeDescriptor.GetConverter(type);
            if (typeDescriptor.GetStandardValuesSupported())
            {
                var standardValues = new List<string>();
                foreach (var standardValue in typeDescriptor.GetStandardValues())
                {
                    var converter = FindConverter(standardValue.GetType());
                    var strValue = converter.ConvertToInvariantString(standardValue);
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
            foreach (var importer in _importers)
            {
                if (importer.Description.TypeName.Equals(importerName))
                    return importer;
            }

            return null;
        }

        public ProcessorInfo GetProcessorInfo(string processorName)
        {
            // Search for the importer.
            foreach (var processor in _processors)
            {
                if (processor.Description.TypeName.Equals(processorName))
                    return processor;
            }

            return null;
        }

        public ImporterInfo GetImporterInfoByExtension(string ext)
        {
            foreach (var importer in _importers)
            {
                if (importer.Description.FileExtensions.Any(e => e.Equals(ext, StringComparison.InvariantCultureIgnoreCase)))
                    return importer;
            }

            return null;
        }

        public IContentImporter CreateImporter(ImporterInfo importer)
        {
            return Activator.CreateInstance(importer.Type) as IContentImporter;
        }

        public IContentProcessor CreateProcessor(ProcessorInfo processor, OpaqueDataDictionary processorParameters)
        {
            var processorType = processor.Type;

            // Create the processor.
            var processorInstance = (IContentProcessor)Activator.CreateInstance(processorType);

            // Convert and set the parameters on the processor.
            foreach (var param in processorParameters)
            {
                var propInfo = processorType.GetProperty(param.Key, BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null || propInfo.GetSetMethod(false) == null)
                    continue;

                // If the property value is already of the correct type then set it.
                if (propInfo.PropertyType.IsInstanceOfType(param.Value))
                    propInfo.SetValue(processorInstance, param.Value, null);
                else
                {
                    // Find a type converter for this property.
                    var typeConverter = TypeDescriptor.GetConverter(propInfo.PropertyType);
                    if (typeConverter.CanConvertFrom(param.Value.GetType()))
                    {
                        var propValue = typeConverter.ConvertFrom(null, CultureInfo.InvariantCulture, param.Value);
                        propInfo.SetValue(processorInstance, propValue, null);
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
                importerName = null;
                var importerInfo = GetImporterInfoByExtension(Path.GetExtension(sourceFilepath));
                if (importerInfo != null)
                    importerName = importerInfo.Description.TypeName;
            }
            if (string.IsNullOrEmpty(importerName))
                throw new Exception(string.Format("Couldn't find a default importer for '{0}'.", sourceFilepath));

            // Resolve the processor name.
            if (string.IsNullOrEmpty(processorName))
            {
                processorName = null;
                var importerInfo = GetImporterInfo(importerName);
                if (importerInfo != null)
                    processorName = importerInfo.Description.DefaultProcessor;
            }
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
            var defaultValues = new OpaqueDataDictionary();
            try
            {
                var properties = processorInstance.GetType().GetProperties(BindingFlags.FlattenHierarchy | BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    var propertyName = property.Name;
                    var propertyValue = property.GetValue(processorInstance, null);
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
