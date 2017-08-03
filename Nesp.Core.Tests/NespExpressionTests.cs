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
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Antlr4.Runtime;
using NUnit.Framework;

using Nesp.Extensions;
using Nesp.Internals;
using Nesp.Expressions;

namespace Nesp
{
    [TestFixture]
    public class NespExpressionTests
    {
        private sealed class MemberBinder : INespMemberBinder
        {
            public MethodInfo SelectMethod(MethodInfo[] candidates, Type[] types)
            {
                return Type.DefaultBinder.SelectMethod(
                    BindingFlags.Public | BindingFlags.Static, candidates, types, null) as MethodInfo;
            }
        }

        private NespExpression ParseAndVisit(string replLine)
        {
            var inputStream = new AntlrInputStream(replLine);
            var lexer = new NespGrammarLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var grammarParser = new NespGrammarParser(commonTokenStream);

            var parser = new NespParser(new MemberBinder());
            parser.AddMembers(NespBaseExtension.CreateMemberProducer());
            parser.AddMembers(NespStandardExtension.CreateMemberProducer());
            return parser.Visit(grammarParser.list());
        }

        //#region Numeric
        //[Test]
        //public void ByteConstantTest()
        //{
        //    var expr = ParseAndVisit("123");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual((byte)123, numericExpr.Value);
        //}

        //[Test]
        //public void Int16ConstantTest()
        //{
        //    var expr = ParseAndVisit("12345");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual((short)12345, numericExpr.Value);
        //}

        //[Test]
        //public void Int32ConstantTest()
        //{
        //    var expr = ParseAndVisit("1234567");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(1234567, numericExpr.Value);
        //}

        //[Test]
        //public void Int64ConstantTest()
        //{
        //    var expr = ParseAndVisit("12345678901234");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(12345678901234L, numericExpr.Value);
        //}

        //[Test]
        //public void DoubleConstantTest()
        //{
        //    var expr = ParseAndVisit("123.45678901234567");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123.45678901234567, numericExpr.Value);
        //}

        //[Test]
        //public void DecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("12345678901234567890123456789");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(12345678901234567890123456789m, numericExpr.Value);
        //}

        //[Test]
        //public void ByteHexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x7c");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual((byte)0x7c, numericExpr.Value);
        //}

        //[Test]
        //public void Int16HexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x1234");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual((short)0x1234, numericExpr.Value);
        //}

        //[Test]
        //public void Int32HexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x1234567");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(0x1234567, numericExpr.Value);
        //}

        //[Test]
        //public void Int64HexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x12345678901234");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(0x12345678901234L, numericExpr.Value);
        //}

        //[Test]
        //public void Int64AsStrictConstantTest()
        //{
        //    var expr = ParseAndVisit("123456L");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123456L, numericExpr.Value);
        //}

        //[Test]
        //public void UInt32AsStrictConstantTest()
        //{
        //    var expr = ParseAndVisit("123456U");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123456U, numericExpr.Value);
        //}

        //[Test]
        //public void UInt64AsStrictConstantTest()
        //{
        //    var expr = ParseAndVisit("123456UL");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123456UL, numericExpr.Value);
        //}

        //[Test]
        //public void Int64AsStrictHexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x123456L");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(0x123456L, numericExpr.Value);
        //}

        //[Test]
        //public void UInt32AsStrictHexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x123456U");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(0x123456U, numericExpr.Value);
        //}

        //[Test]
        //public void UInt64AsStrictHexadecimalConstantTest()
        //{
        //    var expr = ParseAndVisit("0x123456UL");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(0x123456UL, numericExpr.Value);
        //}

        //[Test]
        //public void FloatAsStrictConstantTest()
        //{
        //    var expr = ParseAndVisit("123.456f");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123.456f, numericExpr.Value);
        //}

        //[Test]
        //public void DoubleAsStrictConstantTest()
        //{
        //    var expr = ParseAndVisit("123.456d");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123.456d, numericExpr.Value);
        //}

        //[Test]
        //public void DecimalAsStrictConstantTest()
        //{
        //    var expr = ParseAndVisit("123.456m");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123.456m, numericExpr.Value);
        //}

        //[Test]
        //public void PlusValueConstantTest()
        //{
        //    var expr = ParseAndVisit("+123456");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(123456, numericExpr.Value);
        //}

