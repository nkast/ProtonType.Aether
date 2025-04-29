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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;


namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    public class BuildEvent
    {
        public const string Extension = ".kniContent";

        public BuildEvent()
        {
            SourceFile = string.Empty;
            DestFile = string.Empty;
            Importer = string.Empty;
            Processor = string.Empty;
            ProcessorParams = new OpaqueDataDictionary();
            Dependencies = new List<string>();
            BuildAsset = new List<string>();
            BuildOutput = new List<string>();
        }

        /// <summary>
        /// Absolute path to the source file.
        /// </summary>
        public string SourceFile { get; set; }

        /// <summary>
        /// The date/time stamp of the source file.
        /// </summary>
        public DateTime SourceTime { get; set; }

        /// <summary>
        /// Absolute path to the output file.
        /// </summary>
        public string DestFile { get; set; }

        /// <summary>
        /// The date/time stamp of the destination file.
        /// </summary>
        public DateTime DestTime { get; set; }

        public string Importer { get; set; }

        /// <summary>
        /// The date/time stamp of the DLL containing the importer.
        /// </summary>
        public DateTime ImporterTime { get; set; }

        public string Processor { get; set; }

        /// <summary>
        /// The date/time stamp of the DLL containing the processor.
        /// </summary>
        public DateTime ProcessorTime { get; set; }

        public OpaqueDataDictionary ProcessorParams { get; set; }

        /// <summary>
        /// Gets or sets the dependencies.
        /// </summary>
        /// <value>The dependencies.</value>
        /// <remarks>
        /// Dependencies are extra files that are required in addition to the <see cref="SourceFile"/>.
        /// Dependencies are added using <see cref="ContentProcessorContext.AddDependency"/>. Changes
        /// to the dependent file causes a rebuilt of the content.
        /// </remarks>
        public List<string> Dependencies { get; set; }

        /// <summary>
        /// Gets or sets the additional (nested) assets.
        /// </summary>
        /// <value>The additional (nested) assets.</value>
        /// <remarks>
        /// <para>
        /// Additional assets are built by using an <see cref="ExternalReference{T}"/> and calling
        /// <see cref="ContentProcessorContext.BuildAndLoadAsset{TInput,TOutput}(ExternalReference{TInput},string)"/>
        /// or <see cref="ContentProcessorContext.BuildAsset{TInput,TOutput}(ExternalReference{TInput},string)"/>.
        /// </para>
        /// <para>
        /// Examples: The mesh processor may build textures and effects in addition to the mesh.
        /// </para>
        /// </remarks>
        public List<string> BuildAsset { get; set; }

        /// <summary>
        /// Gets or sets the related output files.
        /// </summary>
        /// <value>The related output files.</value>
        /// <remarks>
        /// Related output files are non-XNB files that are included in addition to the XNB files.
        /// Related output files need to be copied to the output folder by a content processor and
        /// registered by calling <see cref="ContentProcessorContext.AddOutputFile"/>.
        /// </remarks>
        public List<string> BuildOutput { get; set; }


        public void SaveBinary(string filePath)
        {
            // Make sure the directory exists.
            Directory.CreateDirectory(Path.GetDirectoryName(filePath) + Path.DirectorySeparatorChar);

            using (Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new PipelineBuildEventBinaryWriter(stream))
            {
                writer.Write(this);
            }
        }

        public static BuildEvent LoadBinary(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                using (var reader = new PipelineBuildEventBinaryReader(stream))
                {
                    BuildEvent result = new BuildEvent();
                    reader.Read(result);
                    return result;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string ConvertToString(object value)
        {
            if (value == null)
                return null;

            //Convert.ToString(value, CultureInfo.InvariantCulture);
            TypeConverter typeConverter = TypeDescriptor.GetConverter(value.GetType());
            return typeConverter.ConvertToInvariantString(value);
        }


        internal class PipelineBuildEventBinaryWriter : BinaryWriter
        {
            private const string Header = "KNIC"; // content db
            private const short MajorVersion =  3;
            private const short MinorVersion = 15;
            private const short DataType = 2; // PipelineBuildEvent data


            public PipelineBuildEventBinaryWriter(Stream output) : base(output)
            {
            }

            internal void Write(BuildEvent value)
            {
                Write(Header.ToCharArray());
                Write((Int16)MajorVersion);
                Write((Int16)MinorVersion);
                Write((Int32)DataType);
                Write((Int32)0); // reserved


                WriteStringOrNull(value.SourceFile);
                WriteDateTime(value.SourceTime);

                WriteStringOrNull(value.DestFile);
                WriteDateTime(value.DestTime);

                WriteStringOrNull(value.Importer);
                WriteDateTime(value.ImporterTime);

                WriteStringOrNull(value.Processor);
                WriteDateTime(value.ProcessorTime);

                WritePackedInt(value.ProcessorParams.Count);
                foreach (var param in value.ProcessorParams)
                {
                    WriteStringOrNull(param.Key);
                    WriteStringOrNull(ConvertToString(param.Value));
                }

                WritePackedInt(value.Dependencies.Count);
                for (int i = 0; i < value.Dependencies.Count; i++)
                    WriteStringOrNull(value.Dependencies[i]);

                WritePackedInt(value.BuildAsset.Count);
                for (int i = 0; i < value.BuildAsset.Count; i++)
                    WriteStringOrNull(value.BuildAsset[i]);

                WritePackedInt(value.BuildOutput.Count);
                for (int i = 0; i < value.BuildOutput.Count; i++)
                    WriteStringOrNull(value.BuildOutput[i]);

                return;
            }

            protected void WritePackedInt(int value)
            {
                // write zigzag encoded int
                int zzint = ((value << 1) ^ (value >> 31));
                Write7BitEncodedInt(zzint);
            }

            private void WriteStringOrNull(string value)
            {
                if (value != null)
                {
                    Write(true);
                    Write(value);
                }
                else
                    Write(false);
            }

            private void WriteDateTime(DateTime value)
            {
                Write((Int64)value.ToBinary());
            }
        }


        internal class PipelineBuildEventBinaryReader : BinaryReader
        {
            private const string Header = "KNIC"; // content db
            private const short MajorVersion =  3;
            private const short MinorVersion = 15;
            private const int DataType = 2; // PipelineBuildEvent data


            public PipelineBuildEventBinaryReader(Stream output) : base(output)
            {
            }

            internal void Read(BuildEvent value)
            {
                if (ReadByte() != Header[0]
                ||  ReadByte() != Header[1]
                ||  ReadByte() != Header[2]
                ||  ReadByte() != Header[3])
                    throw new Exception("Invalid file.");

                if (ReadInt16() != MajorVersion
                || ReadInt16() != MinorVersion)
                    throw new Exception("Invalid file version.");

                int dataType = ReadInt32();
                if (dataType != DataType)
                    throw new Exception("Invalid data type.");

                int reserved0 = ReadInt32();

                value.SourceFile = ReadStringOrNull();
                value.SourceTime = ReadDateTime();

                value.DestFile = ReadStringOrNull();
                value.DestTime = ReadDateTime();

                value.Importer = ReadStringOrNull();
                value.ImporterTime = ReadDateTime();

                value.Processor = ReadStringOrNull();
                value.ProcessorTime = ReadDateTime();

                int parametersCount = ReadPackedInt();
                value.ProcessorParams = new OpaqueDataDictionary();
                for (int i = 0; i < parametersCount; i++)
                {
                    value.ProcessorParams.Add(
                        ReadStringOrNull(),
                        ReadStringOrNull());
                }

                int dependenciesCount = ReadPackedInt();
                value.Dependencies = new List<string>(dependenciesCount);
                for (int i = 0; i < dependenciesCount; i++)
                    value.Dependencies.Add(ReadStringOrNull());

                int buildAssetCount = ReadPackedInt();
                value.BuildAsset = new List<string>(buildAssetCount);
                for (int i = 0; i < buildAssetCount; i++)
                    value.BuildAsset.Add(ReadStringOrNull());

                int buildOutputCount = ReadPackedInt();
                value.BuildOutput = new List<string>(buildOutputCount);
                for (int i = 0; i < buildOutputCount; i++)
                    value.BuildOutput.Add(ReadStringOrNull());

                return;
            }


            private int ReadPackedInt()
            {
                unchecked
                {
                    // read zigzag encoded int
                    int zzint = Read7BitEncodedInt();
                    return ((int)((uint)zzint >> 1) ^ (-(zzint & 1)));
                }
            }

            private string ReadStringOrNull()
            {
                if (ReadBoolean())
                    return ReadString();
                else
                    return null;
            }

            private DateTime ReadDateTime()
            {
                return DateTime.FromBinary(ReadInt64());
            }
        }
    }
}
