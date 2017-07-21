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
using System.Threading.Tasks;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using Nesp.Extensions;
using Nesp.Internals;

namespace Nesp
{
    public enum NespExpressionType
    {
        Repl,
        Strict
    }

    public sealed class NespEngine
    {
        private readonly NespParser parser;
        private readonly Dictionary<string, Func<object>> cachedFuncs =
            new Dictionary<string, Func<object>>();
        private readonly NespExpressionType type;

        public NespEngine(NespExpressionType type, INespMemberBinder binder)
        {
            this.type = type;
            this.parser = new NespParser(binder);
        }

        public static string GetReadableTypeName(Type type)
        {
            return NespStandardExtension.ReservedTypeNames.TryGetValue(type, out var typeName)
                ? typeName
                : NespUtilities.GetReadableTypeName(
                    type, GetReadableTypeName);
        }

        public static string FormatReadableString(object value)
        {
            return NespUtilities.FormatReadableString(
                value, GetReadableTypeName);
        }

        public async Task AddExtensionAsync(INespExtension extension)
        {
            var members =
                await extension.GetMemberProducerAsync()
                .ConfigureAwait(false);

            lock (parser)
            {
                parser.AddMembers(members);
            }
        }

        public Expression<Func<object>> ParseExpression(string expression)
        {
            var inputStream = new AntlrInputStream(expression);
            var lexer = new NespGrammarLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var grammarParser = new NespGrammarParser(commonTokenStream);

            var context = (type == NespExpressionType.Repl)
                ? (IParseTree)grammarParser.list()          // Take from list
                : (IParseTree)grammarParser.expression();   // Take from expression

            Expression expr;
            lock (parser)
            {
                expr = parser.Visit(context);
            }

            var valueType = typeof(object);
            var strictExpr = (expr.Type == valueType)
                ? expr
                : Expression.Convert(expr, valueType);

            return Expression.Lambda<Func<object>>(
                strictExpr, false, Enumerable.Empty<ParameterExpression>());
        }

        public async Task<Func<object>> CompileExpressionAsync(string expression)
        {
            lock (cachedFuncs)
            {
                if (cachedFuncs.TryGetValue(expression, out var cachedFunc) == true)
                {
                    return cachedFunc;
                }
            }

            var func = await Task.Run(() =>
                {
                    var expr = this.ParseExpression(expression);
                    return expr.Compile();
                });

            lock (cachedFuncs)
            {
                cachedFuncs.Add(expression, func);
            }

            return func;
        }
    }
}
