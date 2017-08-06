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
using System.Reflection;
using Antlr4.Runtime;
using NUnit.Framework;

using Nesp.Extensions;
using Nesp.Internals;
using Nesp.Expressions;

namespace Nesp
{
    [TestFixture]
    public class NespParserTests
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

        #region Numeric
        [Test]
        public void ByteConstantTest()
        {
            var expr = ParseAndVisit("123");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual((byte)123, numericExpr.Value);
        }

        [Test]
        public void Int16ConstantTest()
        {
            var expr = ParseAndVisit("12345");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual((short)12345, numericExpr.Value);
        }

        [Test]
        public void Int32ConstantTest()
        {
            var expr = ParseAndVisit("1234567");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(1234567, numericExpr.Value);
        }

        [Test]
        public void Int64ConstantTest()
        {
            var expr = ParseAndVisit("12345678901234");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(12345678901234L, numericExpr.Value);
        }

        [Test]
        public void DoubleConstantTest()
        {
            var expr = ParseAndVisit("123.45678901234567");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123.45678901234567, numericExpr.Value);
        }

        [Test]
        public void DecimalConstantTest()
        {
            var expr = ParseAndVisit("12345678901234567890123456789");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(12345678901234567890123456789m, numericExpr.Value);
        }

        [Test]
        public void ByteHexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x7c");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual((byte)0x7c, numericExpr.Value);
        }

        [Test]
        public void Int16HexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x1234");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual((short)0x1234, numericExpr.Value);
        }

        [Test]
        public void Int32HexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x1234567");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(0x1234567, numericExpr.Value);
        }

        [Test]
        public void Int64HexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x12345678901234");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(0x12345678901234L, numericExpr.Value);
        }

        [Test]
        public void Int64AsStrictConstantTest()
        {
            var expr = ParseAndVisit("123456L");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123456L, numericExpr.Value);
        }

        [Test]
        public void UInt32AsStrictConstantTest()
        {
            var expr = ParseAndVisit("123456U");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123456U, numericExpr.Value);
        }

        [Test]
        public void UInt64AsStrictConstantTest()
        {
            var expr = ParseAndVisit("123456UL");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123456UL, numericExpr.Value);
        }

        [Test]
        public void Int64AsStrictHexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x123456L");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(0x123456L, numericExpr.Value);
        }

        [Test]
        public void UInt32AsStrictHexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x123456U");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(0x123456U, numericExpr.Value);
        }

        [Test]
        public void UInt64AsStrictHexadecimalConstantTest()
        {
            var expr = ParseAndVisit("0x123456UL");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(0x123456UL, numericExpr.Value);
        }

        [Test]
        public void FloatAsStrictConstantTest()
        {
            var expr = ParseAndVisit("123.456f");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123.456f, numericExpr.Value);
        }

        [Test]
        public void DoubleAsStrictConstantTest()
        {
            var expr = ParseAndVisit("123.456d");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123.456d, numericExpr.Value);
        }

        [Test]
        public void DecimalAsStrictConstantTest()
        {
            var expr = ParseAndVisit("123.456m");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123.456m, numericExpr.Value);
        }

        [Test]
        public void PlusValueConstantTest()
        {
            var expr = ParseAndVisit("+123456");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(123456, numericExpr.Value);
        }

        [Test]
        public void MinusValueConstantTest()
        {
            var expr = ParseAndVisit("-123456");
            var numericExpr = (NespNumericExpression)expr;
            Assert.AreEqual(-123456, numericExpr.Value);
        }
        #endregion

        #region String
        [Test]
        public void StringConstantTest()
        {
            var expr = ParseAndVisit("\"abcdef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abcdef", stringExpr.Value);
        }

        [Test]
        public void EscapedCharStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\\"def\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\"def", stringExpr.Value);
        }

        [Test]
        public void Escaped0StringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\0def\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\0def", stringExpr.Value);
        }

        [Test]
        public void EscapedBStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\bdef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\bdef", stringExpr.Value);
        }

        [Test]
        public void EscapedFStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\fdef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\fdef", stringExpr.Value);
        }

        [Test]
        public void EscapedTStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\tdef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\tdef", stringExpr.Value);
        }

        [Test]
        public void EscapedRStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\rdef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\rdef", stringExpr.Value);
        }

        [Test]
        public void EscapedNStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\ndef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\ndef", stringExpr.Value);
        }

        [Test]
        public void EscapedVStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\vdef\"");
            var stringExpr = (NespStringExpression)expr;
            Assert.AreEqual("abc\vdef", stringExpr.Value);
        }
        #endregion

        #region Char
        [Test]
        public void CharConstantTest()
        {
            var expr = ParseAndVisit("'a'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('a', stringExpr.Value);
        }

        [Test]
        public void EscapedCharCharConstantTest()
        {
            var expr = ParseAndVisit("'\\\''");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\'', stringExpr.Value);
        }

        [Test]
        public void Escaped0CharConstantTest()
        {
            var expr = ParseAndVisit("'\\0'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\0', stringExpr.Value);
        }

        [Test]
        public void EscapedBCharConstantTest()
        {
            var expr = ParseAndVisit("'\\b'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\b', stringExpr.Value);
        }

        [Test]
        public void EscapedFCharConstantTest()
        {
            var expr = ParseAndVisit("'\\f'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\f', stringExpr.Value);
        }

        [Test]
        public void EscapedTCharConstantTest()
        {
            var expr = ParseAndVisit("'\\t'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\t', stringExpr.Value);
        }

        [Test]
        public void EscapedRCharConstantTest()
        {
            var expr = ParseAndVisit("'\\r'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\r', stringExpr.Value);
        }

        [Test]
        public void EscapedNCharConstantTest()
        {
            var expr = ParseAndVisit("'\\n'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\n', stringExpr.Value);
        }

        [Test]
        public void EscapedVCharConstantTest()
        {
            var expr = ParseAndVisit("'\\v'");
            var stringExpr = (NespCharExpression)expr;
            Assert.AreEqual('\v', stringExpr.Value);
        }
        #endregion

        #region Id
        [Test]
        public void SimpleIdTest()
        {
            var expr = ParseAndVisit("abc");
            var idExpr = (NespIdExpression)expr;
            Assert.AreEqual("abc", idExpr.Id);
        }

        [Test]
        public void DotNotatedIdTest()
        {
            var expr = ParseAndVisit("abc.def");
            var idExpr = (NespIdExpression)expr;
            Assert.AreEqual("abc.def", idExpr.Id);
        }

        [Test]
        public void BracketedIdTest()
        {
            var expr = ParseAndVisit("abc<def>");
            var idExpr = (NespIdExpression)expr;
            Assert.AreEqual("abc<def>", idExpr.Id);
        }

        [Test]
        public void TrueTest()
        {
            var expr = ParseAndVisit("true");
            var boolExpr = (NespBoolExpression)expr;
            Assert.AreEqual(true, boolExpr.Value);
        }

        [Test]
        public void FalseTest()
        {
            var expr = ParseAndVisit("false");
            var boolExpr = (NespBoolExpression)expr;
            Assert.AreEqual(false, boolExpr.Value);
        }
        #endregion

        #region List
        [Test]
        public void NoValuesListTest()
        {
            var expr = ParseAndVisit("");
            var unitExpr = (NespUnitExpression)expr;
            Assert.IsNotNull(unitExpr);
        }

        [Test]
        public void ListWithNumericValuesTest()
        {
            var expr = ParseAndVisit("123 456.789 12345ul \"abc\"");
            var listExpr = (NespListExpression)expr;
            Assert.IsTrue(
                new object[] { (byte)123, 456.789, 12345UL, "abc" }
                    .SequenceEqual(listExpr.List.Select(iexpr =>
                        ((NespTokenExpression)iexpr).Value)));
        }
        #endregion

        #region Expression
        [Test]
        public void NoValuesExpressionTest()
        {
            var expr = ParseAndVisit("()");
            var unitExpr = (NespUnitExpression)expr;
            Assert.IsNotNull(unitExpr);
        }

        [Test]
        public void ExpressionWithValuesTest()
        {
            var expr = ParseAndVisit("(123 456.789 12345ul \"abc\")");
            var listExpr = (NespListExpression)expr;
            Assert.IsTrue(
                new object[] { (byte)123, 456.789, 12345UL, "abc" }
                    .SequenceEqual(listExpr.List.Select(iexpr =>
                        ((NespTokenExpression)iexpr).Value)));
        }
        #endregion
    }
}