        //[Test]
        //public void MinusValueConstantTest()
        //{
        //    var expr = ParseAndVisit("-123456");
        //    var numericExpr = (NespNumericExpression)expr;
        //    Assert.AreEqual(-123456, numericExpr.Value);
        //}
        //#endregion

        //#region String
        //[Test]
        //public void StringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abcdef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abcdef", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedCharStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\\"def\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\"def", stringExpr.Value);
        //}

        //[Test]
        //public void Escaped0StringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\0def\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\0def", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedBStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\bdef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\bdef", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedFStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\fdef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\fdef", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedTStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\tdef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\tdef", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedRStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\rdef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\rdef", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedNStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\ndef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\ndef", stringExpr.Value);
        //}

        //[Test]
        //public void EscapedVStringConstantTest()
        //{
        //    var expr = ParseAndVisit("\"abc\\vdef\"");
        //    var stringExpr = (NespStringExpression)expr;
        //    Assert.AreEqual("abc\vdef", stringExpr.Value);
        //}
        //#endregion

        //#region Char
        //[Test]
        //public void CharConstantTest()
        //{
        //    var expr = ParseAndVisit("'a'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('a', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedCharCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\\''");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\'', stringExpr.Value);
        //}

        //[Test]
        //public void Escaped0CharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\0'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\0', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedBCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\b'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\b', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedFCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\f'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\f', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedTCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\t'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\t', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedRCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\r'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\r', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedNCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\n'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\n', stringExpr.Value);
        //}

        //[Test]
        //public void EscapedVCharConstantTest()
        //{
        //    var expr = ParseAndVisit("'\\v'");
        //    var stringExpr = (NespCharExpression)expr;
        //    Assert.AreEqual('\v', stringExpr.Value);
        //}
        //#endregion

        //#region Id
        //[Test]
        //public void TrueTest()
        //{
        //    var expr = ParseAndVisit("true");
        //    var boolExpr = (NespBoolExpression)expr;
        //    Assert.AreEqual(true, boolExpr.Value);
        //}

        //[Test]
        //public void FalseTest()
        //{
        //    var expr = ParseAndVisit("false");
        //    var boolExpr = (NespBoolExpression)expr;
        //    Assert.AreEqual(false, boolExpr.Value);
        //}
        //#endregion

        //#region Id
        public sealed class FieldIdTestClass
        {
            public static readonly bool BoolInitOnly = true;
            public static readonly string StringInitOnly = "abc";
            public static readonly char CharInitOnly = 'a';
            public static readonly Guid GuidInitOnly = Guid.NewGuid();

            public static readonly byte ByteInitOnly = 123;
            public static readonly sbyte SByteInitOnly = 123;
            public static readonly short Int16InitOnly = 12345;
            public static readonly ushort UInt16InitOnly = 12345;
            public static readonly int Int32InitOnly = 12345678;
            public static readonly uint UInt32InitOnly = 12345678;
            public static readonly long Int64InitOnly = 12345678901234567;
            public static readonly ulong UInt64InitOnly = 12345678901234567;
            public static readonly float FloatInitOnly = 123.45f;
            public static readonly double DoubleInitOnly = 123.45d;
            public static readonly decimal DecimalInitOnly = 1234567890123456789012345678m;

            public const bool BoolLiteral = true;
            public const string StringLiteral = "def";
            public const char CharLiteral = 'b';

            public const byte ByteLiteral = 112;
            public const sbyte SByteLiteral = 112;
            public const short Int16Literal = 11234;
            public const ushort UInt16Literal = 11234;
            public const int Int32Literal = 112345678;
            public const uint UInt32Literal = 112345678;
            public const long Int64Literal = 112345678901234567;
            public const ulong UInt64Literal = 112345678901234567;
            public const float FloatLiteral = 1123.45f;
            public const double DoubleLiteral = 1123.45d;
            public const decimal DecimalLiteral = 11234567890123456789012345678m;

            public static bool BoolField = true;
            public static string StringField = "ghi";
            public static char CharField = 'c';
            public static Guid GuidField = Guid.NewGuid();

            public static byte ByteField = 122;
            public static sbyte SByteField = 122;
            public static short Int16Field = 12245;
            public static ushort UInt16Field = 12245;
            public static int Int32Field = 12245678;
            public static uint UInt32Field = 12245678;
            public static long Int64Field = 12245678901234567;
            public static ulong UInt64Field = 12245678901234567;
            public static float FloatField = 122.45f;
            public static double DoubleField = 122.45d;
            public static decimal DecimalField = 1224567890123456789012345678m;
        }

