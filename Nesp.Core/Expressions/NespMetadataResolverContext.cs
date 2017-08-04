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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Nesp.Internals;

namespace Nesp.Expressions
{
    public sealed class NespMetadataResolverContext
    {
        private readonly CandidatesDictionary<FieldInfo> fields = new CandidatesDictionary<FieldInfo>();
        private readonly CandidatesDictionary<PropertyInfo> properties = new CandidatesDictionary<PropertyInfo>();
        private readonly CandidatesDictionary<MethodInfo> methods = new CandidatesDictionary<MethodInfo>();

        public NespMetadataResolverContext()
        {
            Task.WaitAll(
                typeof(object).GetTypeInfo().Assembly.DefinedTypes
                    .Where(typeInfo => typeInfo.IsPublic && (typeInfo.IsClass || typeInfo.IsValueType || typeInfo.IsEnum))
                    .Select(typeInfo => Task.Run(() => this.AddCandidate(typeInfo)))
                    .ToArray());
        }

        public void AddCandidate(Assembly assembly)
        {
            Task.WhenAll(
                assembly.DefinedTypes
                .Where(typeInfo => typeInfo.IsPublic && (typeInfo.IsClass || typeInfo.IsValueType || typeInfo.IsEnum))
                .Select(typeInfo => Task.Run(() => this.AddCandidate(typeInfo))))
                .Wait();
        }

        public void AddCandidate(Type type)
        {
            this.AddCandidate(type.GetTypeInfo());
        }

        public void AddCandidate(TypeInfo typeInfo)
        {
            var typeName = NespUtilities.GetReadableTypeName(typeInfo.AsType());

            lock (fields)
            {
                foreach (var field in typeInfo.DeclaredFields
                    .Where(field => field.IsPublic && (!typeInfo.IsEnum || !field.IsSpecialName)))
                {
                    var key = $"{typeName}.{field.Name}";
                    fields.AddCandidate(key, field);
                }
            }

            var propertyMethods = new HashSet<MethodInfo>();
            lock (properties)
            {
                foreach (var property in typeInfo.DeclaredProperties
                    .Where(property => property.CanRead && property.GetMethod.IsPublic))
                {
                    var key = $"{typeName}.{property.Name}";
                    properties.AddCandidate(key, property);

                    propertyMethods.Add(property.GetMethod);
                    propertyMethods.Add(property.SetMethod);
                }
            }

            lock (methods)
            {
                foreach (var method in typeInfo.DeclaredMethods
                    .Where(method => method.IsPublic && (propertyMethods.Contains(method) == false)))
                {
                    var key = $"{typeName}.{method.Name}";
                    methods.AddCandidate(key, method);
                }
            }
        }

        private static NespExpression[][] TransposeLists(NespExpression[][] list)
        {
            var results = new List<NespExpression[]>();
            var targetIndex = new int[list.Length];
            while (true)
            {
                var exprs = new NespExpression[list.Length];
                for (var listIndex = 0; listIndex < list.Length; listIndex++)
                {
                    var iexprs = list[listIndex];
                    exprs[listIndex] = iexprs[targetIndex[listIndex]];
                }

                results.Add(exprs);

                var index = 0;
                for (; index < list.Length; index++)
                {
                    targetIndex[index]++;
                    if (targetIndex[index] < list[index].Length)
                    {
                        break;
                    }
                    targetIndex[index] = 0;
                }

                if (index >= list.Length)
                {
                    break;
                }
            }

            return results.ToArray();
        }

        private static NespExpression ConstructExpressionFromList(NespExpression[] list)
        {
            if (list.Length == 1)
            {
                return list[0];
            }

            // TODO: ResolveMetadataしてしまうと、ResolveByIdAsyncで引数を取らないmethodを検索して失敗してしまう
            var applyFunctionExpr = list[0] as NespApplyFunctionExpression;
            if (applyFunctionExpr != null)
            {
                
            }
            return list[0];
        }

        internal NespExpression[] ResolveByList(NespExpression[] list, NespListExpression untypedExpression)
        {
            Debug.Assert(list.Length >= 1);

            var idExpression0 = list[0] as NespIdExpression;
            if (idExpression0 != null)
            {
                // TODO:
                return list;
            }
            else
            {
                var resolvedExpressionsList = list
                    .Select(iexpr => iexpr.ResolveMetadata(this))
                    .ToArray();

                var transposedLists = TransposeLists(resolvedExpressionsList);

                var filtered = transposedLists
                    .Select(resultlist => ConstructExpressionFromList(resultlist))
                    .Where(iexpr => iexpr != null)
                    .ToArray();

                return filtered;
            }
        }

        private static NespExpression ConstructExpressionFromField(
            FieldInfo field, NespTokenExpression untypedExpression)
        {
            if (field.IsStatic && (field.IsLiteral || field.IsInitOnly))
            {
                var value = field.GetValue(null);

                var type = field.FieldType;
                if (type == typeof(bool))
                {
                    return new NespBoolExpression((bool)value, untypedExpression.Token);
                }
                if (type == typeof(string))
                {
                    return new NespStringExpression((string)value, untypedExpression.Token);
                }
                if (type == typeof(char))
                {
                    return new NespCharExpression((char)value, untypedExpression.Token);
                }
                if ((type == typeof(byte))
                    || (type == typeof(sbyte))
                    || (type == typeof(short))
                    || (type == typeof(ushort))
                    || (type == typeof(int))
                    || (type == typeof(uint))
                    || (type == typeof(long))
                    || (type == typeof(ulong))
                    || (type == typeof(float))
                    || (type == typeof(double))
                    || (type == typeof(decimal)))
                {
                    return new NespNumericExpression(value, untypedExpression.Token);
                }
                if (type.GetTypeInfo().IsEnum)
                {
                    return new NespEnumExpression((Enum)value, untypedExpression.Token);
                }

                return new NespConstantExpression(value, untypedExpression.Token);
            }

            return new NespFieldExpression(field, untypedExpression.Token);
        }

        internal NespExpression[] ResolveById(string id, NespTokenExpression untypedExpression)
        {
            var fieldInfos = fields[id];
            if (fieldInfos.Length >= 1)
            {
                return fieldInfos
                    .Select(field => ConstructExpressionFromField(field, untypedExpression))
                    .ToArray();
            }

            var propertyInfos = properties[id];
            if (propertyInfos.Length >= 1)
            {
                return propertyInfos
                    .Select(property => (NespExpression)new NespPropertyExpression(property, untypedExpression.Token))
                    .ToArray();
            }

            // This situation only id (no list), so selectable methods have to no arguments.
            // (Maybe nothing or only 1 methodInfo)
            var methodInfos = methods[id]
                .Where(method => method.GetParameters().Length == 0)
                .ToArray();
            if (methodInfos.Length >= 1)
            {
                return methodInfos
                    .Select(method => (NespExpression)new NespApplyFunctionExpression(method, untypedExpression.Token))
                    .ToArray();
            }

            throw new ArgumentException();
        }
    }
}
