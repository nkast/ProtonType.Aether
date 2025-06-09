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
using System.IO;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    [Serializable]
    public class ProcessorDescription
    {
        public readonly string AssemblyPath;
        public readonly string TypeName;
        public readonly string TypeFullName;
        public readonly string OutputTypeFullName;
        public readonly string InputTypeFullName;
        public readonly ProcessorParamDescription[] ProcessorParams;

        // ContentProcessorAttribute
        public readonly string DisplayName;
        
        public ProcessorDescription(BinaryReader reader)
        {
            AssemblyPath = ReadString(reader);
            TypeName = ReadString(reader);
            TypeFullName = ReadString(reader);
            InputTypeFullName = ReadString(reader);
            OutputTypeFullName = ReadString(reader);

            ProcessorParams = new ProcessorParamDescription[reader.ReadInt32()];
            for (int i = 0; i < ProcessorParams.Length; i++)
            {
                ProcessorParams[i] = new ProcessorParamDescription(
                        ReadString(reader),
                        ReadString(reader),
                        ReadString(reader),
                        ReadString(reader),
                        ReadString(reader),
                        ReadStringArray(reader)
                    );
            }

            // ContentImporterAttribute 
            DisplayName = ReadString(reader);
        }
        
        public ProcessorDescription(string assemblyPath, string typeName, string typeFullName, string inputTypeFullName, string outputTypeFullName, ProcessorParamDescription[] processorParams, string displayName)
        {
            // TODO: Complete member initialization
            this.AssemblyPath = assemblyPath;
            this.TypeName = typeName;
            this.TypeFullName = typeFullName;
            this.InputTypeFullName = inputTypeFullName;
            this.OutputTypeFullName = outputTypeFullName;
            this.ProcessorParams = processorParams;
            this.DisplayName = displayName;
        }
        
        public void Write(BinaryWriter writer)
        {
            WriteString(writer, AssemblyPath);
            WriteString(writer, TypeName);
            WriteString(writer, TypeFullName);
            WriteString(writer, InputTypeFullName);
            WriteString(writer, OutputTypeFullName);

            writer.Write((Int32)ProcessorParams.Length);
            for (int i = 0; i < ProcessorParams.Length; i++)
            {
                WriteString(writer, ProcessorParams[i].Name);
                WriteString(writer, ProcessorParams[i].TypeName);
                WriteString(writer, ProcessorParams[i].TypeFullName);
                WriteString(writer, ProcessorParams[i].TypeAssemblyQualifiedName);
                WriteString(writer, ProcessorParams[i].DefaultValue);
                WriteStringArray(writer, ProcessorParams[i].StandardValues);
            }

            // ContentImporterAttribute
            WriteString(writer, DisplayName);
        }
        
        private void WriteString(BinaryWriter writer, string value)
        {
            bool isNull = (value == null);
            writer.Write(isNull);
            if (isNull) return;
                        
            writer.Write(value);
        }

        private string ReadString(BinaryReader Reader)
        {
            bool isNull = Reader.ReadBoolean();
            if (isNull) return null;

            return Reader.ReadString();
        }

        private void WriteStringArray(BinaryWriter writer, string[] values)
        {
            bool isNull = (values == null);
            writer.Write(isNull);
            if (isNull) return;

            writer.Write((Int32)values.Length);
            for (int i = 0; i < values.Length; i++)
                WriteString(writer, values[i]);
        }

        private string[] ReadStringArray(BinaryReader Reader)
        {
            bool isNull = Reader.ReadBoolean();
            if (isNull) return null;

            string[] values = new string[Reader.ReadInt32()];
            for (int i = 0; i < values.Length; i++)
                values[i] = ReadString(Reader);
            return values;            
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }

}
