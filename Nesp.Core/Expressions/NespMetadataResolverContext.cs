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
        private static readonly NespExpression[] emptyExpressions = new NespExpression[0];

        private readonly object[] locks = Enumerable.Range(0, 3).Select(index => new object()).ToArray();

        private readonly CandidatesDictionary<FieldInfo> fields = new CandidatesDictionary<FieldInfo>();
        private readonly CandidatesDictionary<PropertyInfo> properties = new CandidatesDictionary<PropertyInfo>();
        private readonly CandidatesDictionary<MethodInfo> methods = new CandidatesDictionary<MethodInfo>();

        public NespMetadataResolverContext()
        {
            // TODO: Remove
            this.AddCandidate(typeof(object).GetTypeInfo().Assembly);
        }

        public void AddCandidate(Assembly assembly)
        {
            // Not better performance than other parallelised technology,
            // but this project uses PCL so it impls cannot use Thread, ThreadPool and PLINQ directly.
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

            lock (locks[0])
            {
                foreach (var field in typeInfo.DeclaredFields
                    .Where(field => field.IsPublic && (!typeInfo.IsEnum || !field.IsSpecialName)))
                {
                    var key = $"{typeName}.{field.Name}";
                    fields.AddCandidate(key, field);
                }
            }

            var propertyMethods = new HashSet<MethodInfo>();
            lock (locks[1])
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

            lock (locks[2])
            {
                foreach (var method in typeInfo.DeclaredMethods
                    .Where(method => method.IsPublic && (propertyMethods.Contains(method) == false)))
                {
                    var key = $"{typeName}.{method.Name}";
                    methods.AddCandidate(key, method);
                }
            }
        }

        internal bool EqualCondition(NespMetadataResolverContext cachedContext)
        {
            if (object.ReferenceEquals(this, cachedContext) == false)
            {
                return false;
            }

            return (this.fields.Equals(cachedContext.fields))
                   && (this.properties.Equals(cachedContext.properties))
                   && (this.methods.Equals(cachedContext.methods));
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

        private static int CalculateCandidateScoreByArguments(
            MethodInfo method, NespExpression[] argumentResolvedExpressions)
        {
            // TODO: param array
            var parameterTypes = method.GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray();
            if (parameterTypes.Length != argumentResolvedExpressions.Length)
            {
                // Argument count mismatched.
                return -1;
            }

            var score = 0;
            for (var parameterIndex = 0; parameterIndex < parameterTypes.Length; parameterIndex++)
            {
                var expressionType = argumentResolvedExpressions[parameterIndex].Type;
                if (expressionType == null)
                {
                    // Don't match for expression will be resolving.
                    continue;
                }

                var parameterType = parameterTypes[parameterIndex];
                if (object.ReferenceEquals(parameterType, expressionType))
                {
                    // Exactly match.
                    score += 10000;
                    continue;
                }

                // TODO: Generic type arguments
                var parameterTypeInfo = parameterType.GetTypeInfo();
                var expressionTypeInfo = expressionType.GetTypeInfo();
                if (parameterTypeInfo.IsAssignableFrom(expressionTypeInfo))
                {
                    // Argument can cast implicitly.
                    score += 10;
                }
                else
                {
                    // Don't match
                    return -1;
                }
            }

            return score;
        }

        private static NespExpression ConstructExpressionFromList(
            NespExpression[] listExpressions, NespListExpression untypedExpression)
        {
            // TODO: Property apply

            // Function apply:
            var applyFunction0 = listExpressions[0] as NespApplyFunctionExpression;
            if (applyFunction0 != null)
            {
                var argumentExpresssions = listExpressions
                    .Skip(1)
                    .ToArray();

                var score = CalculateCandidateScoreByArguments(applyFunction0.Method, argumentExpresssions);

                var expr = new NespApplyFunctionExpression(
                    applyFunction0.Method, argumentExpresssions, untypedExpression.Source);
                expr.SetScore(score);
                return expr;
            }

            // Only one expression.
            if (listExpressions.Length == 1)
            {
                // Very low but verified score
                listExpressions[0].SetScore(0);
                return listExpressions[0];
            }
            else
            {
                // Construct list expression.
                // TODO: List type.
                var type = typeof(object[]);
                var expr = new NespResolvedListExpression(listExpressions, type);
                expr.SetScore(0);
                return expr;
            }
        }

        internal NespExpression[] ResolveByList(NespExpression[] list, NespListExpression untypedExpression)
        {
            Debug.Assert(list.Length >= 1);

            var transposedResolvedExpressionLists = TransposeLists(
                list
                .Select(iexpr => iexpr.ResolveMetadata(this))
                .ToArray());

            var filteredCandidatesScored = transposedResolvedExpressionLists
                .Select(resolvedExpressionList => ConstructExpressionFromList(resolvedExpressionList, untypedExpression))
                .Where(scored => scored.Score >= 0)
                .OrderByDescending(scored => scored.Score)
                .ToArray();
            if (filteredCandidatesScored.Length >= 1)
            {
                // Exactly matched
                if (filteredCandidatesScored[0].Score == int.MaxValue)
                {
                    return new [] { filteredCandidatesScored[0] };
                }
                else
                {
                    return filteredCandidatesScored;
                }
            }

            throw new ArgumentException();
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
                    return new NespBoolExpression((bool)value, untypedExpression.Source);
                }
                if (type == typeof(string))
                {
                    return new NespStringExpression((string)value, untypedExpression.Source);
                }
                if (type == typeof(char))
                {
                    return new NespCharExpression((char)value, untypedExpression.Source);
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
                    return new NespNumericExpression(value, untypedExpression.Source);
                }
                if (type.GetTypeInfo().IsEnum)
                {
                    return new NespEnumExpression((Enum)value, untypedExpression.Source);
                }

                return new NespConstantExpression(value, untypedExpression.Source);
            }

            return new NespFieldExpression(field, untypedExpression.Source);
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
                    .Select(property => (NespExpression)new NespPropertyExpression(property, untypedExpression.Source))
                    .ToArray();
            }

            // TODO: Events

            var methodInfos = methods[id];
            if (methodInfos.Length >= 1)
            {
                return methodInfos
                    .Select(method => (NespExpression)new NespApplyFunctionExpression(method, emptyExpressions, untypedExpression.Source))
                    .ToArray();
            }

            throw new ArgumentException();
        }
    }
}
