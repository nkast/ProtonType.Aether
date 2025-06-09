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
using System.IO;
using System.Linq;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    [Serializable]
    public class ImporterDescription
    {
        public readonly string AssemblyPath;
        public readonly string TypeName;
        public readonly string TypeFullName;

        public readonly string[] OutputBaseTypesFullName;

        // ContentImporterAttribute
        public readonly string DisplayName;
        public readonly string DefaultProcessor;
        public readonly IEnumerable<string> FileExtensions;

        public ImporterDescription(BinaryReader reader)
        {
            AssemblyPath = ReadString(reader);
            TypeName = ReadString(reader);
            TypeFullName = ReadString(reader);

            OutputBaseTypesFullName = new String[reader.ReadInt32()];
            for (int i = 0; i < OutputBaseTypesFullName.Length; i++)
                OutputBaseTypesFullName[i] = ReadString(reader);

            // ContentImporterAttribute
            DisplayName = ReadString(reader);
            DefaultProcessor = ReadString(reader);

            int fileExtensionsCount = reader.ReadInt32();
            List<string> fileExtensions = new List<string>(fileExtensionsCount);
            for (int i = 0; i < fileExtensionsCount; i++)
                fileExtensions.Add(ReadString(reader));
            FileExtensions = fileExtensions;
        }

        public ImporterDescription(string assemblyPath, string typeName, string typeFullName, string[] outputBaseTypesFullName, string displayName, string defaultProcessor, IEnumerable<string> fileExtensions)
        {
            this.AssemblyPath = assemblyPath;
            this.TypeName = typeName;
            this.TypeFullName = typeFullName;
            this.OutputBaseTypesFullName = outputBaseTypesFullName;
            this.DisplayName = displayName;
            this.DefaultProcessor = defaultProcessor;
            this.FileExtensions = fileExtensions;
        }


        public void Write(BinaryWriter writer)
        {
            WriteString(writer, AssemblyPath);
            WriteString(writer, TypeName);
            WriteString(writer, TypeFullName);

            writer.Write((Int32)OutputBaseTypesFullName.Length);
            for (int i = 0; i < OutputBaseTypesFullName.Length;i++)
                WriteString(writer, OutputBaseTypesFullName[i]);

            // ContentImporterAttribute
            WriteString(writer, DisplayName);
            WriteString(writer, DefaultProcessor);

            writer.Write((Int32)FileExtensions.Count());
            foreach (var fileExtension in FileExtensions)
                WriteString(writer, fileExtension);
        }
        
        private void WriteString(BinaryWriter writer, string value)
        {
            bool isNotNull = value != null;
            writer.Write(isNotNull);
            if (isNotNull)
                writer.Write(value);
        }

        private string ReadString(BinaryReader Reader)
        {
            bool isNotNull = Reader.ReadBoolean();
            return (isNotNull) ? Reader.ReadString() : null;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
