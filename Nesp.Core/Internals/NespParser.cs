﻿/////////////////////////////////////////////////////////////////////////////////////////////////
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
using System.Linq.Expressions;
using System.Reflection;

using Nesp.Extensions;

namespace Nesp.Internals
{
    internal sealed class NespParser : NespGrammarBaseVisitor<Expression>
    {
        private readonly INespMemberBinder binder;
        private readonly Stack<CandidateInfo> candidateInfos = new Stack<CandidateInfo>();

        public NespParser(INespMemberBinder binder)
        {
            this.binder = binder;
            this.candidateInfos.Push(new CandidateInfo());
        }

        public void AddMembers(IMemberProducer members)
        {
            var current = candidateInfos.Peek();

            foreach (var entry in members.TypesByName)
            {
                current.Types.AddCandidates(
                    entry.Key,
                    entry.Value.Select(Expression.Constant).ToArray());
            }

            foreach (var entry in members.FieldsByName)
            {
                current.Fields.AddCandidates(
                    entry.Key,
                    entry.Value.Select(fi =>
                        (fi.IsLiteral || fi.IsInitOnly)
                            ? (Expression)Expression.Constant(fi.GetValue(null))
                            : (Expression)Expression.Field(null, fi)).ToArray());
            }

            foreach (var entry in members.PropertiesByName)
            {
                current.Properties.AddCandidates(
                    entry.Key,
                    entry.Value.Select(pi => Expression.Property(null, pi)).ToArray());
            }

            foreach (var entry in members.MethodsByName)
            {
                current.Methods.AddCandidates(
                    entry.Key,
                    entry.Value.ToArray());
            }
        }

        public override Expression VisitExpression(NespGrammarParser.ExpressionContext context)
        {
            var listContext = (NespGrammarParser.ListContext)context.GetChildren()[1];
            return this.Visit(listContext);
        }

        private static Expression NormalizeType(Expression expr, Type targetType)
        {
            return (expr.Type != targetType) ? Expression.Convert(expr, targetType) : expr;
        }

        private MethodCallExpression SelectMethod(MethodInfo[] candidates, Expression[] argExprs)
        {
            if (candidates.Length >= 1)
            {
                var types = argExprs
                    .Select(argExpr => argExpr.Type)
                    .ToArray();

                // TODO: DefaultBinder.SelectMethod can't resolve variable arguments (params).
                var mi = binder.SelectMethod(candidates, types);
                if (mi != null)
                {
                    var argTypes = mi.GetParameters()
                        .Select(pi => pi.ParameterType)
                        .ToArray();
                    return Expression.Call(
                        null, mi, argExprs
                            .Select((argExpr, index) => NormalizeType(argExpr, argTypes[index])));
                }
            }

            return null;
        }

