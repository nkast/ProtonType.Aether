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
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;

namespace nkast.ProtonType.XnaContentPipeline.ProxyServer
{
    [XmlRoot(ElementName = "SourceFileCollection")]
    public sealed class SourceFileCollection
    {
        public static readonly string XmlExtension = ".mgcontent";

        public GraphicsProfile Profile { get; set; }

        public TargetPlatform Platform { get; set; }

        public string Config { get; set; }

        [XmlArrayItem("File")]
        public List<string> SourceFiles { get; set; }

        [XmlArrayItem("File")]
        public List<string> DestFiles { get; set; }


        public SourceFileCollection()
        {
            SourceFiles = new List<string>();
            DestFiles = new List<string>();
            Config = string.Empty;
        }

        static public SourceFileCollection LoadXml(string filePath)
        {
            try
            {
                using (var textReader = new StreamReader(filePath))
                {
                    XmlSerializer deserializer = new XmlSerializer(typeof(SourceFileCollection));
                    SourceFileCollection result = (SourceFileCollection)deserializer.Deserialize(textReader);

                    if (result.DestFiles.Count != result.SourceFiles.Count)
                        return null; // file is invalid

                    return result;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SaveXml(string filePath)
        {
            using (var textWriter = new StreamWriter(filePath, false, new UTF8Encoding(false)))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(SourceFileCollection));
                serializer.Serialize(textWriter, this);
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
    }
}
