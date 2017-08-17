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

using Nesp.Internals;

namespace Nesp.Extensions
{
    public sealed class MemberExtractor : IMemberProducer
    {
        public MemberExtractor(IEnumerable<Assembly> assemblies)
            : this(assemblies.SelectMany(assembly => assembly.DefinedTypes).Where(typeInfo => typeInfo.IsPublic))
        {
        }

        public MemberExtractor(params TypeInfo[] types)
            : this((IEnumerable<TypeInfo>)types)
        {
        }

        public MemberExtractor(IEnumerable<TypeInfo> typeInfos)
        {
            var classOrValueTypeInfos = typeInfos
                .Where(type => type.IsValueType || type.IsClass)
                .ToArray();

            this.TypesByName =
                (from typeInfo in classOrValueTypeInfos
                 let typeName = GetTypeName(typeInfo)
                 group typeInfo by typeName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Distinct().ToArray());

            this.FieldsByName =
                (from typeInfo in classOrValueTypeInfos
                 from fi in typeInfo.DeclaredFields
                 where fi.IsPublic && fi.IsStatic    // Include enums
                 let fullName = GetMemberName(fi)
                 group fi by fullName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Distinct().ToArray());

            var properties =
                (from typeInfo in classOrValueTypeInfos
                 from pi in typeInfo.DeclaredProperties
                 let getter = pi.GetMethod
                 where pi.CanRead && (getter != null) && getter.IsPublic && getter.IsStatic
                 select new { mi = getter, pi })
                .ToDictionary(entry => entry.mi, entry => entry.pi);

            this.PropertiesByName =
                (from entry in properties
                 let fullName = GetMemberName(entry.Value)
                 group entry.Value by fullName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Distinct().ToArray());

            this.MethodsByName =
                (from typeInfo in classOrValueTypeInfos
                 from mi in typeInfo.DeclaredMethods
                 where mi.IsPublic && mi.IsStatic && !properties.ContainsKey(mi)
                 let fullName = GetMemberName(mi)
                 group mi by fullName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Distinct().ToArray());
        }

        private static string GetReadableTypeName(TypeInfo type)
        {
            return NespUtilities.GetReadableTypeName(type, GetReadableTypeName);
        }

        private static string GetTypeName(TypeInfo typeInfo)
        {
            var memberBind = typeInfo.GetCustomAttribute<MemberBindAttribute>();
            return (memberBind != null)
                ? memberBind.MemberName
                : GetReadableTypeName(typeInfo);
        }

        private static string GetMemberName(MemberInfo member)
        {
            var memberBind = member.GetCustomAttribute<MemberBindAttribute>();
            if (memberBind != null)
            {
                return memberBind.MemberName;
            }
            if (NespUtilities.OperatorMethodNames.TryGetValue(member.Name, out var name))
            {
                return name;
            }

            return member.DeclaringType.FullName + "." + member.Name;
        }

        public IReadOnlyDictionary<string, TypeInfo[]> TypesByName { get; }
        public IReadOnlyDictionary<string, FieldInfo[]> FieldsByName { get; }
        public IReadOnlyDictionary<string, PropertyInfo[]> PropertiesByName { get; }
        public IReadOnlyDictionary<string, MethodInfo[]> MethodsByName { get; }
    }
}
