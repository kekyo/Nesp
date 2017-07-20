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

namespace Nesp
{
    public sealed class NespDefaultExtension : INespExtension
    {
        public static readonly INespExtension Instance = new NespDefaultExtension();

        private IReadOnlyDictionary<string, MemberInfo[]> cached;

        private NespDefaultExtension()
        {
        }

        internal static IReadOnlyDictionary<string, MemberInfo[]> CreateMembers()
        {
            var reservedTypeNames = new Dictionary<Type, String>
            {
                { typeof(object), "object" },
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(short), "short" },
                { typeof(ushort), "ushort" },
                { typeof(int), "int" },
                { typeof(uint), "uint" },
                { typeof(long), "long" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
                { typeof(bool), "bool" },
                { typeof(string), "string" },
                { typeof(DateTime), "datetime" },
                { typeof(TimeSpan), "timespan" },
                { typeof(Guid), "guid" },
                { typeof(Math), "math" },
                { typeof(Enum), "enum" },
                { typeof(Type), "type" },
            };

            var assemblies = new[] { typeof(object), typeof(Uri), typeof(Enumerable) }
                .Select(type => type.GetTypeInfo().Assembly);
            var members = assemblies
                .SelectMany(assembly => assembly.DefinedTypes)
                .Where(type => (type.IsValueType || type.IsClass) && type.IsPublic)
                .SelectMany(type => type.DeclaredMembers)
                .ToArray();
            var fields =
                from fi in members.OfType<FieldInfo>()
                where fi.IsPublic && fi.IsStatic    // Include enums
                select (MemberInfo)fi;
            var properties =
                (from pi in members.OfType<PropertyInfo>()
                    let getter = pi.GetMethod
                    where pi.CanRead && (getter != null) && getter.IsPublic && getter.IsStatic
                    select new { mi = getter, pi })
                .ToDictionary(entry => entry.mi, entry => (MemberInfo)entry.pi);
            var methods =
                from mi in members.OfType<MethodInfo>()
                where mi.IsPublic && mi.IsStatic && !properties.ContainsKey(mi)
                select (MemberInfo)mi;

            var membersByName =
                from member in properties.Values.Concat(methods).Concat(fields)
                select new
                {
                    FullName = member.DeclaringType.FullName + "." + member.Name,
                    Member = member
                };
            var reservedMembers =
                from member in properties.Values.Concat(methods).Concat(fields)
                let typeName = reservedTypeNames.GetValue(member.DeclaringType)
                where typeName != null
                select new
                {
                    FullName = typeName + "." + member.Name,
                    Member = member
                };

            return
                (from entry in membersByName.Concat(reservedMembers)
                    group entry.Member by entry.FullName)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }

        public Task<IReadOnlyDictionary<string, MemberInfo[]>> GetMembersAsync()
        {
            if (cached != null)
            {
                return Task.FromResult(cached);
            }

            return Task.Run(() =>
            {
                cached = CreateMembers();
                return cached;
            });
        }
    }
}
