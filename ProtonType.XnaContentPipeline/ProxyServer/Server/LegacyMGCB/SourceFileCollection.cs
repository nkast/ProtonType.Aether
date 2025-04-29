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
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    public sealed class SourceFileCollection
    {
        public const string Extension = ".kniContent";

        public GraphicsProfile Profile { get; set; }

        public TargetPlatform Platform { get; set; }

        public ContentCompression Compression { get; set; }

        public string Config { get; set; }

        public List<string> SourceFiles { get; set; }

        public List<string> DestFiles { get; set; }


        public SourceFileCollection()
        {
            SourceFiles = new List<string>();
            DestFiles = new List<string>();
            Config = string.Empty;
        }

        public void SaveBinary(string filePath)
        {
            using (Stream stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new SourceFileCollectionBinaryWriter(stream))
            {
                writer.Write(this);
            }
        }

        public static SourceFileCollection LoadBinary(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return null;

                using (Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                using (var writer = new SourceFileCollectionBinaryReader(stream))
                {
                    SourceFileCollection result = new SourceFileCollection();
                    writer.Read(result);
                    return result;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public int SourceFilesCount { get { return this.SourceFiles.Count; } }

        internal void AddFile(string sourceFile, string outputFile)
        {
            this.SourceFiles.Add(sourceFile);
            this.DestFiles.Add(outputFile);
        }

        public void Merge(SourceFileCollection previousFileCollection)
        {
            for (int i = 0; i < previousFileCollection.SourceFiles.Count; i++)
            {
                string prevSourceFile = previousFileCollection.SourceFiles[i];
                string prevDestFile = previousFileCollection.DestFiles[i];

                bool contains = this.SourceFiles.Exists((sourceFile) => string.Equals(sourceFile, prevSourceFile, StringComparison.InvariantCultureIgnoreCase));
                if (!contains)
                {
                    this.SourceFiles.Add(prevSourceFile);
                    this.DestFiles.Add(prevDestFile);
                }
            }
        }


        internal class SourceFileCollectionBinaryWriter : BinaryWriter
        {
            private const string Header = "KNIC"; // content db
            private const short MajorVersion =  3;
            private const short MinorVersion = 15;
            private const int DataType = 1; // SourceFileCollection data


            public SourceFileCollectionBinaryWriter(Stream output) : base(output)
            {
            }

            internal void Write(SourceFileCollection value)
            {
                Write((byte)Header[0]);
                Write((byte)Header[1]);
                Write((byte)Header[2]);
                Write((byte)Header[3]);
                Write((Int16)MajorVersion);
                Write((Int16)MinorVersion);
                Write((Int32)DataType);
                Write((Int32)0); // reserved


                Write((Int32)value.Profile);
                Write((Int32)value.Platform);
                Write((Int32)value.Compression);
                WriteStringOrNull(value.Config);

                WritePackedInt(value.SourceFiles.Count);
                for (int i = 0; i < value.SourceFiles.Count; i++)
                {
                    WriteStringOrNull(value.SourceFiles[i]);
                    WriteStringOrNull(value.DestFiles[i]);
                }

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

        }

        internal class SourceFileCollectionBinaryReader : BinaryReader
        {
            private const string Header = "KNIC"; // content db
            private const short MajorVersion =  3;
            private const short MinorVersion = 15;
            private const int DataType = 1; // SourceFileCollection data


            public SourceFileCollectionBinaryReader(Stream output) : base(output)
            {
            }

            internal void Read(SourceFileCollection value)
            {
                if (ReadByte() != Header[0]
                ||  ReadByte() != Header[1]
                ||  ReadByte() != Header[2]
                ||  ReadByte() != Header[3])
                    throw new Exception("Invalid file.");

                if (ReadInt16() != MajorVersion
                ||  ReadInt16() != MinorVersion)
                    throw new Exception("Invalid file version.");

                int dataType = ReadInt32(); 
                if (dataType != DataType)
                    throw new Exception("Invalid data type.");

                int reserved0 = ReadInt32();


                value.Profile = (GraphicsProfile)ReadInt32();
                value.Platform = (TargetPlatform)ReadInt32();
                value.Compression = (ContentCompression)ReadInt32();
                value.Config = ReadStringOrNull();

                int filesCount = ReadPackedInt();
                value.SourceFiles = new List<string>(filesCount);
                value.DestFiles = new List<string>(filesCount);
                for (int i = 0; i < filesCount; i++)
                {
                    value.SourceFiles.Add(ReadStringOrNull());
                    value.DestFiles.Add(ReadStringOrNull());
                }

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

        }
    }
}
