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
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    /// <summary>
    /// Adapted from this generic command line argument parser:
    /// http://blogs.msdn.com/b/shawnhar/archive/2012/04/20/a-reusable-reflection-based-command-line-parser.aspx     
    /// </summary>
    internal class CommandLineParser
    {
        #region Supporting Types

        public class PreprocessorProperty
        {
            public string Name;
            public string CurrentValue;

            public PreprocessorProperty()
            {
                Name = string.Empty;
                CurrentValue = string.Empty;
            }
        }

        public class PreprocessorPropertyCollection
        {
            private readonly List<PreprocessorProperty> _properties;

            public PreprocessorPropertyCollection()
            {
                _properties = new List<PreprocessorProperty>();
            }

            public string this[string name]
            {
                get
                {
                    foreach (PreprocessorProperty i in _properties)
                    {
                        if (i.Name.Equals(name))
                            return i.CurrentValue;
                    }

                    return null;
                }

                set
                {
                    foreach (PreprocessorProperty i in _properties)
                    {
                        if (i.Name.Equals(name))
                        {
                            i.CurrentValue = value;
                            return;
                        }
                    }

                    PreprocessorProperty prop = new PreprocessorProperty()
                        {
                            Name = name,
                            CurrentValue = value,
                        };
                    _properties.Add(prop);
                }
            }
        }

        #endregion

        internal static IEnumerable<string> Preprocess(IEnumerable<string> args)
        {
            PreprocessorPropertyCollection properties = new PreprocessorPropertyCollection();

            List<string> output = new List<string>();
            List<string> lines = new List<string>(args);
            var ifstack = new Stack<Tuple<string, string>>();

            while (lines.Count > 0)
            {            
                string arg = lines[0];
                lines.RemoveAt(0);

                if (arg.StartsWith("$endif"))
                {
                    ifstack.Pop();
                    continue;
                }
                
                if (ifstack.Count > 0)
                {
                    bool skip = false;
                    foreach (var i in ifstack)
                    {
                        string val = properties[i.Item1];
                        if (!(i.Item2).Equals(val))
                        {
                            skip = true;
                            break;
                        }
                    }

                    if (skip)
                        continue;
                }

                if (arg.StartsWith("$set"))
                {
                    string[] words = arg.Substring(5).Split('=');
                    string name = words[0];
                    string value = words[1];

                    properties[name] = value;

                    continue;
                }

                if (arg.StartsWith("$if"))
                {
                    string[] words = arg.Substring(4).Split('=');
                    string name = words[0];
                    string value = words[1];

                    var condition = new Tuple<string, string>(name, value);
                    ifstack.Push(condition);
                    
                    continue;
                }

                if (arg.StartsWith("/define:"))
                {
                    string[] words = arg.Substring(8).Split('=');
                    string name = words[0];
                    string value = words[1];

                    properties[name] = value;

                    continue;
                }

                output.Add(arg);
            }

            return output;
        }

    }

}
