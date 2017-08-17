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
using System.Diagnostics;
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
        public static readonly IReadOnlyDictionary<TypeInfo, string> ReservedTypeNames =
            new Dictionary<TypeInfo, string>
            {
                { typeof(object).GetTypeInfo(), "object" },
                { typeof(byte).GetTypeInfo(), "byte" },
                { typeof(sbyte).GetTypeInfo(), "sbyte" },
                { typeof(short).GetTypeInfo(), "short" },
                { typeof(ushort).GetTypeInfo(), "ushort" },
                { typeof(int).GetTypeInfo(), "int" },
                { typeof(uint).GetTypeInfo(), "uint" },
                { typeof(long).GetTypeInfo(), "long" },
                { typeof(ulong).GetTypeInfo(), "ulong" },
                { typeof(float).GetTypeInfo(), "float" },
                { typeof(double).GetTypeInfo(), "double" },
                { typeof(decimal).GetTypeInfo(), "decimal" },
                { typeof(bool).GetTypeInfo(), "bool" },
                { typeof(char).GetTypeInfo(), "char" },
                { typeof(string).GetTypeInfo(), "string" },
                { typeof(DateTime).GetTypeInfo(), "datetime" },
                { typeof(TimeSpan).GetTypeInfo(), "timespan" },
                { typeof(Guid).GetTypeInfo(), "guid" },
                { typeof(Math).GetTypeInfo(), "math" },
                { typeof(Enum).GetTypeInfo(), "enum" },
                { typeof(Type).GetTypeInfo(), "type" },
            };

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

        private static readonly TypeInfo voidType = typeof(void).GetTypeInfo();
        private static readonly TypeInfo unitType = typeof(Unit).GetTypeInfo();
        private static readonly TypeInfo delegateType = typeof(Delegate).GetTypeInfo();

        public static string GetReadableTypeName(TypeInfo typeInfo)
        {
            return GetReadableTypeName(typeInfo, GetReadableTypeName);
        }

        public static string GetReservedReadableTypeName(TypeInfo typeInfo)
        {
            if (ReservedTypeNames.TryGetValue(typeInfo, out var typeName))
            {
                return typeName;
            }

            return GetReadableTypeName(typeInfo, GetReservedReadableTypeName);
        }

        public static string GetReadableTypeName(TypeInfo typeInfo, Func<TypeInfo, string> getTypeName)
        {
            // getTypeName is recursive call target (combinator)

            // Void (Unit)
            if (typeInfo == voidType)
            {
                return getTypeName(unitType);
            }

            // Array
            if (typeInfo.IsArray)
            {
                var elementType = typeInfo.GetElementType().GetTypeInfo();
                return $"{getTypeName(elementType)}[{new string(Enumerable.Range(0, typeInfo.GetArrayRank() - 1).Select(index => ',').ToArray())}]";
            }

            // Generic parameter
            if (typeInfo.IsGenericParameter)
            {
                return typeInfo.Name;
            }

            // Nested type
            if (typeInfo.IsNested)
            {
                return $"{getTypeName(typeInfo.DeclaringType.GetTypeInfo())}.{typeInfo.Name}";
            }

            // Delegate (Func<>)
            if ((typeInfo.IsAbstract == false)
                && delegateType.IsAssignableFrom(typeInfo))
            {
                var invokeMethod = typeInfo.GetDeclaredMethod("Invoke");
                var parameters = invokeMethod.GetParameters();
                var parameterTypes = string.Join(" -> ", parameters.Select(parameter => getTypeName(parameter.ParameterType.GetTypeInfo())));
                parameterTypes = (parameterTypes.Length >= 1) ? parameterTypes : getTypeName(unitType);
                return $"{string.Join(" -> ", parameterTypes)} -> {getTypeName(invokeMethod.ReturnType.GetTypeInfo())}";
            }

            // TODO: Generic type
            // TODO: Inner type
            return $"{typeInfo.Namespace}.{typeInfo.Name}";
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
            if (value is char)
            {
                return "'" + value + "'";
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

        public static string FormatReadableString(object value)
        {
            return FormatReadableString(value, FormatReadableString);
        }

        public static string FormatReservedReadableString(object value)
        {
            return FormatReadableString(value, GetReservedReadableTypeName);
        }

        public static string FormatReadableString(object value, Func<TypeInfo, string> getTypeName)
        {
            if (value == null)
            {
                return "(null)";
            }

            var typeInfo = value.GetType().GetTypeInfo();
            var typeName = getTypeName(typeInfo);

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
                        case '0':
                            sb.Append('\0');
                            break;
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

        public static IEnumerable<T> Traverse<T>(this T first, Func<T, T> next)
            where T : class
        {
            var current = first;
            while (current != null)
            {
                yield return current;
                current = next(current);
            }
        }

        public static IEnumerable<V> UnbalancedZip<T, U, V>(
            this IEnumerable<T> enumerable0, IEnumerable<U> enumerable1, Func<T, U, V> predict)
        {
            using (var enumerator0 = enumerable0.GetEnumerator())
            {
                using (var enumerator1 = enumerable1.GetEnumerator())
                {
                loop:
                    var next0 = enumerator0.MoveNext();
                    var next1 = enumerator1.MoveNext();
                    if (next0 && next1)
                    {
                        yield return predict(enumerator0.Current, enumerator1.Current);
                        goto loop;
                    }
                    if (next0)
                    {
                        do
                        {
                            yield return predict(enumerator0.Current, default(U));
                        }
                        while (enumerator0.MoveNext());
                    }
                    else if (next1)
                    {
                        do
                        {
                            yield return predict(default(T), enumerator1.Current);
                        }
                        while (enumerator1.MoveNext());
                    }
                }
            }
        }

        private static readonly TypeInfo[] emptyTypeInfo = new TypeInfo[0];

        public static TypeInfo[] CalculateElementType(this TypeInfo typeInfo, TypeInfo genericDefinitionType)
        {
            Debug.Assert(genericDefinitionType.IsGenericTypeDefinition);

            if (genericDefinitionType.IsInterface)
            {
                return typeInfo.ImplementedInterfaces
                    .Select(type => type.GetTypeInfo())
                    .Where(ti => ti.IsGenericType &&
                        object.ReferenceEquals(ti.GetGenericTypeDefinition().GetTypeInfo(), genericDefinitionType))
                    .Select(ti => ti.GenericTypeParameters.Select(t => t.GetTypeInfo()).ToArray())
                    .FirstOrDefault()
                    ?? emptyTypeInfo;
            }
            else
            {
                return typeInfo.Traverse(ti => ti.BaseType?.GetTypeInfo())
                    .Where(ti => ti.IsGenericType &&
                        object.ReferenceEquals(ti.GetGenericTypeDefinition().GetTypeInfo(), genericDefinitionType))
                    .Select(ti => ti.GenericTypeParameters.Select(t => t.GetTypeInfo()).ToArray())
                    .FirstOrDefault()
                    ?? emptyTypeInfo;
            }
        }
    }
}
