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

namespace tainicom.ProtonType.XnaContentPipeline.Common
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
                    foreach (var i in _properties)
                    {
                        if (i.Name.Equals(name))
                            return i.CurrentValue;
                    }

                    return null;
                }

                set
                {
                    foreach (var i in _properties)
                    {
                        if (i.Name.Equals(name))
                        {
                            i.CurrentValue = value;
                            return;
                        }
                    }

                    var prop = new PreprocessorProperty()
                        {
                            Name = name,
                            CurrentValue = value,
                        };
                    _properties.Add(prop);
                }
            }
        }

        #endregion

        private readonly object _optionsObject;
        private readonly Queue<MemberInfo> _requiredOptions;
        private readonly Dictionary<string, MemberInfo> _optionalOptions;
        private readonly Dictionary<string, string> _flags;
        private readonly List<string> _requiredUsageHelp;

        public readonly PreprocessorPropertyCollection _properties;

        public delegate void ErrorCallback(string msg);
        public event ErrorCallback OnError;

        public CommandLineParser(object optionsObject)
        {
            _optionsObject = optionsObject;
            _requiredOptions = new Queue<MemberInfo>();
            _optionalOptions = new Dictionary<string, MemberInfo>();
            _requiredUsageHelp = new List<string>();

            _properties = new PreprocessorPropertyCollection();

            // Reflect to find what commandline options are available...

            // Fields
            foreach (var field in optionsObject.GetType().GetFields())
            {
                var param = GetAttribute<CommandLineParameterAttribute>(field);
                if (param == null)
                    continue;

                CheckReservedPrefixes(param.Name);

                if (param.Required)
                {
                    // Record a required option.
                    _requiredOptions.Enqueue(field);

                    _requiredUsageHelp.Add(string.Format("<{0}>", param.Name));
                }
                else
                {
                    // Record an optional option.
                    _optionalOptions.Add(param.Name.ToLowerInvariant(), field);
                }
            }

            // Properties
            foreach (var property in optionsObject.GetType().GetProperties())
            {
                var param = GetAttribute<CommandLineParameterAttribute>(property);
                if (param == null)
                    continue;

                CheckReservedPrefixes(param.Name);

                if (param.Required)
                {
                    // Record a required option.
                    _requiredOptions.Enqueue(property);

                    _requiredUsageHelp.Add(string.Format("<{0}>", param.Name));
                }
                else
                {
                    // Record an optional option.
                    _optionalOptions.Add(param.Name.ToLowerInvariant(), property);
                }
            }

            // Methods
            foreach (var method in optionsObject.GetType().GetMethods())
            {
                var param = GetAttribute<CommandLineParameterAttribute>(method);
                if (param == null)
                    continue;

                CheckReservedPrefixes(param.Name);

                // Only accept methods that take less than 1 parameter.
                if (method.GetParameters().Length > 1)
                    throw new NotSupportedException("Methods must have one or zero parameters.");

                if (param.Required)
                {
                    // Record a required option.
                    _requiredOptions.Enqueue(method);

                    _requiredUsageHelp.Add(string.Format("<{0}>", param.Name));
                }
                else
                {
                    // Record an optional option.
                    _optionalOptions.Add(param.Name.ToLowerInvariant(), method);
                }
            }

            _flags = new Dictionary<string, string>();
            foreach(var pair in _optionalOptions)
            {
                var fi = GetAttribute<CommandLineParameterAttribute>(pair.Value);
                if(!string.IsNullOrEmpty(fi.Flag))
                    _flags.Add(fi.Flag, fi.Name);
            }
        }

        private bool Parse(IEnumerable<string> args)
        {   
            var success = true;
            foreach (var arg in args)
            {
                if (!ParseFlags(arg))
                {
                    success = false;
                    break;
                }
            }

            var missingRequiredOption = _requiredOptions.FirstOrDefault(field => !IsList(field) || GetList(field).Count == 0);
            if (missingRequiredOption != null)
            {
                var msg = string.Format("Missing argument '{0}'", GetAttribute<CommandLineParameterAttribute>(missingRequiredOption).Name);
                ShowError(msg);
                return false;
            }

            return success;
        }

        private IEnumerable<string> Preprocess(IEnumerable<string> args)
        {
            var output = new List<string>();
            var lines = new List<string>(args);
            var ifstack = new Stack<Tuple<string, string>>();
            var fileStack = new Stack<string>();

            while (lines.Count > 0)
            {            
                var arg = lines[0];
                lines.RemoveAt(0);

                if (arg.StartsWith("# Begin:"))
                {
                    var file = arg.Substring(8);
                    fileStack.Push(file);
                    continue;
                }

                if (arg.StartsWith("# End:"))
                {
                    fileStack.Pop();
                    continue;
                }

                if (arg.StartsWith("$endif"))
                {
                    ifstack.Pop();
                    continue;
                }
                
                if (ifstack.Count > 0)
                {
                    var skip = false;
                    foreach (var i in ifstack)
                    {
                        var val = _properties[i.Item1];
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
                    var words = arg.Substring(5).Split('=');
                    var name = words[0];
                    var value = words[1];

                    _properties[name] = value;

                    continue;
                }

                if (arg.StartsWith("$if"))
                {
                    if (fileStack.Count == 0)
                        throw new Exception("$if is invalid outside of a response file.");

                    var words = arg.Substring(4).Split('=');
                    var name = words[0];
                    var value = words[1];

                    var condition = new Tuple<string, string>(name, value);
                    ifstack.Push(condition);
                    
                    continue;
                }

                if (arg.StartsWith("/define:") || arg.StartsWith("--define:"))
                {
                    var words = arg.Substring(8).Split('=');
                    var name = words[0];
                    var value = words[1];

                    _properties[name] = value;

                    continue;

                }

                output.Add(arg);
            }

            return output.ToArray();
        }

        internal void ParseFile(string file)
        {
            IEnumerable<string> commands = ReadFile(file);
            commands = Preprocess(commands);
            Parse(commands);
        }

        private static List<string> ReadFile(string file)
        {
            var commands = File.ReadAllLines(file);

            var lines = new List<string>();

            for (var j = 0; j < commands.Length; j++)
            {
                var line = commands[j];
                if (string.IsNullOrEmpty(line))
                    continue;
                if (line.StartsWith("#"))
                    continue;

                lines.Add(line);
            }
            
            return lines;
        }

        private bool ParseFlags(string arg)
        {
            // Only one flag
            if (arg.Length >= 3 &&
                (arg[0] == '-' || arg[0] == '/') &&
                (arg[2] == '=' || arg[2] == ':'))
            {
                string name;
                if (!_flags.TryGetValue(arg[1].ToString(), out name))
                {
                    var msg = string.Format("Unknown option '{0}'", arg[1].ToString());
                    ShowError(msg);
                    return false;
                }

                ParseArgument("/" + name + arg.Substring(2));
                return true;
            }

            // Multiple flags
            if (arg.Length >= 2 &&
               ((arg[0] == '-' && arg[1] != '-') || arg[0] == '/') &&
               !arg.Contains(":") && !arg.Contains("=") &&
               !_optionalOptions.ContainsKey(arg.Substring(1)))
            {
                for (int i = 1; i < arg.Length; i++)
                {
                    string name;
                    if (!_flags.TryGetValue(arg[i].ToString(), out name))
                    {
                        var msg = string.Format("Unknown option '{0}'", arg[i].ToString());
                        ShowError(msg);
                        break;
                    }
                }
            }

            // Not a flag, parse argument
            return ParseArgument(arg);
        }

        private bool ParseArgument(string arg)
        {
            if (arg.StartsWith("/") || arg.StartsWith("--"))
            {
                // After the first escaped argument we can no
                // longer read non-escaped arguments.
                if (_requiredOptions.Count > 0)
                    return false;

                // Parse an optional argument.
                char[] separators = { ':', '=' };

                var split = arg.Substring(arg.StartsWith("/") ? 1 : 2).Split(separators, 2, StringSplitOptions.None);

                var name = split[0];
                var value = (split.Length > 1) ? split[1] : "true";

                MemberInfo member;

                if (!_optionalOptions.TryGetValue(name.ToLowerInvariant(), out member))
                {
                    var msg = string.Format("Unknown option '{0}'", name);
                    ShowError(msg);
                    return false;
                }

                return SetOption(member, value);
            }

            if (_requiredOptions.Count > 0)
            {
                // Parse the next non escaped argument.
                var field = _requiredOptions.Peek();

                if (!IsList(field))
                    _requiredOptions.Dequeue();

                return SetOption(field, arg);
            }

            ShowError("Too many arguments");
            return false;
        }


        bool SetOption(MemberInfo member, string value)
        {
            try
            {
                if (IsList(member))
                {
                    // Append this value to a list of options.
                    GetList(member).Add(ChangeType(value, ListElementType(member)));
                }
                else
                {
                    // Set the value of a single option.
                    if (member is MethodInfo)
                    {
                        var method = member as MethodInfo;
                        var parameters = method.GetParameters();
                        if (parameters.Length == 0)
                            method.Invoke(_optionsObject, null);
                        else
                            method.Invoke(_optionsObject, new[] { ChangeType(value, parameters[0].ParameterType) });
                    }
                    else if (member is FieldInfo)
                    {
                        var field = member as FieldInfo;
                        field.SetValue(_optionsObject, ChangeType(value, field.FieldType));
                    }
                    else 
                    {
                        var property = member as PropertyInfo;
                        property.SetValue(_optionsObject, ChangeType(value, property.PropertyType), null);
                    }
                }

                return true;
            }
            catch
            {
                var msg = string.Format("Invalid value '{0}' for option '{1}'", value, GetAttribute<CommandLineParameterAttribute>(member).Name);
                ShowError(msg);
                return false;
            }
        }

        static readonly string[] ReservedPrefixes = new[]
            {   
                "$",
                "/",                
                "#",
                "--",
                "-"
            };

        static void CheckReservedPrefixes(string str)
        {
            foreach (var i in ReservedPrefixes)
            {
                if (str.StartsWith(i))
                    throw new Exception(string.Format("'{0}' is a reserved prefix and cannot be used at the start of an argument name.", i));
            }
        }

        static object ChangeType(string value, Type type)
        {
            var converter = TypeDescriptor.GetConverter(type);

            return converter.ConvertFromInvariantString(value);
        }


        static bool IsList(MemberInfo member)
        {
            if (member is MethodInfo)
                return false;

            if (member is FieldInfo)
                return typeof(IList).IsAssignableFrom((member as FieldInfo).FieldType);
            
            return typeof(IList).IsAssignableFrom((member as PropertyInfo).PropertyType);
        }


        IList GetList(MemberInfo member)
        {
            if (member is PropertyInfo)
                return (IList)(member as PropertyInfo).GetValue(_optionsObject, null);

            if (member is FieldInfo)
                return (IList)(member as FieldInfo).GetValue(_optionsObject);

            throw new Exception();
        }


        static Type ListElementType(MemberInfo member)
        {
            if (member is FieldInfo)
            {
                var field = member as FieldInfo;
                var interfaces = from i in field.FieldType.GetInterfaces()
                                 where i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEnumerable<>)
                                 select i;

                return interfaces.First().GetGenericArguments()[0];
            }

            if (member is PropertyInfo)
            {
                var property = member as PropertyInfo;
                var interfaces = from i in property.PropertyType.GetInterfaces()
                                 where i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                 select i;

                return interfaces.First().GetGenericArguments()[0];
            }

            throw new ArgumentException("Only FieldInfo and PropertyInfo are valid arguments.", "member");
        }
        
        bool IsWindows()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                case PlatformID.WinCE:
                    return true;
            }
            return false;
        }

        public void ShowError(string message)
        {
            var handler = OnError;
            if (handler == null)
                return;

            OnError(message);
        }


        static T GetAttribute<T>(ICustomAttributeProvider provider) where T : Attribute
        {
            return provider.GetCustomAttributes(typeof(T), false).OfType<T>().FirstOrDefault();
        }
    }

    // Used on an optionsObject field or method to rename the corresponding commandline option.
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method | AttributeTargets.Property)]
    public sealed class CommandLineParameterAttribute : Attribute
    {
        public CommandLineParameterAttribute()
        {
            ValueName = "value";
        }

        public string Name { get; set; }

        public string Flag { get; set; }

        public bool Required { get; set; }

        public string ValueName { get; set; }

        public string Description { get; set; }
    }
}
