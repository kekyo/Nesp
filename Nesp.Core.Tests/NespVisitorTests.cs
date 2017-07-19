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
using System.Linq.Expressions;
using Antlr4.Runtime;
using NUnit.Framework;

namespace Nesp
{
    [TestFixture]
    public class NespVisitorTests
    {
        private Expression ParseAndVisit(string replLine)
        {
            var inputStream = new AntlrInputStream(replLine);
            var lexer = new NespLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new NespParser(commonTokenStream);

            var visitor = new NespVisitor();
            return visitor.Visit(parser.list());
        }

        #region Numeric
        [Test]
        public void ByteConstantTest()
        {
            var expr = ParseAndVisit("123");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual((byte)123, constExpr.Value);
        }

        [Test]
        public void Int16ConstantTest()
        {
            var expr = ParseAndVisit("12345");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual((short)12345, constExpr.Value);
        }

        [Test]
        public void Int32ConstantTest()
        {
            var expr = ParseAndVisit("1234567");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual(1234567, constExpr.Value);
        }

        [Test]
        public void Int64ConstantTest()
        {
            var expr = ParseAndVisit("12345678901234");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual(12345678901234L, constExpr.Value);
        }

        [Test]
        public void DoubleConstantTest()
        {
            var expr = ParseAndVisit("123.45678901234567");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual(123.45678901234567, constExpr.Value);
        }

        [Test]
        public void PlusValueConstantTest()
        {
            var expr = ParseAndVisit("+123456");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual(123456, constExpr.Value);
        }

        [Test]
        public void MinusValueConstantTest()
        {
            var expr = ParseAndVisit("-123456");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual(-123456, constExpr.Value);
        }
        #endregion

        #region String
        [Test]
        public void StringConstantTest()
        {
            var expr = ParseAndVisit("\"abcdef\"");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual("abcdef", constExpr.Value);
        }

        [Test]
        public void EscapedStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\tdef\"");
            var constExpr = (ConstantExpression)expr;
            Assert.AreEqual("abc\tdef", constExpr.Value);
        }
        #endregion
    }
}
