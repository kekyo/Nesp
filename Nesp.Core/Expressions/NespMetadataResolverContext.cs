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
            foreach (var typeInfo in
                typeof(object).GetTypeInfo().Assembly.DefinedTypes
                .Where(typeInfo =>
                    typeInfo.IsPublic && (typeInfo.IsClass || typeInfo.IsValueType || typeInfo.IsEnum)))
            {
                this.AddCandidate(typeInfo);
            }
        }

        public void AddCandidate(Type type)
        {
            this.AddCandidate(type.GetTypeInfo());
        }

        public void AddCandidate(TypeInfo typeInfo)
        {
            var typeName = NespUtilities.GetReadableTypeName(typeInfo.AsType());

            foreach (var field in typeInfo.DeclaredFields
                .Where(field => field.IsPublic && (!typeInfo.IsEnum || !field.IsSpecialName)))
            {
                var key = $"{typeName}.{field.Name}";
                fields.AddCandidate(key, field);
            }

            var propertyMethods = new HashSet<MethodInfo>();
            foreach (var property in typeInfo.DeclaredProperties
                .Where(property => property.CanRead && property.GetMethod.IsPublic))
            {
                var key = $"{typeName}.{property.Name}";
                properties.AddCandidate(key, property);

                propertyMethods.Add(property.GetMethod);
                propertyMethods.Add(property.SetMethod);
            }

            foreach (var method in typeInfo.DeclaredMethods
                .Where(method => method.IsPublic && (propertyMethods.Contains(method) == false)))
            {
                var key = $"{typeName}.{method.Name}";
                methods.AddCandidate(key, method);
            }
        }

        internal async Task<NespExpression[]> ResolveListAsync(
            NespExpression[] list, NespListExpression untypedExpression)
        {
            var resolvedList = await Task.WhenAll(list.Select(iexpr => iexpr.ResolveMetadataAsync(this)));
            if (resolvedList.Length == 1)
            {
                return resolvedList[0];
            }
            else
            {
                return resolvedList.Select(iexprs => new NespListExpression(iexprs)).ToArray();
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

        internal Task<NespExpression[]> ResolveIdAsync(
            string id, NespTokenExpression untypedExpression)
        {
            var fieldInfos = fields[id];
            if (fieldInfos.Length >= 1)
            {
                return Task.FromResult(
                    fieldInfos.Select(field => ConstructExpressionFromField(field, untypedExpression)).ToArray());
            }

            var propertyInfos = properties[id];
            if (propertyInfos.Length >= 1)
            {
                return Task.FromResult(
                    propertyInfos.Select(property => (NespExpression)new NespPropertyExpression(property, untypedExpression.Token)).ToArray());
            }

            var methodInfos = methods[id];
            if (methodInfos.Length >= 1)
            {
                return Task.FromResult(
                    methodInfos.Select(method => (NespExpression)new NespApplyFunctionExpression(method, untypedExpression.Token)).ToArray());
            }

            throw new ArgumentException();
        }
    }
}
