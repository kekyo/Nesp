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
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using Antlr4.Runtime;

using Nesp.Expressions.Abstracts;
using Nesp.Expressions.Resolved;
using Nesp.Extensions;
using Nesp.Internals;

namespace Nesp.Expressions
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

        private static NespSourceInformation GetSourceInformation(ParserRuleContext context)
        {
            var start = context.Start;
            var stop = context.Stop ?? context.Start;

            return new NespSourceInformation(
                start.Line - 1,
                start.Column,
                stop.Line - 1,
                stop.Column + (stop.StopIndex - stop.StartIndex));
        }

        public override NespExpression VisitBody(NespGrammarParser.BodyContext context)
        {
            var childContext = context.GetChild(0);
            return this.Visit(childContext);
        }

        public override NespExpression VisitBracketed(NespGrammarParser.BracketedContext context)
        {
            var listContext = (NespGrammarParser.ListContext)context.GetChild(1);

            var source = GetSourceInformation(context);
            var list = listContext.GetChildren().Select(this.Visit).ToArray();

            return new NespBracketedListExpression(list, source);
        }

        public override NespExpression VisitList(NespGrammarParser.ListContext context)
        {
            var source = GetSourceInformation(context);
            var list = context.GetChildren().Select(this.Visit).ToArray();

            return new NespListExpression(list, source);
        }

        public override NespExpression VisitString(NespGrammarParser.StringContext context)
        {
            var symbol = context.STRING().Symbol;
            var text = symbol.Text;
            var source = GetSourceInformation(context);

            var unquoted = text.Substring(1, text.Length - 2);
            var unescaped = unquoted.InterpretEscapes();

            return new NespStringExpression(unescaped, source);
        }

        public override NespExpression VisitChar(NespGrammarParser.CharContext context)
        {
            var symbol = context.CHAR().Symbol;
            var text = symbol.Text;
            var source = GetSourceInformation(context);

            var unquoted = text.Substring(1, text.Length - 2);
            var unescaped = unquoted.InterpretEscapes();

            return new NespCharExpression(unescaped[0], source);
        }

        private static NespNumericExpression ParseHexadecimalNumeric(
            string text, NespSourceInformation token)
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
                    return new NespNumericExpression(ulongValue, token);
                }
            }
            else if (numericText.EndsWith("l"))
            {
                numericText = numericText.Substring(0, numericText.Length - 1);
                if (long.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new NespNumericExpression(longValue, token);
                }
            }
            else if (numericText.EndsWith("u"))
            {
                numericText = numericText.Substring(0, numericText.Length - 1);
                if (uint.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var uintValue))
                {
                    return new NespNumericExpression(uintValue, token);
                }
            }
            else
            {
                if (byte.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var byteValue))
                {
                    return new NespNumericExpression(byteValue, token);
                }
                if (short.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var shortValue))
                {
                    return new NespNumericExpression(shortValue, token);
                }
                if (int.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var intValue))
                {
                    return new NespNumericExpression(intValue, token);
                }
                if (long.TryParse(numericText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new NespNumericExpression(longValue, token);
                }
            }

            return null;
        }

        private static NespNumericExpression ParseStrictNumeric(
            string text, NespSourceInformation token)
        {
            if (text.EndsWith("ul"))
            {
                var numericText = text.Substring(0, text.Length - 2);
                if (ulong.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var ulongValue))
                {
                    return new NespNumericExpression(ulongValue, token);
                }
            }
            else if (text.EndsWith("l"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (long.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
                {
                    return new NespNumericExpression(longValue, token);
                }
            }
            else if (text.EndsWith("u"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (uint.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var uintValue))
                {
                    return new NespNumericExpression(uintValue, token);
                }
            }
            else if (text.EndsWith("f"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (float.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var floatValue))
                {
                    return new NespNumericExpression(floatValue, token);
                }
            }
            else if (text.EndsWith("d"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (double.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
                {
                    return new NespNumericExpression(doubleValue, token);
                }
            }
            else if (text.EndsWith("m"))
            {
                var numericText = text.Substring(0, text.Length - 1);
                if (decimal.TryParse(numericText, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
                {
                    return new NespNumericExpression(decimalValue, token);
                }
            }

            return null;
        }

        private static NespNumericExpression ParseNumeric(
            string text, NespSourceInformation token)
        {
            if (byte.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var byteValue))
            {
                return new NespNumericExpression(byteValue, token);
            }
            if (short.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var shortValue))
            {
                return new NespNumericExpression(shortValue, token);
            }
            if (int.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var intValue))
            {
                return new NespNumericExpression(intValue, token);
            }
            if (long.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var longValue))
            {
                return new NespNumericExpression(longValue, token);
            }
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleValue))
            {
                return new NespNumericExpression(doubleValue, token);
            }
            if (decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return new NespNumericExpression(decimalValue, token);
            }

            return null;
        }

        public override NespExpression VisitNumeric(NespGrammarParser.NumericContext context)
        {
            var symbol = context.NUMERIC().Symbol;
            var text = symbol.Text.ToLowerInvariant();
            var source = GetSourceInformation(context);

            var expr =
                ParseHexadecimalNumeric(text, source) ??
                ParseStrictNumeric(text, source) ??
                ParseNumeric(text, source);

            if (expr == null)
            {
                throw new FormatException();
            }

            return expr;
        }

        public override NespExpression VisitId(NespGrammarParser.IdContext context)
        {
            var symbol = context.ID().Symbol;
            var text = symbol.Text;
            var source = GetSourceInformation(context);

            if (bool.TryParse(text, out var boolValue))
            {
                return new NespBoolExpression(boolValue, source);
            }
            else
            {
                return new NespIdExpression(text, source);
            }
        }
    }
}
