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
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Antlr4.Runtime.Misc;

namespace Nesp
{
    public sealed class NespVisitor : NespBaseVisitor<Expression>
    {
        private static readonly ImmutableDictionary<string, MemberInfo[]> initialMembers;

        static NespVisitor()
        {
            var assemblies = new[] {typeof(object), typeof(Uri), typeof(Enumerable)}
                .Select(type => type.GetTypeInfo().Assembly);
            var members = assemblies
                .SelectMany(assembly => assembly.DefinedTypes)
                .Where(type => (type.IsValueType || type.IsClass) && type.IsPublic)
                .SelectMany(type => type.DeclaredMembers)
                .ToArray();
            var fields =
                from fi in members.OfType<FieldInfo>()
                where fi.IsPublic && fi.IsStatic    // Include enums
                select (MemberInfo)fi;
            var properties =
                (from pi in members.OfType<PropertyInfo>()
                 let getter = pi.GetMethod
                 where pi.CanRead && (getter != null) && getter.IsPublic && getter.IsStatic
                 select new {mi = getter, pi})
                .ToDictionary(entry => entry.mi, entry => (MemberInfo)entry.pi);
            var methods =
                from mi in members.OfType<MethodInfo>()
                where mi.IsPublic && mi.IsStatic && !properties.ContainsKey(mi)
                select (MemberInfo)mi;

            var dict =
                (from member in properties.Values.Concat(methods).Concat(fields)
                 let fullName = member.DeclaringType.FullName + "." + member.Name
                 group member by fullName)
                .ToDictionary(g => g.Key, g => g.ToArray());

            initialMembers = new ImmutableDictionary<string, MemberInfo[]>(dict);
        }

        private readonly ImmutableDictionary<string, MemberInfo[]> members = initialMembers;

        public NespVisitor()
        {
        }

        public override Expression VisitExpression([NotNull] NespParser.ExpressionContext context)
        {
            return VisitChildren(context);
        }

        public override Expression VisitList([NotNull] NespParser.ListContext context)
        {
            return VisitChildren(context);
        }

        public override Expression VisitToken([NotNull] NespParser.TokenContext context)
        {
            return VisitChildren(context);
        }

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

        public override Expression VisitId([NotNull] NespParser.IdContext context)
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

                var pi = candidates[0] as PropertyInfo;
                if (pi != null)
                {
                    return Expression.Property(null, pi);
                }

                var mi = candidates[0] as MethodInfo;
                if (mi != null)
                {
                    return Expression.Call(null, mi);
                }
            }

            throw new ArgumentException("Id not found: " + id);
        }
    }
}
