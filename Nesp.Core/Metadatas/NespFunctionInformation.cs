/////////////////////////////////////////////////////////////////////////////////////////////////
//
// Nesp - A Lisp-like lightweight functional language on .NET
// Copyright (c) 2017 Kouji Matsui (@kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
/////////////////////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.Metadatas
{
    public sealed class NespFunctionParameter
    {
        internal NespFunctionParameter(string name, NespTypeInformation type)
        {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; }
        public NespTypeInformation Type { get; }

        public override string ToString()
        {
            return $"{this.Name}:{this.Type})";
        }
    }

    public sealed class NespFunctionInformation
    {
        private static readonly TypeInfo enumerableTypeInfo = typeof(IEnumerable<>).GetTypeInfo();

        private readonly MethodInfo method;

        internal NespFunctionInformation(MethodInfo method, NespMetadataContext context)
        {
            this.method = method;
            this.DeclaringType = context.FromType(method.DeclaringType.GetTypeInfo());
            this.ReturnType = context.FromType(method.ReturnType.GetTypeInfo());

            var parameters = method.GetParameters();
            this.Parameters = parameters
                .Select(parameter => new NespFunctionParameter(
                    parameter.Name,
                    context.FromType(parameter.ParameterType.GetTypeInfo())))
                .ToArray();
            parameters.LastOrDefault()
                .Match(lastParameter => lastParameter.ParameterType
                    .GetTypeInfo()
                    .CalculateElementType(enumerableTypeInfo)
                    .FirstOrDefault()
                    .Match(lastTypeElement => this.ParamArrayElementType = context.FromType(lastTypeElement)));
        }

        public string Name => method.Name;
        public NespTypeInformation DeclaringType { get; }
        public NespTypeInformation ReturnType { get; }
        public NespFunctionParameter[] Parameters { get; }
        public NespTypeInformation ParamArrayElementType { get; private set; }

        public override string ToString()
        {
            return $"{this.DeclaringType}.{this.Name}({string.Join(",", (object[])this.Parameters)})";
        }
    }
}
