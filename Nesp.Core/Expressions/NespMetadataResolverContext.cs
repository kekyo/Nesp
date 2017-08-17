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
using Nesp.Metadatas;

namespace Nesp.Expressions
{
    public sealed class NespMetadataResolverContext
    {
        #region Static
        /// <summary>
        /// These entries are holding custom expression factories for basic type constants.
        /// </summary>
        private static readonly Dictionary<NespTypeInformation, Func<object, NespSourceInformation, NespResolvedExpression>> constantExpressionFactories =
            new Dictionary<NespTypeInformation, Func<object, NespSourceInformation, NespResolvedExpression>>
            {
                { NespMetadataContext.UnsafeFromType<bool>(), (value, source) => new NespBoolExpression((bool)value, source) },
                { NespMetadataContext.UnsafeFromType<string>(), (value, source) => new NespStringExpression((string)value, source) },
                { NespMetadataContext.UnsafeFromType<char>(), (value, source) => new NespCharExpression((char)value, source) },
                { NespMetadataContext.UnsafeFromType<byte>(), (value, source) => new NespNumericExpression<byte>((byte)value, source) },
                { NespMetadataContext.UnsafeFromType<sbyte>(), (value, source) => new NespNumericExpression<sbyte>((sbyte)value, source) },
                { NespMetadataContext.UnsafeFromType<short>(), (value, source) => new NespNumericExpression<short>((short)value, source) },
                { NespMetadataContext.UnsafeFromType<ushort>(), (value, source) => new NespNumericExpression<ushort>((ushort)value, source) },
                { NespMetadataContext.UnsafeFromType<int>(), (value, source) => new NespNumericExpression<int>((int)value, source) },
                { NespMetadataContext.UnsafeFromType<uint>(), (value, source) => new NespNumericExpression<uint>((uint)value, source) },
                { NespMetadataContext.UnsafeFromType<long>(), (value, source) => new NespNumericExpression<long>((long)value, source) },
                { NespMetadataContext.UnsafeFromType<ulong>(), (value, source) => new NespNumericExpression<ulong>((ulong)value, source) },
                { NespMetadataContext.UnsafeFromType<float>(), (value, source) => new NespNumericExpression<float>((float)value, source) },
                { NespMetadataContext.UnsafeFromType<double>(), (value, source) => new NespNumericExpression<double>((double)value, source) },
                { NespMetadataContext.UnsafeFromType<decimal>(), (value, source) => new NespNumericExpression<decimal>((decimal)value, source) },
            };

        private static readonly NespExpression[] emptyExpressions = new NespExpression[0];
        #endregion

        private readonly NespMetadataContext metadataContext = new NespMetadataContext();

        private readonly CandidatesDictionary<NespFieldInformation> fields = new CandidatesDictionary<NespFieldInformation>();
        private readonly CandidatesDictionary<NespPropertyInformation> properties = new CandidatesDictionary<NespPropertyInformation>();
        private readonly CandidatesDictionary<NespFunctionInformation> functions = new CandidatesDictionary<NespFunctionInformation>();
        private readonly CandidatesDictionary<NespSymbolExpression> symbols = new CandidatesDictionary<NespSymbolExpression>();

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

