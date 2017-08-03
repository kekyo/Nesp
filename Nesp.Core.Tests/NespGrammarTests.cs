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
using Antlr4.Runtime;
using NUnit.Framework;

using Nesp.Internals;

namespace Nesp
{
    [TestFixture]
    public class NespGrammarTests
    {
        private static string Parse(string text, Func<NespGrammarParser, ParserRuleContext> parseToContext)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new NespGrammarLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var grammarParser = new NespGrammarParser(commonTokenStream);
            var graphContext = parseToContext(grammarParser);
            return graphContext.ToStringTree(grammarParser.RuleNames);
        }

        #region Numeric
        [Test]
        public void IntegerNumericTest()
        {
            var actual = Parse("123456", p => p.numeric());
            Assert.AreEqual("(numeric 123456)", actual);
        }

        [Test]
        public void FloatingPointNumericTest()
        {
            var actual = Parse("123.456", p => p.numeric());
            Assert.AreEqual("(numeric 123.456)", actual);
        }

        [Test]
        public void HexadecimalNumericTest()
        {
            var actual = Parse("0x123456", p => p.numeric());
            Assert.AreEqual("(numeric 0x123456)", actual);
        }

        [Test]
        public void Int64AsStrictTest()
        {
            var actual = Parse("123456l", p => p.numeric());
            Assert.AreEqual("(numeric 123456l)", actual);
        }

        [Test]
        public void UInt64AsStrictTest()
        {
            var actual = Parse("123456u", p => p.numeric());
            Assert.AreEqual("(numeric 123456u)", actual);
        }

        [Test]
        public void Int64AsStrictHexadecimalTest()
        {
            var actual = Parse("0x123456l", p => p.numeric());
            Assert.AreEqual("(numeric 0x123456l)", actual);
        }

        [Test]
        public void UInt64AsStrictHexadecimalTest()
        {
            var actual = Parse("0x123456u", p => p.numeric());
            Assert.AreEqual("(numeric 0x123456u)", actual);
        }

        [Test]
        public void FloatAsStrictTest()
        {
            var actual = Parse("123.456f", p => p.numeric());
            Assert.AreEqual("(numeric 123.456f)", actual);
        }

        [Test]
        public void DoubleAsStrictTest()
        {
            var actual = Parse("123.456d", p => p.numeric());
            Assert.AreEqual("(numeric 123.456d)", actual);
        }

        [Test]
        public void DecimalAsStrictTest()
        {
            var actual = Parse("123.456m", p => p.numeric());
            Assert.AreEqual("(numeric 123.456m)", actual);
        }

        [Test]
        public void PlusNumericTest()
        {
            var actual = Parse("+123456", p => p.numeric());
            Assert.AreEqual("(numeric +123456)", actual);
        }

        [Test]
        public void MinusNumericTest()
        {
            var actual = Parse("-123456", p => p.numeric());
            Assert.AreEqual("(numeric -123456)", actual);
        }
        #endregion

        #region String
        [Test]
        public void StringTest()
        {
            var actual = Parse("\"abcdef\"", p => p.@string());
            Assert.AreEqual("(string \"abcdef\")", actual);
        }

        [Test]
        public void StringWithEscapedTest()
        {
            var actual = Parse("\"abc\\tdef\"", p => p.@string());
            Assert.AreEqual("(string \"abc\\tdef\")", actual);
        }

        [Test]
        public void StringWithEscapedQuoteTest()
        {
            var actual = Parse("\"abc\\\"def\"", p => p.@string());
            Assert.AreEqual("(string \"abc\\\"def\")", actual);
        }

        [Test]
        public void StringWithEscapeCharTest()
        {
            var actual = Parse("\"abc\\\\def\"", p => p.@string());
            Assert.AreEqual("(string \"abc\\\\def\")", actual);
        }
        #endregion

        #region Char
        [Test]
        public void CharTest()
        {
            var actual = Parse("'a'", p => p.@char());
            Assert.AreEqual("(char 'a')", actual);
        }

        [Test]
        public void CharWithEscapedTest()
        {
            var actual = Parse("'\\t'", p => p.@char());
            Assert.AreEqual("(char '\\t')", actual);
        }

        [Test]
        public void CharWithEscapedQuoteTest()
        {
            var actual = Parse("'\\''", p => p.@char());
            Assert.AreEqual("(char '\\'')", actual);
        }

        [Test]
        public void CharWithEscapeCharTest()
        {
            var actual = Parse("'\\\\'", p => p.@char());
            Assert.AreEqual("(char '\\\\')", actual);
        }
        #endregion

