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
using System.Linq.Expressions;
using System.Text;
using Antlr4.Runtime.Misc;

namespace Nesp
{
    public sealed class NespVisitor : NespBaseVisitor<Expression>
    {
        //public override Expression VisitExpression([NotNull] NespParser.ExpressionContext context)
        //{
        //    return VisitChildren(context);
        //}

        //public override Expression VisitList([NotNull] NespParser.ListContext context)
        //{
        //    return VisitChildren(context);
        //}

        //public override Expression VisitToken([NotNull] NespParser.TokenContext context)
        //{
        //    return VisitChildren(context);
        //}

        public override Expression VisitString([NotNull] NespParser.StringContext context)
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

        public override Expression VisitNumeric([NotNull] NespParser.NumericContext context)
        {
            var text = context.children[0].GetText();
            if (byte.TryParse(text, out var byteValue))
            {
                return Expression.Constant(byteValue);
            }
            if (short.TryParse(text, out var shortValue))
            {
                return Expression.Constant(shortValue);
            }
            if (int.TryParse(text, out var intValue))
            {
                return Expression.Constant(intValue);
            }
            if (long.TryParse(text, out var longValue))
            {
                return Expression.Constant(longValue);
            }
            if (double.TryParse(text, out var doubleValue))
            {
                return Expression.Constant(doubleValue);
            }

            throw new OverflowException();
        }

        public override Expression VisitId([NotNull] NespParser.IdContext context)
        {
            return VisitChildren(context);
        }
    }
}
