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
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Antlr4.Runtime.Misc;

namespace Nesp
{
    public sealed class NespParser : NespGrammarBaseVisitor<Expression>
    {
        private readonly INespMemberBinder binder;
        private ImmutableDictionary<string, MemberInfo[]> members;

        public NespParser(INespMemberBinder binder)
        {
            this.binder = binder;
        }

        public void AddMembers(IEnumerable<KeyValuePair<string, MemberInfo[]>> newMembers)
        {
            foreach (var entry in newMembers)
            {
                if (members.TryGetValue(entry.Key, out var mis))
                {
                    // Extension can override members (Insert before last members).
                    members.SetValue(entry.Key, entry.Value.Concat(mis).Distinct().ToArray());
                }
                else
                {
                    members.AddValue(entry.Key, entry.Value.Distinct().ToArray());
                }
            }
        }

        public override Expression VisitExpression([NotNull] NespGrammarParser.ExpressionContext context)
        {
            return VisitChildren(context);
        }

        private static Expression NormalizeType(Expression expr, Type targetType)
        {
            return (expr.Type != targetType) ? Expression.Convert(expr, targetType) : expr;
        }

        public override Expression VisitList([NotNull] NespGrammarParser.ListContext context)
        {
            var childContext0 = context.children[0] as NespGrammarParser.IdContext;
            if (childContext0 != null)
            {
                var id0 = childContext0.children[0].GetText();

                if (members.TryGetValue(id0, out var candidates))
                {
                    var candidatesForMethod = candidates
                        .OfType<MethodInfo>()
                        .ToArray();
                    if (candidatesForMethod.Length >= 1)
                    {
                        var argExprs = context.children
                            .Skip(1)
                            .Select(this.Visit)
                            .ToArray();

                        var types = argExprs
                            .Select(argExpr => argExpr.Type)
                            .ToArray();

                        var mi = binder.SelectMethod(candidatesForMethod, types);
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
                }
            }

            return VisitChildren(context);
        }

        public override Expression VisitString([NotNull] NespGrammarParser.StringContext context)
        {
            var text = context.children[0].GetText();
            text = text.Substring(1, text.Length - 2);

            var sb = new StringBuilder();
            var index = 0;
            while (index < text.Length)
            {
                var ch = text[index];
                if (ch == '\\')
                {
                    index++;
                    ch = text[index];
                    switch (ch)
                    {
                        case 'b':
                            sb.Append('\b');
                            break;
                        case 'f':
                            sb.Append('\f');
                            break;
                        case 't':
                            sb.Append('\t');
                            break;
                        case 'r':
                            sb.Append('\r');
                            break;
                        case 'n':
                            sb.Append('\n');
                            break;
                        case 'v':
                            sb.Append('\v');
                            break;
                        default:
                            sb.Append(ch);
                            break;
                    }
                }
                else
                {
                    sb.Append(ch);
                }
                index++;
            }

            return Expression.Constant(sb.ToString());
        }

        public override Expression VisitNumeric([NotNull] NespGrammarParser.NumericContext context)
        {
            var numericText = context.children[0].GetText();

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

        public override Expression VisitId([NotNull] NespGrammarParser.IdContext context)
        {
            var id = context.children[0].GetText();

            if (members.TryGetValue(id, out var candidates))
            {
                var fi = candidates[0] as FieldInfo;
                if (fi != null)
                {
                    if (fi.IsLiteral || fi.IsInitOnly)
                    {
                        var value = fi.GetValue(null);
                        return Expression.Constant(value);
                    }
                    else
                    {
                        return Expression.Field(null, fi);
                    }
                }

                // TODO: indexer
                var pi = candidates[0] as PropertyInfo;
                if (pi != null)
                {
                    return Expression.Property(null, pi);
                }
            }

            throw new ArgumentException("Id not found: " + id);
        }
    }
}