        public void AddCandidate(TypeInfo typeInfo)
        {
            // TODO: Move to metadata context.

            var typeName = NespUtilities.GetReadableTypeName(typeInfo);

            lock (fields)
            {
                foreach (var field in typeInfo.DeclaredFields
                    .Where(field => field.IsPublic && (!typeInfo.IsEnum || !field.IsSpecialName)))
                {
                    var key = $"{typeName}.{field.Name}";
                    fields.AddCandidate(key, new NespFieldInformation(field, metadataContext));
                }
            }

            var propertyMethods = new HashSet<MethodInfo>();
            lock (properties)
            {
                foreach (var property in typeInfo.DeclaredProperties
                    .Where(property => property.CanRead && property.GetMethod.IsPublic))
                {
                    var key = $"{typeName}.{property.Name}";
                    properties.AddCandidate(key, new NespPropertyInformation(property, metadataContext));

                    propertyMethods.Add(property.GetMethod);
                    propertyMethods.Add(property.SetMethod);
                }
            }

            lock (functions)
            {
                foreach (var method in typeInfo.DeclaredMethods
                    .Where(method => method.IsPublic && (propertyMethods.Contains(method) == false)))
                {
                    var key = $"{typeName}.{method.Name}";
                    functions.AddCandidate(key, new NespFunctionInformation(method, metadataContext));
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
                   && (this.functions.Equals(cachedContext.functions));
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
        /// <param name="function">Target method</param>
        /// <param name="argumentResolvedExpressions">Target expressions (require resolved, will be update)</param>
        /// <returns>Adaptable score (null: cannot use, 0 >= better for large amount value, int.MaxValue: exactly matched)</returns>
        private static ulong? CalculateAdaptableScoreByArguments(
            NespFunctionInformation function, NespResolvedExpression[] argumentResolvedExpressions)
        {
            Debug.Assert(argumentResolvedExpressions.Length <= 31);

            // Lack for arguments.
            var parameters = function.Parameters;
            if (parameters.Length > argumentResolvedExpressions.Length)
            {
                // Argument count mismatched.
                return null;
            }

            // Step 1: Do match between first and last (or first of variable)
            var totalExactlyCount = 0;
            var score = 0UL;
            for (var index = 0; index < parameters.Length; index++)
            {
                var parameterType = parameters[index].Type;
                var argumentResolvedExpression = argumentResolvedExpressions[index];
                var expressionType = argumentResolvedExpression.Type;
                if (expressionType == null)
                {
                    // Type inference by method parameter[index]
                    var referenceSymbolExpression = argumentResolvedExpression as NespSymbolExpression;
                    if (referenceSymbolExpression != null)
                    {
                        expressionType = parameterType;

                        // Clone instance and update list.
                        // Because type inference has to effect only this resolving.
                        referenceSymbolExpression = referenceSymbolExpression.Clone();
                        argumentResolvedExpressions[index] = argumentResolvedExpression;

                        // Inference.
                        //referenceSymbolExpression.InferenceByType(expressionType);

                        // Exactly match, but not fixed.
                        score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                        continue;
                    }
                    else
                    {
                        // Don't match for expression will be resolving.
                        continue;
                    }
                }

                if (object.ReferenceEquals(parameterType, expressionType))
                {
                    // Exactly match.
                    score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                    totalExactlyCount++;
                    continue;
                }

                // TODO: Generic type arguments
                if (parameterType.IsAssignableFrom(expressionType))
                {
                    // Argument can cast implicitly.
                    score += 2UL << ((argumentResolvedExpressions.Length - index) * 2);
                    continue;
                }

                if (function.ParamArrayElementType != null)
                {
                    var elementType = function.ParamArrayElementType;

                    // TODO: Support seq<T>
                    if (object.ReferenceEquals(elementType, expressionType))
                    {
                        // Exactly match in variable argument.
                        score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                        totalExactlyCount++;
                        continue;
                    }

                    // TODO: Generic type arguments
                    if (elementType.IsAssignableFrom(expressionType))
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
            if (function.ParamArrayElementType != null)
            {
                var elementType = function.ParamArrayElementType;

                // Variable first is already verified.
                for (var index = parameters.Length; index < argumentResolvedExpressions.Length; index++)
                {
                    var argumentResolvedExpression = argumentResolvedExpressions[index];
                    var expressionType = argumentResolvedExpression.Type;
                    if (expressionType == null)
                    {
                        // Type inference by method parameter[index]
                        var referenceSymbolExpression = argumentResolvedExpression as NespSymbolExpression;
                        if (referenceSymbolExpression != null)
                        {
                            expressionType = elementType;
                            //referenceSymbolExpression.InferenceByType(expressionType);

                            // Exactly match, but not fixed.
                            score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                            continue;
                        }
                        else
                        {
                            // Don't match for expression will be resolving.
                            continue;
                        }
                    }

                    if (object.ReferenceEquals(elementType, expressionType))
                    {
                        // Exactly match.
                        score += 3UL << ((argumentResolvedExpressions.Length - index) * 2);
                        totalExactlyCount++;
                        continue;
                    }

                    // TODO: Generic type arguments
                    if (elementType.IsAssignableFrom(expressionType))
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
            return (totalExactlyCount >= argumentResolvedExpressions.Length) ? ulong.MaxValue : score;
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
                var score = CalculateAdaptableScoreByArguments(applyFunction0.Function, argumentExpressions);
                if (score == null)
                {
                    // Can't match
                    return null;
                }

                // Construct function apply expression.
                var expr = new NespApplyFunctionExpression(
                    applyFunction0.Function, argumentExpressions, untypedExpression.Source);
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
            var defineExpr0 = listExpressions.FirstOrDefault() as NespSymbolExpression;
            if (defineExpr0?.Symbol == "define")
            {
                // TODO: Bind expression

                if (listExpressions.Length == 4)
                {
                    // Get symbol name, parameters and body.
                    var symbolNameExpression = listExpressions[1] as NespSymbolExpression;
                    var parametersExpression = listExpressions[2] as NespResolvedListExpression;
                    var bodyExpression = listExpressions[3];

                    if ((symbolNameExpression != null)
                        && (parametersExpression != null)
                        && (bodyExpression != null))
                    {
                        var preParameterExpressions = parametersExpression.List
                            .Select(iexpr => iexpr as NespSymbolExpression)
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
                var type = metadataContext.FromType(typeof(object[]).GetTypeInfo());
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

            // If first expression is exactly mathched
            if (filteredCandidatesScored.FirstOrDefault()?.Score == ulong.MaxValue)
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
                var type = metadataContext.FromType(typeof(object[]).GetTypeInfo());
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

            // If first expression is exactly mathched
            if (filteredCandidatesScored.FirstOrDefault()?.Score == ulong.MaxValue)
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
            NespFieldInformation field, NespIdExpression untypedExpression)
        {
            // Is field a constant value?
            if (field.IsConstant)
            {
                // Get constant value.
                var value = field.GetConstantValue();
                var type = field.FieldType;
                if (constantExpressionFactories.TryGetValue(field.FieldType, out var factory))
                {
                    return factory(value, untypedExpression.Source);
                }

                if (type.IsEnumType)
                {
                    return new NespEnumExpression(type, (Enum)value, untypedExpression.Source);
                }

                return new NespValueExpression(type, value, untypedExpression.Source);
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

            var methodInfos = functions[id];
            if (methodInfos.Length >= 1)
            {
                return methodInfos
                    .Select(method => (NespResolvedExpression)new NespApplyFunctionExpression(
                        method,
                        emptyExpressions,
                        untypedExpression.Source))
                    .ToArray();
            }

            // TODO: Handle type annotation
            var symbolExpression = new NespSymbolExpression(id, untypedExpression.Source);
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
