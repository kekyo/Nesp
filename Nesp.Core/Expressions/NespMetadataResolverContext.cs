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
using Nesp.Expressions.Abstracts;
using Nesp.Expressions.Resolved;
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

        /// <summary>
        /// Transpose expression lists.
        /// </summary>
        /// <param name="list">Expression lists</param>
        /// <returns>Transposed expression lists</returns>
        /// <remarks>This method compute transpose for expression list.</remarks>
        private static NespExpression[][] TransposeLists(NespExpression[][] list)
        {
            // From list (These expression lists are candidate argument expressions):
            //   Target: string GetString(int a0, char a1, double a2, object a3)
            //   [a0]: { [a00], [a01], [a02] }          // Argument a0's expression candidates are a00 or a01 or a02
            //   [a1]: { [a10], [a11] }                 // Argument a1's expression candidates are a10 or a11
            //   [a2]: { [a20], [a21], [a22] }          // Argument a2's expression candidates are a20 or a21 or a22
            //   [a3]: { [a30], [a31], [a32], [a33] }   // Argument a3's expression candidates are a30 or a31 or a32 or a33

            // Transposed list (Completely arguments composed candidate list):
            //   [0]:  { [a00], [a10], [a20], [a30] }   // string GetString(a00, a10, a20, a30)
            //   [1]:  { [a01], [a10], [a20], [a30] }   // string GetString(a01, a10, a20, a30)
            //   [2]:  { [a02], [a10], [a20], [a30] }   // string GetString(a02, a10, a20, a30)
            //   [3]:  { [a00], [a11], [a20], [a30] }   // string GetString(a00, a11, a20, a30)
            //   [4]:  { [a01], [a11], [a20], [a30] }   // string GetString(a01, a11, a20, a30)
            //   [5]:  { [a02], [a11], [a20], [a30] }   // string GetString(a02, a11, a20, a30)
            //   [6]:  { [a00], [a10], [a21], [a30] }   // string GetString(a00, a10, a21, a30)
            //   [7]:  { [a01], [a10], [a21], [a30] }   // string GetString(a01, a10, a21, a30)
            //   [8]:  { [a02], [a10], [a21], [a30] }   // string GetString(a02, a10, a21, a30)
            //   [9]:  { [a00], [a11], [a21], [a30] }   // string GetString(a00, a11, a21, a30)
            //   [10]: { [a01], [a11], [a21], [a30] }   // string GetString(a01, a11, a21, a30)
            //   [11]: { [a02], [a11], [a21], [a30] }   // string GetString(a02, a11, a21, a30)
            //   [12]: { [a00], [a10], [a22], [a30] }   // string GetString(a00, a10, a22, a30)
            //   [13]: { [a01], [a10], [a22], [a30] }   // string GetString(a01, a10, a22, a30)
            //
            //            ...
            //
            //   [68]: { [a02], [a10], [a22], [a33] }   // string GetString(a02, a10, a22, a33)
            //   [69]: { [a00], [a11], [a22], [a33] }   // string GetString(a00, a11, a22, a33)
            //   [70]: { [a01], [a11], [a22], [a33] }   // string GetString(a01, a11, a22, a33)
            //   [71]: { [a02], [a11], [a22], [a33] }   // string GetString(a02, a11, a22, a33)

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

        /// <summary>
        /// Calculate how adaptable do apply this method's parametes and expressions.
        /// </summary>
        /// <param name="method">Target method</param>
        /// <param name="argumentResolvedExpressions">Target expressions (require resolved)</param>
        /// <returns>Adaptable score (null: cannot use, 0 >= better for large amount value, int.MaxValue: exactly matched)</returns>
        private static ulong? CalculateAdaptableScoreByArguments(
            MethodInfo method, NespExpression[] argumentResolvedExpressions)
        {
            Debug.Assert(argumentResolvedExpressions.All(iexpr => iexpr.IsResolved));
            Debug.Assert(argumentResolvedExpressions.Length <= 31);

            var parameters = method.GetParameters();
            if (parameters.Length > argumentResolvedExpressions.Length)
            {
                // Argument count mismatched.
                return null;
            }

            // Step 1: Do match between first and last (or first of variable)
            var exactlyCount = 0;
            var score = 0UL;
            for (var index = 0; index < parameters.Length; index++)
            {
                var expressionType = argumentResolvedExpressions[index].Type;
                if (expressionType == null)
                {
                    // Don't match for expression will be resolving.
                    continue;
                }

                var parameterType = parameters[index].ParameterType;
                if (object.ReferenceEquals(parameterType, expressionType))
                {
                    // Exactly match.
                    score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                    exactlyCount++;
                    continue;
                }

                // TODO: Generic type arguments
                var parameterTypeInfo = parameterType.GetTypeInfo();
                var expressionTypeInfo = expressionType.GetTypeInfo();
                if (parameterTypeInfo.IsAssignableFrom(expressionTypeInfo))
                {
                    // Argument can cast implicitly.
                    score += 2UL << ((argumentResolvedExpressions.Length - index) * 2);
                    continue;
                }

                if (((index + 1) == parameters.Length)
                    && (parameters[index].IsDefined(typeof(ParamArrayAttribute))))
                {
                    // TODO: Support seq<T>
                    var elementType = parameterType.GetElementType();
                    if (object.ReferenceEquals(elementType, expressionType))
                    {
                        // Exactly match in variable argument.
                        score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                        exactlyCount++;
                        continue;
                    }

                    // TODO: Generic type arguments
                    var elementTypeInfo = elementType.GetTypeInfo();
                    if (elementTypeInfo.IsAssignableFrom(expressionTypeInfo))
                    {
                        // Argument can cast implicitly.
                        score += 2UL << ((argumentResolvedExpressions.Length - index) * 2);
                        continue;
                    }
                }

                // Can't match
                return null;
            }

            // Step 2: Do match variables
            var parameterLast = parameters.LastOrDefault();
            if (parameterLast?.IsDefined(typeof(ParamArrayAttribute)) ?? false)
            {
                var elementType = parameters[parameters.Length - 1].ParameterType.GetElementType();
                var elementTypeInfo = elementType.GetTypeInfo();

                // Variable first is already verified.
                for (var index = parameters.Length; index < argumentResolvedExpressions.Length; index++)
                {
                    var expressionType = argumentResolvedExpressions[index].Type;
                    if (expressionType == null)
                    {
                        // Don't match for expression will be resolving.
                        continue;
                    }

                    if (object.ReferenceEquals(elementType, expressionType))
                    {
                        // Exactly match.
                        score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                        exactlyCount++;
                        continue;
                    }

                    // TODO: Generic type arguments
                    var expressionTypeInfo = expressionType.GetTypeInfo();
                    if (elementTypeInfo.IsAssignableFrom(expressionTypeInfo))
                    {
                        // Argument can cast implicitly.
                        score += 2UL << ((argumentResolvedExpressions.Length - index) * 2);
                        continue;
                    }

                    // Can't match
                    return null;
                }
            }

            // Maximum score gives if all arguments exactly matched.
            return (exactlyCount >= argumentResolvedExpressions.Length) ? ulong.MaxValue : score;
        }

        /// <summary>
        /// Construct expression from list expressions.
        /// </summary>
        /// <param name="listExpressions">List contained expressions.</param>
        /// <param name="untypedExpression">Target untyped expression reference.</param>
        /// <returns>Expression (resolved)</returns>
        private static NespExpression ConstructExpressionFromList(
            NespExpression[] listExpressions, NespListExpression untypedExpression)
        {
            // List expressions are:
            //   Target: (a0 a1 a2 a3)
            //   listExpressions = { [a00], [a10], [a20], [a30] }    // Transposed a candidate list.

            // TODO: Apply indexer

            // Apply function:
            //   Target: (a0 a1 a2 a3)
            //            |  |  |  |
            //            |  |  |  a30
            //            |  |  a20
            //            |  a10
            //            a00: Foo.GetString(int, char, double)
            var applyFunction0 = listExpressions[0] as NespApplyFunctionExpression;
            if (applyFunction0 != null)
            {
                // Calculate adaptable score.
                var argumentExpresssions = listExpressions
                    .Skip(1)
                    .ToArray();
                var score = CalculateAdaptableScoreByArguments(applyFunction0.Method, argumentExpresssions);
                if (score == null)
                {
                    // Can't match
                    return null;
                }

                // Construct function apply expression.
                var expr = new NespApplyFunctionExpression(
                    applyFunction0.Method, argumentExpresssions, untypedExpression.Source);
                expr.SetScore(score.Value);
                return expr;
            }

            // Only one expression.
            if (listExpressions.Length == 1)
            {
                // Very low but adaptable.
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

        /// <summary>
        /// Resolve by expression list.
        /// </summary>
        /// <param name="list">Expression list</param>
        /// <param name="untypedExpression">Target untyped expression reference.</param>
        /// <returns>Expression (resolved)</returns>
        internal NespExpression[] ResolveByList(NespExpression[] list, NespListExpression untypedExpression)
        {
            Debug.Assert(list.Length >= 1);

            var transposedResolvedExpressionLists = TransposeLists(
                list
                .Select(iexpr => iexpr.ResolveMetadata(this))
                .ToArray());

            var filteredCandidatesScored = transposedResolvedExpressionLists
                .Select(resolvedExpressionList => ConstructExpressionFromList(resolvedExpressionList, untypedExpression))
                .Where(scored => scored != null)
                .OrderByDescending(scored => scored.Score)
                .ToArray();
            if (filteredCandidatesScored.Length >= 1)
            {
                // All arguments exactly matched.
                if (filteredCandidatesScored[0].Score == ulong.MaxValue)
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

        /// <summary>
        /// Construct expression from field information.
        /// </summary>
        /// <param name="field">Field information.</param>
        /// <param name="untypedExpression">Target untyped expression reference.</param>
        /// <returns>Expression (resolved)</returns>
        /// <remarks>This method construct expression from field.
        /// If field gives concrete value (Literal or marked initonly),
        /// get real value at this point and construct constant expression.</remarks>
        private static NespExpression ConstructExpressionFromField(
            FieldInfo field, NespTokenExpression untypedExpression)
        {
            // Field is literal or marked initonly.
            if (field.IsStatic && (field.IsLiteral || field.IsInitOnly))
            {
                // Get real value.
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

            // Field reference resolved at runtime.
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
