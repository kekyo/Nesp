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
    public interface IMemberProducer
    {
        IReadOnlyDictionary<string, Type[]> TypesByName { get; }
        IReadOnlyDictionary<string, FieldInfo[]> FieldsByName { get; }
        IReadOnlyDictionary<string, PropertyInfo[]> PropertiesByName { get; }
        IReadOnlyDictionary<string, MethodInfo[]> MethodsByName { get; }
    }

    public sealed class MemberExtractor : IMemberProducer
    {
        private static readonly Dictionary<string, string> operatorNames =
            new Dictionary<string, string>
            {
                // For C#
                { "op_Addition", "+" },
                { "op_Subtraction", "-" },
                { "op_Multiply", "*" },
                { "op_Division", "/" },
                { "op_Modulus", "%" },
                { "op_Equality", "==" },
                { "op_Inequality", "!=" },
                { "op_GreaterThan", ">" },
                { "op_GreaterThanOrEqual", ">=" },
                { "op_LessThan", "<" },
                { "op_LessThanOrEqual", "<=" },
                { "op_Increment", "++" },
                { "op_Decrement", "--" },
                { "op_BitwiseOr", "|" },
                { "op_BitwiseAnd", "&" },
                { "op_ExclusiveOr", "^" },
                { "op_OnesComplement", "~" },
                { "op_LogicalNot", "!" },
                { "op_LeftShift", "<<" },
                { "op_RightShift", ">>" },

                // For F#
                { "op_Nil", "[]" },
                { "op_Cons", "::" },
                { "op_Append", "@" },
                { "op_Concatenate", "^" },
                { "op_UnaryPlus", "~+" },
                { "op_UnaryNegation", "~-" },
                { "op_Dynamic", "?" },
                { "op_PipeLeft", "<|" },
                { "op_PipeRight", "|>" },
                { "op_Dereference", "!" },
                { "op_ComposeLeft", "<<" },
                { "op_ComposeRight", ">>" },
                { "op_Range", ".." },
            };

        public MemberExtractor(IEnumerable<Assembly> assemblies)
            : this(assemblies.SelectMany(assembly => assembly.DefinedTypes).Where(typeInfo => typeInfo.IsPublic))
        {
        }

        public MemberExtractor(params Type[] types)
            : this((IEnumerable<Type>)types)
        {
        }

        public MemberExtractor(IEnumerable<Type> types)
            : this(types.Select(typeInfo => typeInfo.GetTypeInfo()))
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
                    g => g.Distinct().Select(typeInfo => typeInfo.AsType()).ToArray());

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

        private static string GetReadableTypeName(Type type)
        {
            return NespReflectionUtilities.GetReadableTypeName(type, GetReadableTypeName);
        }

        private static string GetTypeName(TypeInfo typeInfo)
        {
            var memberBind = typeInfo.GetCustomAttribute<MemberBindAttribute>();
            return (memberBind != null)
                ? memberBind.MemberName
                : GetReadableTypeName(typeInfo.AsType());
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

            var type = member.AsType();
            if (type != null)
            {
                return GetReadableTypeName(type);
            }

            return member.DeclaringType.FullName + "." + member.Name;
        }

        public IReadOnlyDictionary<string, Type[]> TypesByName { get; }
        public IReadOnlyDictionary<string, FieldInfo[]> FieldsByName { get; }
        public IReadOnlyDictionary<string, PropertyInfo[]> PropertiesByName { get; }
        public IReadOnlyDictionary<string, MethodInfo[]> MethodsByName { get; }
    }
}
