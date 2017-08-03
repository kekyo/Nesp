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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using Nesp.Expressions;
using Nesp.Extensions;

namespace Nesp.Internals
{
    internal sealed class NespParser : NespGrammarBaseVisitor<NespExpression>
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

        public override NespExpression VisitExpression(NespGrammarParser.ExpressionContext context)
        {
            var listContext = (NespGrammarParser.ListContext)context.GetChild(1);
            if (listContext == null)
            {
                // "()"
                return new NespUnitExpression(context.Start.Line, context.Start.Column);
            }

            return this.Visit(listContext);
        }

        public override NespExpression VisitList(NespGrammarParser.ListContext context)
        {
            switch (context.ChildCount)
            {
                case 0:
                    return new NespUnitExpression(context.Start.Line, context.Start.Column);
                case 1:
                    return this.Visit(context.GetChild(0));
                default:
                    return new NespListExpression(
                        context.GetChildren().Select(this.Visit).ToArray());
            }
        }

        public override NespExpression VisitString(NespGrammarParser.StringContext context)
        {
            var token = context.STRING().Symbol;
            var text = token.Text;

            var unquoted = text.Substring(1, text.Length - 2);
            var unescaped = unquoted.InterpretEscapes();

            return new NespStringExpression(unescaped, token.Line - 1, token.Column);
        }

        public override NespExpression VisitChar(NespGrammarParser.CharContext context)
        {
            var token = context.CHAR().Symbol;
            var text = token.Text;

            var unquoted = text.Substring(1, text.Length - 2);
            var unescaped = unquoted.InterpretEscapes();

            return new NespCharExpression(unescaped[0], token.Line - 1, token.Column);
        }

        private static NespNumericExpression ParseHexadecimalNumeric(
            string text, int line, int column)
        {
            if (text.StartsWith("0x") == false)
            {
                return null;
            }

            var numericText = text.Substring(2);
            if (numericText.EndsWith("ul"))
            {
                numericText = numericText.Substring(0, numericText.Length - 2);
                if (ulong.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var ulongValue))
                {
                    return new NespNumericExpression(ulongValue, line, column);
                }
            }
            else if (numericText.EndsWith("l"))
            {
                numericText = numericText.Substring(0, numericText.Length - 1);
                if (long.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new NespNumericExpression(longValue, line, column);
                }
            }
            else if (numericText.EndsWith("u"))
            {
                numericText = numericText.Substring(0, numericText.Length - 1);
                if (uint.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var uintValue))
                {
                    return new NespNumericExpression(uintValue, line, column);
                }
            }
            else
            {
                if (byte.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var byteValue))
                {
                    return new NespNumericExpression(byteValue, line, column);
                }
                if (short.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shortValue))
                {
                    return new NespNumericExpression(shortValue, line, column);
                }
                if (int.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var intValue))
                {
                    return new NespNumericExpression(intValue, line, column);
                }
                if (long.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new NespNumericExpression(longValue, line, column);
                }
            }

            return null;
        }

        private static NespNumericExpression ParseStrictNumeric(
            string text, int line, int column)
        {
            if (text.EndsWith("ul"))
            {
                var numericText = text.Substring(0, text.Length - 2);
                if (ulong.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var ulongValue))
                {
                    return new NespNumericExpression(ulongValue, line, column);
                }
            }
            else if (text.EndsWith("l"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (long.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new NespNumericExpression(longValue, line, column);
                }
            }
            else if (text.EndsWith("u"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (uint.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var uintValue))
                {
                    return new NespNumericExpression(uintValue, line, column);
                }
            }
            else if (text.EndsWith("f"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (float.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                {
                    return new NespNumericExpression(floatValue, line, column);
                }
            }
            else if (text.EndsWith("d"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (double.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    return new NespNumericExpression(doubleValue, line, column);
                }
            }
            else if (text.EndsWith("m"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (decimal.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                {
                    return new NespNumericExpression(decimalValue, line, column);
                }
            }

            return null;
        }

        private static NespNumericExpression ParseNumeric(
            string text, int line, int column)
        {
            if (byte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
            {
                return new NespNumericExpression(byteValue, line, column);
            }
            if (short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue))
            {
                return new NespNumericExpression(shortValue, line, column);
            }
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
            {
                return new NespNumericExpression(intValue, line, column);
            }
            if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
            {
                return new NespNumericExpression(longValue, line, column);
            }
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
            {
                return new NespNumericExpression(doubleValue, line, column);
            }
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return new NespNumericExpression(decimalValue, line, column);
            }

            return null;
        }

        public override NespExpression VisitNumeric(NespGrammarParser.NumericContext context)
        {
            var token = context.NUMERIC().Symbol;
            var text = token.Text.ToLowerInvariant();

            var line = token.Line - 1;
            var column = token.Column;

            var expr =
                ParseHexadecimalNumeric(text, line, column) ??
                ParseStrictNumeric(text, line, column) ??
                ParseNumeric(text, line, column);

            if (expr == null)
            {
                throw new FormatException();
            }

            return expr;
        }

        public override NespExpression VisitId(NespGrammarParser.IdContext context)
        {
            var token = context.ID().Symbol;
            var text = token.Text;

            return new NespIdExpression(text, token.Line - 1, token.Column);
        }
    }
}