        #region Id
        [Test]
        public void SimpleIdTest()
        {
            var actual = Parse("abc", p => p.id());
            Assert.AreEqual("(id abc)", actual);
        }

        [Test]
        public void SimpleIdWithHeadUnderlineTest()
        {
            var actual = Parse("_abc", p => p.id());
            Assert.AreEqual("(id _abc)", actual);
        }

        [Test]
        public void SimpleIdWithCenterUnderlineTest()
        {
            var actual = Parse("ab_c", p => p.id());
            Assert.AreEqual("(id ab_c)", actual);
        }

        [Test]
        public void SimpleIdWithTailUnderlineTest()
        {
            var actual = Parse("abc_", p => p.id());
            Assert.AreEqual("(id abc_)", actual);
        }

        [Test]
        public void SimpleIdWithCenterNumberTest()
        {
            var actual = Parse("ab4c", p => p.id());
            Assert.AreEqual("(id ab4c)", actual);
        }

        [Test]
        public void SimpleIdWithTailNumberTest()
        {
            var actual = Parse("abc7", p => p.id());
            Assert.AreEqual("(id abc7)", actual);
        }

        [Test]
        public void ComplexIdTest()
        {
            var actual = Parse("abc.def.ghi", p => p.id());
            Assert.AreEqual("(id abc.def.ghi)", actual);
        }

        [Test]
        public void GenericIdTest()
        {
            var actual = Parse("abc.def.ghi<jkl>", p => p.id());
            Assert.AreEqual("(id abc.def.ghi<jkl>)", actual);
        }

        [Test]
        public void GenericComplexIdTest()
        {
            var actual = Parse("abc.def.ghi<jkl.mno>", p => p.id());
            Assert.AreEqual("(id abc.def.ghi<jkl.mno>)", actual);
        }

        [Test]
        public void GenericComplexNestedIdTest()
        {
            var actual = Parse("abc.def.ghi<jkl.mno<pqr>>", p => p.id());
            Assert.AreEqual("(id abc.def.ghi<jkl.mno<pqr>>)", actual);
        }
        #endregion

        #region WhiteSpace
        [Test]
        public void SpaceOneTest()
        {
            var actual = Parse("123456 abc.def", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def))", actual);
        }

        [Test]
        public void SpaceTwoTest()
        {
            var actual = Parse("123456  abc.def", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def))", actual);
        }

        [Test]
        public void TabOneTest()
        {
            var actual = Parse("123456\tabc.def", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def))", actual);
        }

        [Test]
        public void ReturnOneTest()
        {
            var actual = Parse("123456\rabc.def", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def))", actual);
        }

        [Test]
        public void NewlineOneTest()
        {
            var actual = Parse("123456\nabc.def", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def))", actual);
        }
        #endregion

        #region List
        [Test]
        public void ListOneTest()
        {
            var actual = Parse("123456", p => p.list());
            Assert.AreEqual("(list (numeric 123456))", actual);
        }

        [Test]
        public void ListTwoTest()
        {
            var actual = Parse("123456 abc.def", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def))", actual);
        }

        [Test]
        public void ListThreeTest()
        {
            var actual = Parse("123456 abc.def -123.456", p => p.list());
            Assert.AreEqual("(list (numeric 123456) (id abc.def) (numeric -123.456))", actual);
        }
        #endregion

        #region Expression
        [Test]
        public void ExpressionOneValueTest()
        {
            var actual = Parse("(123456)", p => p.expression());
            Assert.AreEqual("(expression ( (list (numeric 123456)) ))", actual);
        }

        [Test]
        public void ExpressionTwoValuesTest()
        {
            var actual = Parse("(123456 abc.def)", p => p.expression());
            Assert.AreEqual("(expression ( (list (numeric 123456) (id abc.def)) ))", actual);
        }

        [Test]
        public void ExpressionNestedValuesTest1()
        {
            var actual = Parse("(123456 (abc.def -123.456))", p => p.expression());
            Assert.AreEqual("(expression ( (list (numeric 123456) (expression ( (list (id abc.def) (numeric -123.456)) ))) ))", actual);
        }

        [Test]
        public void ExpressionNestedValuesTest2()
        {
            var actual = Parse("(123456 (abc.def +123.456))", p => p.expression());
            Assert.AreEqual("(expression ( (list (numeric 123456) (expression ( (list (id abc.def) (numeric +123.456)) ))) ))", actual);
        }
        #endregion
    }
}