        [Test]
        public async Task InitOnlyBoolFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.BoolInitOnly");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var boolExpr = (NespBoolExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.BoolInitOnly, boolExpr.Value);
        }

        [Test]
        public async Task InitOnlyStringFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.StringInitOnly");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var stringExpr = (NespStringExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.StringInitOnly, stringExpr.Value);
        }

        [Test]
        public async Task InitOnlyCharFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.CharInitOnly");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var charExpr = (NespCharExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.CharInitOnly, charExpr.Value);
        }

        [Test]
        public async Task InitOnlyFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.GuidInitOnly");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var charExpr = (NespConstantExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.GuidInitOnly, charExpr.Value);
        }

        [Test]
        public async Task InitOnlyNumericFieldIdTest()
        {
            foreach (var entry in await Task.WhenAll(new Dictionary<string, object>
                {
                    {"ByteInitOnly", FieldIdTestClass.ByteInitOnly},
                    {"SByteInitOnly", FieldIdTestClass.SByteInitOnly},
                    {"Int16InitOnly", FieldIdTestClass.Int16InitOnly},
                    {"UInt16InitOnly", FieldIdTestClass.UInt16InitOnly},
                    {"Int32InitOnly", FieldIdTestClass.Int32InitOnly},
                    {"UInt32InitOnly", FieldIdTestClass.UInt32InitOnly},
                    {"Int64InitOnly", FieldIdTestClass.Int64InitOnly},
                    {"UInt64InitOnly", FieldIdTestClass.UInt64InitOnly},
                    {"FloatInitOnly", FieldIdTestClass.FloatInitOnly},
                    {"DoubleInitOnly", FieldIdTestClass.DoubleInitOnly},
                    {"DecimalInitOnly", FieldIdTestClass.DecimalInitOnly},
                }
                .Select(async entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.FieldIdTestClass.{entry.Key}");

                    var context = new NespExpressionResolverContext();
                    context.AddCandidate(typeof(FieldIdTestClass));
                    var typedExpr = await untypedExpr.ResolveAsync(context);

                    var numericExpr = (NespNumericExpression)typedExpr;
                    return new { entry.Value, Result = numericExpr.Value };
                })))
            {
                Assert.AreEqual(entry.Value, entry.Result);
            }
        }
        
        [Test]
        public async Task LiteralBoolFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.BoolLiteral");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var boolExpr = (NespBoolExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.BoolLiteral, boolExpr.Value);
        }

        [Test]
        public async Task LiteralStringFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.StringLiteral");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var stringExpr = (NespStringExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.StringLiteral, stringExpr.Value);
        }

        [Test]
        public async Task LiteralCharFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.CharLiteral");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var charExpr = (NespCharExpression)typedExpr;
            Assert.AreEqual(FieldIdTestClass.CharLiteral, charExpr.Value);
        }

        [Test]
        public async Task LiteralNumericFieldIdTest()
        {
            foreach (var entry in await Task.WhenAll(new Dictionary<string, object>
                {
                    {"ByteLiteral", FieldIdTestClass.ByteLiteral},
                    {"SByteLiteral", FieldIdTestClass.SByteLiteral},
                    {"Int16Literal", FieldIdTestClass.Int16Literal},
                    {"UInt16Literal", FieldIdTestClass.UInt16Literal},
                    {"Int32Literal", FieldIdTestClass.Int32Literal},
                    {"UInt32Literal", FieldIdTestClass.UInt32Literal},
                    {"Int64Literal", FieldIdTestClass.Int64Literal},
                    {"UInt64Literal", FieldIdTestClass.UInt64Literal},
                    {"FloatLiteral", FieldIdTestClass.FloatLiteral},
                    {"DoubleLiteral", FieldIdTestClass.DoubleLiteral},
                    {"DecimalLiteral", FieldIdTestClass.DecimalLiteral},
                }
                .Select(async entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.FieldIdTestClass.{entry.Key}");

                    var context = new NespExpressionResolverContext();
                    context.AddCandidate(typeof(FieldIdTestClass));
                    var typedExpr = await untypedExpr.ResolveAsync(context);

                    var numericExpr = (NespNumericExpression) typedExpr;
                    return new {entry.Value, Result = numericExpr.Value};
                })))
            {
                Assert.AreEqual(entry.Value, entry.Result);
            }
        }

        [Test]
        public async Task BoolFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.BoolField");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var fieldExpr = (NespFieldExpression)typedExpr;
            Assert.AreSame(typeof(FieldIdTestClass).GetField("BoolField"), fieldExpr.Field);
        }

        [Test]
        public async Task StringFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.StringField");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var fieldExpr = (NespFieldExpression)typedExpr;
            Assert.AreSame(typeof(FieldIdTestClass).GetField("StringField"), fieldExpr.Field);
        }

        [Test]
        public async Task CharFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.CharField");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var fieldExpr = (NespFieldExpression)typedExpr;
            Assert.AreSame(typeof(FieldIdTestClass).GetField("CharField"), fieldExpr.Field);
        }

        [Test]
        public async Task FieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestClass.GuidField");

            var context = new NespExpressionResolverContext();
            context.AddCandidate(typeof(FieldIdTestClass));
            var typedExpr = await untypedExpr.ResolveAsync(context);

            var fieldExpr = (NespFieldExpression)typedExpr;
            Assert.AreSame(typeof(FieldIdTestClass).GetField("GuidField"), fieldExpr.Field);
        }

        [Test]
        public async Task NumericFieldIdTest()
        {
            foreach (var entry in await Task.WhenAll(new Dictionary<string, object>
                {
                    {"ByteField", FieldIdTestClass.ByteField},
                    {"SByteField", FieldIdTestClass.SByteField},
                    {"Int16Field", FieldIdTestClass.Int16Field},
                    {"UInt16Field", FieldIdTestClass.UInt16Field},
                    {"Int32Field", FieldIdTestClass.Int32Field},
                    {"UInt32Field", FieldIdTestClass.UInt32Field},
                    {"Int64Field", FieldIdTestClass.Int64Field},
                    {"UInt64Field", FieldIdTestClass.UInt64Field},
                    {"FloatField", FieldIdTestClass.FloatField},
                    {"DoubleField", FieldIdTestClass.DoubleField},
                    {"DecimalField", FieldIdTestClass.DecimalField},
                }
                .Select(async entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.FieldIdTestClass.{entry.Key}");

                    var context = new NespExpressionResolverContext();
                    context.AddCandidate(typeof(FieldIdTestClass));
                    var typedExpr = await untypedExpr.ResolveAsync(context);

                    var fieldExpr = (NespFieldExpression)typedExpr;
                    return new { entry.Key, fieldExpr.Field };
                })))
            {
                Assert.AreSame(typeof(FieldIdTestClass).GetField(entry.Key), entry.Field);
            }
        }

        //[Test]
        //public void EnumIdTest()
        //{
        //    var expr = ParseAndVisit("System.DateTimeKind.Local");
        //    var constExpr = (ConstantExpression)expr;
        //    Assert.AreEqual(DateTimeKind.Local, constExpr.Value);
        //}

        //[Test]
        //public void PropertyIdTest()
        //{
        //    var expr = ParseAndVisit("System.IntPtr.Size");
        //    var memberExpr = (MemberExpression)expr;
        //    var pi = (PropertyInfo)memberExpr.Member;
        //    Assert.IsNull(memberExpr.Expression);
        //    Assert.AreEqual(typeof(IntPtr), pi.DeclaringType);
        //    Assert.AreEqual(typeof(int), pi.PropertyType);
        //    Assert.AreEqual("Size", pi.Name);
        //}

        //[Test]
        //public void MethodIdTest()
        //{
        //    var expr = ParseAndVisit("System.Guid.NewGuid");
        //    var methodCallExpr = (MethodCallExpression)expr;
        //    var mi = methodCallExpr.Method;
        //    Assert.IsNull(methodCallExpr.Object);
        //    Assert.AreEqual(typeof(Guid), mi.DeclaringType);
        //    Assert.AreEqual(typeof(Guid), mi.ReturnType);
        //    Assert.AreEqual("NewGuid", mi.Name);
        //}

        //[Test]
        //public void MethodWithArgsIdTest()
        //{
        //    var expr = ParseAndVisit("System.String.Format \"ABC{0}DEF\" 123");
        //    var methodCallExpr = (MethodCallExpression)expr;
        //    var mi = methodCallExpr.Method;
        //    Assert.IsNull(methodCallExpr.Object);
        //    Assert.AreEqual(typeof(string), mi.DeclaringType);
        //    Assert.AreEqual(typeof(string), mi.ReturnType);
        //    Assert.IsTrue(
        //        new[] { typeof(string), typeof(object) }
        //        .SequenceEqual(mi.GetParameters().Select(pi => pi.ParameterType)));
        //    Assert.IsTrue(
        //        new[] { typeof(string), typeof(object) }
        //            .SequenceEqual(methodCallExpr.Arguments.Select(arg => arg.Type)));
        //    Assert.IsTrue(
        //        new object[] { "ABC{0}DEF", (byte)123 }
        //        .SequenceEqual(new[] {
        //            ((ConstantExpression)methodCallExpr.Arguments[0]).Value,
        //            ((ConstantExpression)((UnaryExpression)methodCallExpr.Arguments[1]).Operand).Value }));
        //    Assert.AreEqual("Format", mi.Name);
        //}

        //[Test]
        //public void ReservedIdTest()
        //{
        //    var expr = ParseAndVisit("int.MinValue");
        //    var constExpr = (ConstantExpression)expr;
        //    Assert.AreEqual(int.MinValue, constExpr.Value);
        //}
        //#endregion

        //#region List
        //[Test]
        //public void NoValuesListTest()
        //{
        //    var expr = ParseAndVisit("");
        //    var unitExpr = (NespUnitExpression)expr;
        //    Assert.IsNotNull(unitExpr);
        //}

        //[Test]
        //public void ListWithNumericValuesTest()
        //{
        //    var expr = ParseAndVisit("123 456.789 12345ul \"abc\"");
        //    var listExpr = (NespListExpression)expr;
        //    Assert.IsTrue(
        //        new object[] { (byte)123, 456.789, 12345UL, "abc" }
        //            .SequenceEqual(listExpr.List.Select(iexpr =>
        //                ((NespTokenExpression)iexpr).Value)));
        //}
        //#endregion

        //#region Expression
        //[Test]
        //public void NoValuesExpressionTest()
        //{
        //    var expr = ParseAndVisit("()");
        //    var unitExpr = (NespUnitExpression)expr;
        //    Assert.IsNotNull(unitExpr);
        //}

        //[Test]
        //public void ExpressionWithValuesTest()
        //{
        //    var expr = ParseAndVisit("(123 456.789 12345ul \"abc\")");
        //    var listExpr = (NespListExpression)expr;
        //    Assert.IsTrue(
        //        new object[] { (byte)123, 456.789, 12345UL, "abc" }
        //            .SequenceEqual(listExpr.List.Select(iexpr =>
        //                ((NespTokenExpression)iexpr).Value)));
        //}
        //#endregion

        //#region Compilation
        //[Test]
        //public void CompileFieldIdTest()
        //{
        //    var expr = ParseAndVisit("System.DBNull.Value");
        //    var lambda = Expression.Lambda<Func<DBNull>>(expr, false, Enumerable.Empty<ParameterExpression>());
        //    var compiled = lambda.Compile();
        //    var value = compiled();
        //    Assert.AreEqual(DBNull.Value, value);
        //}

        //[Test]
        //public void CompileEnumIdTest()
        //{
        //    var expr = ParseAndVisit("System.DateTimeKind.Local");
        //    var lambda = Expression.Lambda<Func<DateTimeKind>>(expr, false, Enumerable.Empty<ParameterExpression>());
        //    var compiled = lambda.Compile();
        //    var value = compiled();
        //    Assert.AreEqual(DateTimeKind.Local, value);
        //}

        //[Test]
        //public void CompilePropertyIdTest()
        //{
        //    var expr = ParseAndVisit("System.IntPtr.Size");
        //    var lambda = Expression.Lambda<Func<int>>(expr, false, Enumerable.Empty<ParameterExpression>());
        //    var compiled = lambda.Compile();
        //    var size = compiled();
        //    Assert.AreEqual(IntPtr.Size, size);
        //}

        //[Test]
        //public void CompileMethodIdTest()
        //{
        //    var expr = ParseAndVisit("System.Guid.NewGuid");
        //    var lambda = Expression.Lambda<Func<Guid>>(expr, false, Enumerable.Empty<ParameterExpression>());
        //    var compiled = lambda.Compile();
        //    var value = compiled();
        //    Assert.IsFalse(value.Equals(Guid.Empty));
        //}
        //#endregion
    }
}
