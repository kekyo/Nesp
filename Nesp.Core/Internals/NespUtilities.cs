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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace Nesp.Internals
{
    internal static class NespUtilities
    {
        public static readonly IReadOnlyDictionary<string, string> OperatorMethodNames =
            new Dictionary<string, string>
            {
                // For C#
                { "op_Addition", "+" },
                { "op_Subtraction", "-" },
                { "op_Multiply", "*" },
                { "op_Division", "/" },
                { "op_Modulus", "%" },
                { "op_Equality", "==" },
                { "op_Inequality", "!=" },
                { "op_GreaterThan", ">" },
                { "op_GreaterThanOrEqual", ">=" },
                { "op_LessThan", "<" },
                { "op_LessThanOrEqual", "<=" },
                { "op_Increment", "++" },
                { "op_Decrement", "--" },
                { "op_BitwiseOr", "|" },
                { "op_BitwiseAnd", "&" },
                { "op_ExclusiveOr", "^" },
                { "op_OnesComplement", "~" },
                { "op_LogicalNot", "!" },
                { "op_LeftShift", "<<" },
                { "op_RightShift", ">>" },
                { "op_UnaryPlus", "+" },
                { "op_UnaryNegation", "-" },

                // For F#
                { "op_Nil", "[]" },
                { "op_Cons", "::" },
                { "op_Append", "@" },
                { "op_Concatenate", "^" },
                { "op_Dynamic", "?" },
                { "op_PipeLeft", "<|" },
                { "op_PipeRight", "|>" },
                { "op_Dereference", "!" },
                { "op_ComposeLeft", "<<" },
                { "op_ComposeRight", ">>" },
                { "op_Range", ".." },
            };

        public static readonly ConstantExpression UnitExpression =
            Expression.Constant(Unit.Value);

        private static readonly IList<IParseTree> empty =
            new IParseTree[0];

        public static string GetReadableTypeName(Type type, Func<Type, string> getTypeName)
        {
            // getTypeName is recursive call target (combinator)

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{getTypeName(elementType)}[{new string(Enumerable.Range(0, type.GetArrayRank() - 1).Select(index => ',').ToArray())}]";
            }

            // TODO: Generic type
            // TODO: Inner type
            // TODO: Delegate type (Format with arrow operator)
            return $"{type.Namespace}.{type.Name}";
        }

        private static string InternalFormatReadableString(object value)
        {
            if (value == null)
            {
                return "(null)";
            }
            if (value is string)
            {
                return "\"" + value + "\"";
            }

            var collection = value as ICollection;
            if (collection != null)
            {
                return string.Join(
                    " ",
                    collection
                    .Cast<object>()
                    .Select(InternalFormatReadableString));
            }
            else
            {
                return value.ToString();
            }
        }

        public static string FormatReadableString(object value, Func<Type, string> getTypeName)
        {
            if (value == null)
            {
                return "(null)";
            }

            var type = value.GetType();
            var typeName = getTypeName(type);

            return $"{InternalFormatReadableString(value)} : {typeName}";
        }

        public static IList<IParseTree> GetChildren(this ParserRuleContext context)
        {
            return context.children ?? empty;
        }

        public static string GetInnerText(this ParserRuleContext context)
        {
            return context.children[0]?.GetText();
        }

        public static string InterpretEscapes(this string escaped)
        {
            var sb = new StringBuilder();
            var index = 0;
            while (index < escaped.Length)
            {
                var ch = escaped[index];
                if (ch == '\\')
                {
                    index++;
                    ch = escaped[index];
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

            return sb.ToString();
        }
    }
}
