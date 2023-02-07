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
using System.Linq;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Graphics;
using nkast.ProtonType.XnaContentPipeline.ProxyClient;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    internal class PipelineProjectReaderException : Exception
    {

    }

    internal class PipelineProjectReader
    {
        private class PipelineProjectParserContext
        {
            #region Other Data

            private readonly PipelineProject _project;
            private readonly IDictionary<string, string> _processorParams = new Dictionary<string, string>();
        
            private string _processor;
            private string _importer;
        
            #endregion

            #region CommandLineParameters
        
            [CommandLineParameter(
                Name = "outputDir",
                ValueName = "directoryPath",
                Description = "The directory where all content is written.")]
            public string OutputDir { set { _project.OutputDir = value; } }

            [CommandLineParameter(
                Name = "intermediateDir",
                ValueName = "directoryPath",
                Description = "The directory where all intermediate files are written.")]
            public string IntermediateDir { set { _project.IntermediateDir = value; } }

            [CommandLineParameter(
                Name = "reference",
                ValueName = "assemblyNameOrFile",
                Description = "Adds an assembly reference for resolving content importers, processors, and writers.")]
            public List<string> References 
            {
                set { _project.References = value; }
                get { return _project.References; }
            }

            [CommandLineParameter(
                Name = "platform",
                ValueName = "targetPlatform",
                Description = "Set the target platform for this build.  Defaults to Windows.")]
            public ProxyTargetPlatform Platform { set { _project.Platform = value; } }

            [CommandLineParameter(
                Name = "profile",
                ValueName = "graphicsProfile",
                Description = "Set the target graphics profile for this build.  Defaults to HiDef.")]
            public ProxyGraphicsProfile Profile { set { _project.Profile = value; } }

            [CommandLineParameter(
                Name = "config",
                ValueName = "string",
                Description = "The optional build config string from the build system.")]
            public string Config { set { _project.Config = value; } }

            // Allow a MGCB file containing the /rebuild parameter to be imported without error
            [CommandLineParameter(
                Name = "rebuild",
                ValueName = "bool",
                Description = "Forces a rebuild of the project.")]
            public bool Rebuild { set { _rebuild = value; } }
            private bool _rebuild;

            // Allow a MGCB file containing the /clean parameter to be imported without error
            [CommandLineParameter(
                Name = "clean",
                ValueName = "bool",
                Description = "Removes intermediate and output files.")]
            public bool Clean { set { _clean = value; } }
            private bool _clean;

            [CommandLineParameter(
                Name = "compress",
                ValueName = "bool",
                Description = "Content files can be compressed for smaller file sizes.")]
            public bool Compress { set { _project.Compress = value; } }

            [CommandLineParameter(
                Name = "importer",
                ValueName = "className",
                Description = "Defines the class name of the content importer for reading source content.")]
            public string Importer
            {
                get { return _importer; }
                set
                {
                    _importer = value;
                }
            }

            [CommandLineParameter(
                Name = "processor",
                ValueName = "className",
                Description = "Defines the class name of the content processor for processing imported content.")]
            public string Processor
            {
                get { return _processor; }
                set
                {
                    _processor = value;
                    _processorParams.Clear();
                }
            }

            [CommandLineParameter(
                Name = "processorParam",
                ValueName = "name=value",
                Description = "Defines a parameter name and value to set on a content processor.")]
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

            [CommandLineParameter(
                Name = "build",
                ValueName = "sourceFile",
                Description = "Build the content source file using the previously set switches and options.")]
            public void OnBuild(string sourceFile)
            {
                AddContent(sourceFile);
            }

            public void AddContent(string sourceFile)
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
                var projectDir = _project.Location + Path.DirectorySeparatorChar;
                sourceFile = PathHelper.GetRelativePath(projectDir, sourceFile);

                // check duplicates.
                var previous = _project.PipelineItems.FirstOrDefault(e => e.OriginalPath.Equals(sourceFile, StringComparison.InvariantCultureIgnoreCase));
                if (previous != null)
                    throw new Exception("sourceFile allready added.");

                // Create the item for processing later.
                var item = new PipelineItem()
                {
                    BuildAction = BuildAction.Build,
                    OriginalPath = sourceFile,
                    DestinationPath = string.IsNullOrEmpty(destinationPath) ? sourceFile : destinationPath,
                    Importer = Importer,
                    Processor = Processor,
                };
                _project.AddItem(item);

                // Copy the current processor parameters blind as we
                // will validate and remove invalid parameters during
                // the build process later.
                foreach (var pair in _processorParams)
                    item.ProcessorParams.Add(pair.Key, pair.Value);
            }

            [CommandLineParameter(
                Name = "copy",
                ValueName = "sourceFile",
                Description = "Copy the content source file verbatim to the output directory.")]
            public void OnCopy(string sourceFile)
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
                var projectDir = _project.Location + Path.DirectorySeparatorChar;
                sourceFile = PathHelper.GetRelativePath(projectDir, sourceFile);

                // check duplicates.
                var previous = _project.PipelineItems.FirstOrDefault(e => e.OriginalPath.Equals(sourceFile, StringComparison.InvariantCultureIgnoreCase));
                if (previous != null)
                    throw new Exception("sourceFile allready added.");

                // Create the item for processing later.
                var item = new PipelineItem()
                {
                    BuildAction = BuildAction.Copy,
                    OriginalPath = sourceFile,
                    DestinationPath = string.IsNullOrEmpty(destinationPath) ? sourceFile : destinationPath,
                };
                _project.AddItem(item);

                // Copy the current processor parameters blind as we
                // will validate and remove invalid parameters during
                // the build process later.
                foreach (var pair in _processorParams)
                    item.ProcessorParams.Add(pair.Key, pair.Value);
            }

            #endregion
        
            public PipelineProjectParserContext(PipelineProject project)
            {
                _project = project;
            }
        }

        internal static PipelineProject LoadProject(string projectFilePath, IPipelineLogger logger)
        {
            PipelineProject project = new PipelineProject();

            project.ClearItems();

            // Store the file name for saving later.
            project.OriginalPath = projectFilePath;

            var parserContext = new PipelineProjectParserContext(project);
            var parser = new CommandLineParser(parserContext);
            parser.OnError += (msg) => logger.LogMessage(Path.GetFileName(projectFilePath) + ": " + msg);

            parser.ParseFile(projectFilePath);

            return project;
        }
    }

    /*internal*/ public class PipelineProjectWriter
    {
        /*internal*/ public static void SaveProject(PipelineProject _project, string filePath, IEnumerable<PipelineItem> pipelineItems)
        {
            string path = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string fileExtension = Path.GetExtension(filePath);

            string tmpFilename = Path.Combine(path, fileName + fileExtension + "~tmp");

            using (var io = File.CreateText(tmpFilename))
                SaveProject(_project, io, pipelineItems);

            if (File.Exists(filePath))
                File.Delete(filePath);
            File.Move(tmpFilename, filePath);
        }

        private static void SaveProject(PipelineProject project, TextWriter io, IEnumerable<PipelineItem> pipelineItems)
        {
            const string lineFormat = "/{0}:{1}";
            const string processorParamFormat = "{0}={1}";
            string line;

            line = FormatDivider("Global Properties");
            io.WriteLine(line);

            line = string.Format(lineFormat, "outputDir", project.OutputDir);
            io.WriteLine(line);

            line = string.Format(lineFormat, "intermediateDir", project.IntermediateDir);
            io.WriteLine(line);

            line = string.Format(lineFormat, "platform", project.Platform);
            io.WriteLine(line);

            line = string.Format(lineFormat, "config", project.Config);
            io.WriteLine(line);

            line = string.Format(lineFormat, "profile", project.Profile);
            io.WriteLine(line);

            line = string.Format(lineFormat, "compress", project.Compress);
            io.WriteLine(line);

            line = FormatDivider("References");
            io.WriteLine(line);

            foreach (var i in project.References)
            {
                line = string.Format(lineFormat, "reference", i);
                io.WriteLine(line);
            }

            line = FormatDivider("Content");
            io.WriteLine(line);
                        
            if (pipelineItems == null)
                pipelineItems = project.PipelineItems;

            foreach (var i in pipelineItems)
            {
                // Wrap content item lines with a begin comment line
                // to make them more cohesive (for version control).                  
                line = string.Format("#begin {0}", i.OriginalPath);
                io.WriteLine(line);

                if (i.BuildAction == BuildAction.Copy)
                {
                    string path = i.OriginalPath;
                    if (i.OriginalPath != i.DestinationPath)
                        path += ";" + i.DestinationPath;
                    line = string.Format(lineFormat, "copy", path);
                    io.WriteLine(line);
                    io.WriteLine();
                }
                else
                {

                    // Write importer.
                    {
                        line = string.Format(lineFormat, "importer", i.Importer);
                        io.WriteLine(line);
                    }

                    // Write processor.
                    {
                        line = string.Format(lineFormat, "processor", i.Processor);
                        io.WriteLine(line);
                    }

                    // Write processor parameters.
                    if (i.Processor == null)
                    {
                        // Could still be missing the real processor.
                        // If so, write the string parameters from import.
                        foreach (var jName in i.ProcessorParams.Keys)
                        {
                            string valueStr = i.ProcessorParams[jName];
                            var processorParam = string.Format(processorParamFormat, jName, valueStr);
                            line = string.Format(lineFormat, "processorParam", processorParam);
                            io.WriteLine(line);
                        }
                    }
                    else
                    {
                        // Otherwise, write only values which are defined by the real processor.
                        foreach (var jName in i.ProcessorParams.Keys)
                        {
                            var valueStr = i.ProcessorParams[jName];
                            var processorParam = string.Format(processorParamFormat, jName, valueStr);
                            line = string.Format(lineFormat, "processorParam", processorParam);
                            io.WriteLine(line);
                        }
                    }

                    string buildValue = i.OriginalPath;
                    if (i.OriginalPath != i.DestinationPath)
                        buildValue += ";" + i.DestinationPath;
                    line = string.Format(lineFormat, "build", buildValue);
                    io.WriteLine(line);
                    io.WriteLine();
                }
            }
        }

        private static string FormatDivider(string label)
        {
            var commentFormat = Environment.NewLine + "#----------------------------------------------------------------------------#" + Environment.NewLine;

            label = " " + label + " ";
            var src = commentFormat.Length / 2 - label.Length / 2;
            var dst = src + label.Length;

            return commentFormat.Substring(0, src) + label + commentFormat.Substring(dst);
        }
        
    }
}
