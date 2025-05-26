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
using System.IO;
using System.Linq;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    internal class PipelineProjectReaderException : Exception
    {

    }

    internal class PipelineProjectReader
    {
        // project context
        private string _importer;
        private string _processor;
        private readonly IDictionary<string, string> _processorParams = new Dictionary<string, string>();

        internal PipelineProject LoadProject(string projectFilePath, IPipelineLogger logger)
        {
            _importer = null;
            _processor = null;
            _processorParams.Clear();

            PipelineProject project = new PipelineProject();

            project.ClearItems();

            CommandLineParser parser = new CommandLineParser();
            IEnumerable<string> commands = ReadFile(projectFilePath);
            commands = CommandLineParser.Preprocess(commands);


            foreach (string option in commands)
            {
                if (!ParseOption(option, project, logger))
                    break;
            }

            return project;
        }

        private static List<string> ReadFile(string file)
        {
            string[] commands = File.ReadAllLines(file);

            List<string> lines = new List<string>();

            for (int j = 0; j < commands.Length; j++)
            {
                string line = commands[j];

                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("#"))
                    continue;

                lines.Add(line);
            }

            return lines;
        }

        private bool ParseOption(string line, PipelineProject project, IPipelineLogger logger)
        {
            if (string.IsNullOrWhiteSpace(line))
                return true;

            if (line.StartsWith("#"))
                return true;

            if (line[0] == '/')
            {
                string optionName;
                string optionValue;

                int optionEnd = line.IndexOf(':');
                if (optionEnd != -1)
                {
                    optionName  = line.Substring(1, optionEnd-1);
                    optionValue = line.Substring(optionEnd+1, line.Length-(optionEnd+1));
                }
                else
                {
                    optionName = line.Substring(1);
                    optionValue = String.Empty;
                }

                return ParseOption(optionName, optionValue, project);
            }
            else
            {
                logger.LogMessage(Path.GetFileName(project.OriginalPath) + ": Invalid line.");
                return false;
            }
        }

        private bool ParseOption(string optionName, string optionValue, PipelineProject project)
        {
            if (String.Compare(optionName, "outputDir", true) == 0)
            {
                project.OutputDir = optionValue;
                return true;
            }
            if (String.Compare(optionName, "intermediateDir", true) == 0)
            {
                project.IntermediateDir = optionValue;
                return true;
            }
            if (String.Compare(optionName, "platform", true) == 0)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(ProxyTargetPlatform));
                ProxyTargetPlatform objValue = (ProxyTargetPlatform)converter.ConvertFromInvariantString(optionValue);
                project.Platform = objValue;
                return true;
            }
            if (String.Compare(optionName, "profile", true) == 0)
            {
                TypeConverter converter = TypeDescriptor.GetConverter(typeof(ProxyGraphicsProfile));
                ProxyGraphicsProfile objValue = (ProxyGraphicsProfile)converter.ConvertFromInvariantString(optionValue);
                project.Profile = objValue;
                return true;
            }
            if (String.Compare(optionName, "config", true) == 0)
            {
                project.Config = optionValue;
                return true;
            }
            if (String.Compare(optionName, "compress", true) == 0)
            {
                if (optionValue == String.Empty)
                    optionValue = "True";

                TypeConverter converter = TypeDescriptor.GetConverter(typeof(Boolean));
                Boolean objValue = (Boolean)converter.ConvertFromInvariantString(optionValue);
                project.Compress = objValue;
                return true;
            }
            if (String.Compare(optionName, "compression", true) == 0)
            {
                if (optionValue == String.Empty)
                    optionValue = "Default";

                TypeConverter converter = TypeDescriptor.GetConverter(typeof(CompressionMethod));
                CompressionMethod objValue = (CompressionMethod)converter.ConvertFromInvariantString(optionValue);
                project.Compression = objValue;
                return true;
            }
            if (String.Compare(optionName, "reference", true) == 0)
            {
                project.References.Add(optionValue);
                return true;
            }
            if (String.Compare(optionName, "packageReference", true) == 0)
            {
                project.PackageReferences.Add(Package.Parse(optionValue));
                return true;
            }

            if (String.Compare(optionName, "importer", true) == 0)
            {
                _importer = optionValue;
                return true;
            }
            if (String.Compare(optionName, "processor", true) == 0)
            {
                _processor = optionValue;
                return true;
            }
            if (String.Compare(optionName, "processorParam", true) == 0)
            {
                AddProcessorParam(optionValue);
                return true;
            }
            if (String.Compare(optionName, "build", true) == 0)
            {
                OnBuild(optionValue, project);
                return true;
            }
            if (String.Compare(optionName, "copy", true) == 0)
            {
                OnCopy(optionValue, project);
                return true;
            }

            return false;
        }

        public void AddProcessorParam(string nameAndValue)
        {
            var keyAndValue = nameAndValue.Split('=');
            if (keyAndValue.Length != 2)
            {
                // Do we error out or something?
                return;
            }

            _processorParams.Remove(keyAndValue[0]);
            _processorParams.Add(keyAndValue[0], keyAndValue[1]);
        }

        public void OnBuild(string sourceFile, PipelineProject project)
        {
            // Split sourceFile;destinationPath
            string destinationPath = null;
            if (sourceFile.Contains(";"))
            {
                var split = sourceFile.Split(';');
                sourceFile = split[0];
                destinationPath = split[1];
            }

            // Make sure the source file is relative to the project.
            if (Path.IsPathRooted(sourceFile))
                throw new InvalidOperationException("Relative path expected. "+ sourceFile);

            // check duplicates.
            PipelineItem previous = project.PipelineItems.FirstOrDefault(e => e.OriginalPath.Equals(sourceFile, StringComparison.InvariantCultureIgnoreCase));
            if (previous != null)
                throw new Exception("sourceFile allready added.");

            // Create the item for processing later.
            PipelineItem item = new PipelineItem()
            {
                BuildAction = BuildAction.Build,
                OriginalPath = sourceFile,
                DestinationPath = string.IsNullOrEmpty(destinationPath) ? sourceFile : destinationPath,
                Importer = _importer,
                Processor = _processor,
            };
            project.AddItem(item);

            foreach (var pair in _processorParams)
                item.ProcessorParams.Add(pair.Key, pair.Value);
        }
                        
        public void OnCopy(string sourceFile, PipelineProject project)
        {
            // Split sourceFile;destinationPath
            string destinationPath = null;
            if (sourceFile.Contains(";"))
            {
                string[] split = sourceFile.Split(';');
                sourceFile = split[0];
                destinationPath = split[1];
            }

            // Make sure the source file is relative to the project.
            if (Path.IsPathRooted(sourceFile))
                throw new InvalidOperationException("Relative path expected. " + sourceFile);

            // check duplicates.
            PipelineItem previous = project.PipelineItems.FirstOrDefault(e => e.OriginalPath.Equals(sourceFile, StringComparison.InvariantCultureIgnoreCase));
            if (previous != null)
                throw new Exception("sourceFile allready added.");

            // Create the item for processing later.
            PipelineItem item = new PipelineItem()
            {
                BuildAction = BuildAction.Copy,
                OriginalPath = sourceFile,
                DestinationPath = string.IsNullOrEmpty(destinationPath) ? sourceFile : destinationPath,
            };
            project.AddItem(item);

            // Copy the current processor parameters blind as we
            // will validate and remove invalid parameters during
            // the build process later.
            foreach (var pair in _processorParams)
                item.ProcessorParams.Add(pair.Key, pair.Value);
        }
    }

    /*internal*/ public class PipelineProjectWriter
    {
        /*internal*/ public static void SaveProject(PipelineProject project, string filePath, IEnumerable<PipelineItem> pipelineItems)
        {
            string path = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath);

            string tmpFilename = Path.Combine(path, fileName + fileExtension + "~tmp");

            using (StreamWriter io = File.CreateText(tmpFilename))
                SaveProject(project, io, pipelineItems);

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tmpFilename, filePath);
        }

        private static void SaveProject(PipelineProject project, TextWriter io, IEnumerable<PipelineItem> pipelineItems)
        {
            const string lineFormat = "/{0}:{1}";
            const string processorParamFormat = "{0}={1}";

            io.WriteLine(FormatDivider("Global Properties"));

            io.WriteLine(String.Format(lineFormat, "outputDir", project.OutputDir));
            io.WriteLine(String.Format(lineFormat, "intermediateDir", project.IntermediateDir));
            io.WriteLine(String.Format(lineFormat, "platform", project.Platform));
            if (project.Config != null)
                io.WriteLine(String.Format(lineFormat, "config", project.Config));
            io.WriteLine(String.Format(lineFormat, "profile", project.Profile));
            io.WriteLine(String.Format(lineFormat, "compress", project.Compress));
            if (project.Compression != CompressionMethod.Default)
                io.WriteLine(String.Format(lineFormat, "compression", project.Compression));

            io.WriteLine(FormatDivider("References"));

            foreach (string i in project.References)
            {
                io.WriteLine(String.Format(lineFormat, "reference", i));
            }

            foreach (Package i in project.PackageReferences)
            {
                io.WriteLine(string.Format(lineFormat, "packageReference", i.ToString()));
            }

            io.WriteLine(FormatDivider("Content"));
                        
            if (pipelineItems == null)
                pipelineItems = project.PipelineItems;

            foreach (PipelineItem i in pipelineItems)
            {
                // Wrap content item lines with a begin comment line
                // to make them more cohesive (for version control).                  
                io.WriteLine(String.Format("#begin {0}", i.OriginalPath));

                if (i.BuildAction == BuildAction.Copy)
                {
                    string path = i.OriginalPath;
                    if (i.OriginalPath != i.DestinationPath)
                        path += ";" + i.DestinationPath;
                    io.WriteLine(String.Format(lineFormat, "copy", path));
                    io.WriteLine();
                }
                else
                {

                    // Write importer.
                    io.WriteLine(String.Format(lineFormat, "importer", i.Importer));

                    // Write processor.
                    io.WriteLine(String.Format(lineFormat, "processor", i.Processor));

                    // Write processor parameters.
                    if (i.Processor == null)
                    {
                        // Could still be missing the real processor.
                        // If so, write the string parameters from import.
                        foreach (string jName in i.ProcessorParams.Keys)
                        {
                            string valueStr = i.ProcessorParams[jName];
                            string processorParam = string.Format(processorParamFormat, jName, valueStr);
                            io.WriteLine(String.Format(lineFormat, "processorParam", processorParam));
                        }
                    }
                    else
                    {
                        // Otherwise, write only values which are defined by the real processor.
                        foreach (string jName in i.ProcessorParams.Keys)
                        {
                            string valueStr = i.ProcessorParams[jName];
                            string processorParam = string.Format(processorParamFormat, jName, valueStr);
                            io.WriteLine(String.Format(lineFormat, "processorParam", processorParam));
                        }
                    }

                    string buildValue = i.OriginalPath;
                    if (i.OriginalPath != i.DestinationPath)
                        buildValue += ";" + i.DestinationPath;
                    io.WriteLine(String.Format(lineFormat, "build", buildValue));
                    io.WriteLine();
                }
            }
        }

        private static string FormatDivider(string label)
        {
            string commentFormat = Environment.NewLine + "#----------------------------------------------------------------------------#" + Environment.NewLine;

            label = " " + label + " ";
            int src = commentFormat.Length / 2 - label.Length / 2;
            int dst = src + label.Length;

            return commentFormat.Substring(0, src) + label + commentFormat.Substring(dst);
        }
        
    }
}
