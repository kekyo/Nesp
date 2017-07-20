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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nesp.Extensions
{
    internal sealed class MemberExtractor
    {
        private static readonly Dictionary<string, string> operatorNames =
            new Dictionary<string, string>
            {
                { "op_Addition", "+" },
                { "op_Subtraction", "-" },
                { "op_Multiply", "*" },
                { "op_Division", "/" },
                { "op_Modulus", "%" },
            };

        public MemberExtractor(IEnumerable<Assembly> assemblies)
            : this(assemblies.SelectMany(assembly => assembly.DefinedTypes).Where(type => type.IsPublic))
        {
        }

        public MemberExtractor(IEnumerable<Type> types)
            : this(types.Select(type => type.GetTypeInfo()))
        {
        }

        public MemberExtractor(IEnumerable<TypeInfo> types)
        {
            var members = types
                .Where(type => (type.IsValueType || type.IsClass))
                .SelectMany(type => type.DeclaredMembers)
                .ToArray();
            this.Fields =
                (from fi in members.OfType<FieldInfo>()
                 where fi.IsPublic && fi.IsStatic    // Include enums
                 select fi)
                .ToArray();
            var properties =
                (from pi in members.OfType<PropertyInfo>()
                 let getter = pi.GetMethod
                 where pi.CanRead && (getter != null) && getter.IsPublic && getter.IsStatic
                 select new { mi = getter, pi })
                .ToDictionary(entry => entry.mi, entry => entry.pi);
            this.Properties =
                properties.Values.ToArray();
            this.Methods =
                (from mi in members.OfType<MethodInfo>()
                 where mi.IsPublic && mi.IsStatic && !properties.ContainsKey(mi)
                 select mi)
                .ToArray();

            this.MembersByName =
                (from member in
                    ((MemberInfo[]) this.Properties)
                    .Concat(this.Fields)
                    .Concat(this.Methods)
                 let name = GetMemberName(member)
                 group member by name)
                .ToDictionary(g => g.Key, g => g.Distinct().ToArray());
        }

        private static string GetMemberName(MemberInfo member)
        {
            var memberBind = member.GetCustomAttribute<MemberBindAttribute>();
            if (memberBind != null)
            {
                return memberBind.MemberName;
            }
            if (operatorNames.TryGetValue(member.Name, out var name))
            {
                return name;
            }

            return member.DeclaringType.FullName + "." + member.Name;
        }

        public readonly FieldInfo[] Fields;
        public readonly PropertyInfo[] Properties;
        public readonly MethodInfo[] Methods;
        public readonly IReadOnlyDictionary<string, MemberInfo[]> MembersByName;
    }
}
