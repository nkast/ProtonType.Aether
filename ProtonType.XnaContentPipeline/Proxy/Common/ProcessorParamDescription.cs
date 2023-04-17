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

namespace nkast.ProtonType.XnaContentPipeline.Common
{
    [Serializable]
    public struct ProcessorParamDescription
    {
        public readonly string Name;
        public readonly string TypeName;
        public readonly string TypeFullName;
        public readonly string TypeAssemblyQualifiedName;
        public readonly string DefaultValue;
        public readonly string[] StandardValues;

        public ProcessorParamDescription(string name, string typeName, string typeFullName, string typeAssemblyQualifiedName, string defaultValue, string[] standardValues)
        {
            this.Name = name;
            this.TypeName = typeName;
            this.TypeFullName = typeFullName;
            this.TypeAssemblyQualifiedName = typeAssemblyQualifiedName;
            this.DefaultValue = defaultValue;
            this.StandardValues = standardValues;
        }
        
        public Type GetParamType()
        {
            string typeAssemblyQualifiedName =TypeAssemblyQualifiedName;
            typeAssemblyQualifiedName = MG2XNATypeAssemblyQualifiedName(typeAssemblyQualifiedName);
            return Type.GetType(typeAssemblyQualifiedName);
        }

        private static string MG2XNATypeAssemblyQualifiedName(string typeAssemblyQualifiedName)
        {
#if XNA
            if (typeAssemblyQualifiedName.Contains(", MonoGame.Framework"))
            {
                var t = typeAssemblyQualifiedName.Split(',');
                t[1] = t[1].Replace("MonoGame.Framework", "Microsoft.Xna.Framework");
                Array.Resize(ref t, 2); // keep only type and assembly name. Remove version, etc
                typeAssemblyQualifiedName = String.Join(",", t);
            }
#endif
            return typeAssemblyQualifiedName;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
