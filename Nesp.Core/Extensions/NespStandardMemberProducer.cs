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
using System.Threading.Tasks;

namespace Nesp.Extensions
{
    internal sealed class NespStandardMemberProducer : IMemberProducer
    {
        public NespStandardMemberProducer(IMemberProducer members)
        {
            this.TypesByName = FixupMemberNames(members.TypesByName, GetTypeName);
            this.FieldsByName = FixupMemberNames(members.FieldsByName, GetMemberName);
            this.PropertiesByName = FixupMemberNames(members.PropertiesByName, GetMemberName);
            this.MethodsByName = FixupMemberNames(members.MethodsByName, GetMemberName);
        }

        private static IReadOnlyDictionary<string, T[]> FixupMemberNames<T>(
            IReadOnlyDictionary<string, T[]> members, Func<T, string, string> getName)
        {
            return
                (from entry in members
                 from member in entry.Value
                 let name = getName(member, entry.Key)
                 group member by name)
                .ToDictionary(
                    g => g.Key,
                    g => g.Distinct().ToArray());
        }

        private static string GetTypeName(Type type, string fallbackName)
        {
            return NespStandardExtension.ReservedTypeNames.TryGetValue(type, out var typeName)
                ? typeName
                : fallbackName;
        }

        private static string GetMemberName(MemberInfo member, string fallbackName)
        {
            var type = member.DeclaringType;
            return NespStandardExtension.ReservedTypeNames.TryGetValue(type, out var typeName)
                ? typeName + "." + member.Name
                : fallbackName;
        }

        public IReadOnlyDictionary<string, Type[]> TypesByName { get; }
        public IReadOnlyDictionary<string, FieldInfo[]> FieldsByName { get; }
        public IReadOnlyDictionary<string, PropertyInfo[]> PropertiesByName { get; }
        public IReadOnlyDictionary<string, MethodInfo[]> MethodsByName { get; }
    }
}
