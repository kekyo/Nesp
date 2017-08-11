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

        private readonly CandidatesDictionary<FieldInfo> fields = new CandidatesDictionary<FieldInfo>();
        private readonly CandidatesDictionary<PropertyInfo> properties = new CandidatesDictionary<PropertyInfo>();
        private readonly CandidatesDictionary<MethodInfo> methods = new CandidatesDictionary<MethodInfo>();
        private readonly CandidatesDictionary<NespReferenceSymbolExpression> symbols = new CandidatesDictionary<NespReferenceSymbolExpression>();

        public NespMetadataResolverContext()
        {
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

        #region List
        /// <summary>
        /// Transpose expression lists.
        /// </summary>
        /// <param name="list">Expression lists</param>
        /// <returns>Transposed expression lists</returns>
        /// <remarks>This method compute transpose for expression list.</remarks>
        private static IEnumerable<TExpression[]> TransposeLists<TExpression>(TExpression[][] list)
            where TExpression : NespExpression
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

            var indexes = new int[list.Length];
            var index = 0;
            while (index < list.Length)
            {
                var exprs = new TExpression[list.Length];
                for (var listIndex = 0; listIndex < list.Length; listIndex++)
                {
                    var iexprs = list[listIndex];
                    var iexpr = iexprs[indexes[listIndex]];
                    exprs[listIndex] = iexpr;
                }

                yield return exprs;

                for (index = 0; index < list.Length; index++)
                {
                    indexes[index]++;
                    if (indexes[index] < list[index].Length)
                    {
                        break;
                    }
                    indexes[index] = 0;
                }
            }
        }

        /// <summary>
        /// Calculate how adaptable do apply this method's parametes and expressions.
        /// </summary>
        /// <param name="method">Target method</param>
        /// <param name="argumentResolvedExpressions">Target expressions (require resolved)</param>
        /// <returns>Adaptable score (null: cannot use, 0 >= better for large amount value, int.MaxValue: exactly matched)</returns>
        private static ulong? CalculateAdaptableScoreByArguments(
            MethodInfo method, NespResolvedExpression[] argumentResolvedExpressions)
        {
            Debug.Assert(argumentResolvedExpressions.Length <= 31);

            // Lack for arguments.
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
            // Too many arguments.
            else if (parameters.Length < argumentResolvedExpressions.Length)
            {
                // Argument count mismatched.
                return null;
            }

            // Maximum score gives if all arguments exactly matched.
            return (exactlyCount >= argumentResolvedExpressions.Length) ? ulong.MaxValue : score;
        }

        /// <summary>
        /// Construct expression from list expressions.
        /// </summary>
        /// <param name="listExpressions">List contained expressions.</param>
        /// <param name="unwrapListIfSingle">Require unwrap list if list contains only a expression.</param>
        /// <param name="untypedExpression">Target untyped expression reference.</param>
        /// <returns>Expression (resolved)</returns>
        private NespResolvedExpression ConstructExpressionFromList(
            NespResolvedExpression[] listExpressions, bool unwrapListIfSingle, NespAbstractListExpression untypedExpression)
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
            var applyFunction0 = listExpressions.FirstOrDefault() as NespApplyFunctionExpression;
            if (applyFunction0 != null)
            {
                // Calculate adaptable score.
                var argumentExpressions = listExpressions
                    .Skip(1)
                    .ToArray();
                var score = CalculateAdaptableScoreByArguments(applyFunction0.Method, argumentExpressions);
                if (score == null)
                {
                    // Can't match
                    return null;
                }

                // Construct function apply expression.
                var expr = new NespApplyFunctionExpression(
                    applyFunction0.Method, argumentExpressions, untypedExpression.Source);
                expr.SetScore(score.Value);
                return expr;
            }

            // Define lambda:
            //   Target: (a0 a1 a2 a3)
            //            |  |  |  |
            //            |  |  |  a30: bodyExpression
            //            |  |  a20: parameterList (Each unique instance)
            //            |  a10: symbolName
            //            a00: 'define'
            var defineExpr0 = listExpressions.FirstOrDefault() as NespReferenceSymbolExpression;
            if (defineExpr0?.Symbol == "define")
            {
                // TODO: Bind expression

                if (listExpressions.Length == 4)
                {
                    // Get symbol name, parameters and body.
                    var symbolNameExpression = listExpressions[1] as NespReferenceSymbolExpression;
                    var parametersExpression = listExpressions[2] as NespResolvedListExpression;
                    var bodyExpression = listExpressions[3];

                    if ((symbolNameExpression != null)
                        && (parametersExpression != null)
                        && (bodyExpression != null))
                    {
                        var preParameterExpressions = parametersExpression.List
                            .Select(iexpr => iexpr as NespReferenceSymbolExpression)
                            .ToArray();
                        if ((preParameterExpressions.Length == 0)
                            || (preParameterExpressions.All(iexpr => iexpr != null)))
                        {
                            // Convert to truely parameter expressions.
                            var parameterExpressions = preParameterExpressions
                                .Select(pexpr => new NespParameterExpression(pexpr.Symbol, pexpr.Type, pexpr.Source))
                                .ToArray();

                            // Deshadowing by parameters.
                            foreach (var pexpr in parameterExpressions)
                            {
                                var rsexpr = symbols.RemoveCandidateLatest(pexpr.Symbol);
                                rsexpr?.SetRelated(pexpr);
                            }

                            // TODO: Undefined parameter symbols.

                            // Construct lambda expression.
                            var lambdaExpression = new NespDefineLambdaExpression(
                                symbolNameExpression.Symbol, bodyExpression, parameterExpressions,
                                untypedExpression.Source);

                            lambdaExpression.SetScore(bodyExpression.Score);
                            return lambdaExpression;
                        }
                    }
                }
            }

            // If requested unwrap and list is single.
            if (unwrapListIfSingle && (listExpressions.Length == 1))
            {
                // Turn to a expression.
                return listExpressions[0];
            }
            else
            {
                // Construct list expression.
                // TODO: List type.
                var type = typeof(object[]);
                var expr = new NespResolvedListExpression(listExpressions, type, untypedExpression.Source);
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
        internal NespResolvedExpression[] ResolveByList(NespExpression[] list, NespListExpression untypedExpression)
        {
            // TODO: Insert handler for expression extension.

            // Expression: "" (nothing)
            // This pattern is only top level expression (body --> list).
            // Resolved result will be unit expression if list contains only a value.
            if (list.Length == 0)
            {
                return new NespResolvedExpression[] { new NespUnitExpression(untypedExpression.Source) };
            }

            var transposedResolvedExpressionLists = TransposeLists(list
                .Select(iexpr => iexpr.IsResolved ? new[] { (NespResolvedExpression)iexpr } : ((NespAbstractExpression)iexpr).ResolveMetadata(this))
                .ToArray());

            // Unwrap if list contains only a value.
            // TODO: Require collect ability for mismatched expression (For use intellisense)
            var filteredCandidatesScored = transposedResolvedExpressionLists
                .Select(resolvedExpressionList => ConstructExpressionFromList(resolvedExpressionList, true, untypedExpression))
                .Where(scored => scored != null)
                .OrderByDescending(scored => scored.Score)
                .ToArray();

            // TODO: If results are empty --> invalid exprs.

            // If all expressions exactly resolved (not contains generic types), first expression is best result.
            if (filteredCandidatesScored.All(iexpr => iexpr.Type != null))
            {
                return new[] { filteredCandidatesScored[0] };
            }
            else
            {
                return filteredCandidatesScored;
            }
        }

        /// <summary>
        /// Resolve by expression list.
        /// </summary>
        /// <param name="list">Expression list</param>
        /// <param name="untypedExpression">Target untyped expression reference.</param>
        /// <returns>Expression (resolved)</returns>
        internal NespResolvedExpression[] ResolveByBracketedList(NespExpression[] list, NespBracketedListExpression untypedExpression)
        {
            // TODO: Insert handler for expression extension.

            // Expression: "()"
            // Resolved result will be empty list expression.
            if (list.Length == 0)
            {
                // TODO: List type.
                var type = typeof(object[]);
                return new NespResolvedExpression[]
                {
                    new NespResolvedListExpression(new NespExpression[0], type, untypedExpression.Source)
                };
            }

            var transposedResolvedExpressionLists = TransposeLists(list
                .Select(iexpr => iexpr.IsResolved ? new[] { (NespResolvedExpression)iexpr } : ((NespAbstractExpression)iexpr).ResolveMetadata(this))
                .ToArray());

            // Always constract list (or apply function).
            // TODO: Require collect ability for mismatched expression (For use intellisense)
            var filteredCandidatesScored = transposedResolvedExpressionLists
                .Select(resolvedExpressionList => ConstructExpressionFromList(resolvedExpressionList, false, untypedExpression))
                .Where(scored => scored != null)
                .OrderByDescending(scored => scored.Score)
                .ToArray();

            // TODO: If results are empty --> invalid exprs.

            // If all expressions exactly resolved (not contains generic types), first expression is best result.
            if (filteredCandidatesScored.All(iexpr => iexpr.Type != null))
            {
                return new[] { filteredCandidatesScored[0] };
            }
            else
            {
                return filteredCandidatesScored;
            }
        }
        #endregion

        #region Id
        /// <summary>
        /// Construct expression from field information.
        /// </summary>
        /// <param name="field">Field information.</param>
        /// <param name="untypedExpression">Target untyped expression reference.</param>
        /// <returns>Expression (resolved)</returns>
        /// <remarks>This method construct expression from field.
        /// If field gives concrete value (Literal or marked initonly),
        /// get real value at this point and construct constant expression.</remarks>
        private static NespResolvedExpression ConstructExpressionFromField(
            FieldInfo field, NespIdExpression untypedExpression)
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

        internal NespResolvedExpression[] ResolveById(string id, NespIdExpression untypedExpression)
        {
            // TODO: Reexamination priority of members and symbols.

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
                    .Select(property => (NespResolvedExpression)new NespPropertyExpression(property, untypedExpression.Source))
                    .ToArray();
            }

            // TODO: Events

            var methodInfos = methods[id];
            if (methodInfos.Length >= 1)
            {
                return methodInfos
                    .Select(method => (NespResolvedExpression)new NespApplyFunctionExpression(
                        method,
                        emptyExpressions,
                        untypedExpression.Source))
                    .ToArray();
            }

            var symbolExpression = new NespReferenceSymbolExpression(id, untypedExpression.Source);
            var sexprs = symbols[id];
            if (sexprs.Length >= 1)
            {
                // Set related (original) expression.
                // This works will effect at lambda expression.
                symbolExpression.SetRelated(sexprs[0]);
            }
            else
            {
                // New symbol created.
                // It's maybe parameter expression.
                symbols.AddCandidate(id, symbolExpression);
            }

            return new NespResolvedExpression[] { symbolExpression };
        }
        #endregion
    }
}