        public override Expression VisitList(NespGrammarParser.ListContext context)
        {
            // Empty.
            if (context.children == null)
            {
                return NespUtilities.UnitExpression;
            }

            // First child is id?
            var children = context.GetChildren();
            var childContext0 = children[0] as NespGrammarParser.IdContext;
            if (childContext0 != null)
            {
                // Lookup id from known dict.
                var current = candidateInfos.Peek();
                var id0 = childContext0.GetInnerText();

                var candidates = current.Methods[id0];
                if (candidates.Length >= 1)
                {
                    var argExprs = children
                        .Skip(1)
                        .Select(this.Visit)
                        .ToArray();
                    var expr = this.SelectMethod(candidates, argExprs);
                    if (expr != null)
                    {
                        return expr;
                    }
                }

                //////////////////////////////////////
                // TODO: HACK: Resolve let operator
                //   MEMO: Convert totally Expression-based infrastructure from MemberInfo.

                if (id0 == "let")
                {
                    // TODO: Static binding (Count == 3)
                    if (children.Count == 4)
                    {
                        var childContext1 = children[1] as NespGrammarParser.IdContext;
                        var childContext2 = children[2] as NespGrammarParser.ExpressionContext;
                        var childContext3 = children[3] as NespGrammarParser.ExpressionContext;
                        if ((childContext1 != null) && (childContext2 != null) && (childContext3 != null))
                        {
                            var name = childContext1.GetInnerText();
                            // name must not contain period
                            if (name.Contains("."))
                            {
                                throw new ArgumentException("Can't bind name contains period: " + name);
                            }

                            var argExprs = ((NespGrammarParser.ListContext)childContext2.GetChildren()[1])
                                .GetChildren()
                                .Select(arg =>
                                {
                                    var argContext = arg as NespGrammarParser.IdContext;
                                    if (argContext != null)
                                    {
                                        var argName = argContext.GetInnerText();
                                        // TODO: Apply generic types
                                        return Expression.Parameter(typeof(object), argName);
                                    }
                                    else
                                    {
                                        return null;
                                    }
                                })
                                .ToArray();

                            // args must contain argument ids.
                            if ((argExprs.Length == 0) || argExprs.Any(arg => arg == null))
                            {
                                throw new ArgumentException("Can't function arguments contains only id: " + name);
                            }

                            current = current.Clone();
                            candidateInfos.Push(current);

                            foreach (var argExpr in argExprs)
                            {
                                current.Locals.AddCandidate(argExpr.Name, argExpr);
                            }

                            var bodyContext = (NespGrammarParser.ListContext)childContext3.GetChildren()[1];
                            var bodyExpr = this.Visit(bodyContext);

                            // TODO: Support tailcall recursion
                            var lambdaExpr = Expression.Lambda(bodyExpr, false, argExprs);

                            candidateInfos.Pop();

                            return lambdaExpr;
                        }
                    }
                }
            }

            // Become literal?
            if (children.Count == 1)
            {
                return this.Visit(children[0]);
            }

            // TODO: Calculate minimum assignable type.
            return Expression.NewArrayInit(
                typeof(object), context.GetChildren().Select(childContext =>
                    NormalizeType(this.Visit(childContext), typeof(object))));
        }

        public override Expression VisitString(NespGrammarParser.StringContext context)
        {
            var text = context.GetInnerText();
            var unquoted = text.Substring(1, text.Length - 2);
            var unescaped = unquoted.InterpretEscapes();
            return Expression.Constant(unescaped);
        }

        public override Expression VisitNumeric(NespGrammarParser.NumericContext context)
        {
            var numericText = context.GetInnerText();

            if (byte.TryParse(numericText, out var byteValue))
            {
                return Expression.Constant(byteValue);
            }
            if (short.TryParse(numericText, out var shortValue))
            {
                return Expression.Constant(shortValue);
            }
            if (int.TryParse(numericText, out var intValue))
            {
                return Expression.Constant(intValue);
            }
            if (long.TryParse(numericText, out var longValue))
            {
                return Expression.Constant(longValue);
            }
            if (double.TryParse(numericText, out var doubleValue))
            {
                return Expression.Constant(doubleValue);
            }

            throw new OverflowException();
        }

        public override Expression VisitId(NespGrammarParser.IdContext context)
        {
            var current = candidateInfos.Peek();

            var id = context.GetInnerText();
            var localCandidate = current.Locals[id].FirstOrDefault();
            if (localCandidate != null)
            {
                return localCandidate;
            }

            var fieldCandidate = current.Fields[id].FirstOrDefault();
            if (fieldCandidate != null)
            {
                return fieldCandidate;
            }

            var propertiesCandidate = current.Properties[id].FirstOrDefault();
            if (propertiesCandidate != null)
            {
                return propertiesCandidate;
            }

            // TODO: indexer

            // We can use only no arguments function in this place.
            // ex: 'string.Format "ABC{0}DEF{1}GHI" 123 System.Guid.NewGuid'
            //     NewGuid function is no arguments so it's legal style and support below.
            var methodCandidates = current.Methods[id];
            var expr = this.SelectMethod(methodCandidates, new Expression[0]);
            if (expr != null)
            {
                return expr;
            }

            var typeCandidate = current.Types[id].FirstOrDefault();
            if (typeCandidate != null)
            {
                return typeCandidate;
            }

            throw new ArgumentException("Id not found: " + id);
        }
    }
}
