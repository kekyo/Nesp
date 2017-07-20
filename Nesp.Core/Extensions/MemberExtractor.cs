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
        public MemberExtractor(IEnumerable<Assembly> assemblies)
            : this(assemblies.SelectMany(assembly => assembly.DefinedTypes))
        {
        }

        public MemberExtractor(IEnumerable<Type> types)
            : this(types.Select(type => type.GetTypeInfo()))
        {
        }

        public MemberExtractor(IEnumerable<TypeInfo> types)
        {
            var members = types
                .Where(type => (type.IsValueType || type.IsClass) && type.IsPublic)
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
                 let fullName = member.DeclaringType.FullName + "." + member.Name
                 group member by fullName)
                .ToDictionary(g => g.Key, g => g.Distinct().ToArray());
        }

        public readonly FieldInfo[] Fields;
        public readonly PropertyInfo[] Properties;
        public readonly MethodInfo[] Methods;
        public readonly IReadOnlyDictionary<string, MemberInfo[]> MembersByName;
    }
}
