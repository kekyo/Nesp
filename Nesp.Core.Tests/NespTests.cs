using System;
using Antlr4.Runtime;
using NUnit.Framework;

namespace Nesp.Core.Tests
{
    [TestFixture]
    public class NespTests
    {
        private string Parse(string text, Func<NespParser, ParserRuleContext> parseToContext)
        {
            var inputStream = new AntlrInputStream(text);
            var lexer = new NespLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var parser = new NespParser(commonTokenStream);
            var graphContext = parseToContext(parser);
            return graphContext.ToStringTree(parser.RuleNames);
        }

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
        #endregion

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

        #region Token
        [Test]
        public void NumericTokenTest()
        {
            var actual = Parse("123456", p => p.token());
            Assert.AreEqual("(token (numeric 123456))", actual);
        }

        [Test]
        public void IdTokenTest()
        {
            var actual = Parse("abc.def", p => p.token());
            Assert.AreEqual("(token (id abc.def))", actual);
        }
        #endregion

        #region WhiteSpace
        [Test]
        public void SpaceOneTest()
        {
            var actual = Parse("123456 abc.def", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace  ) (token (id abc.def)))", actual);
        }

        [Test]
        public void SpaceTwoTest()
        {
            var actual = Parse("123456  abc.def", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace   ) (token (id abc.def)))", actual);
        }

        [Test]
        public void TabOneTest()
        {
            var actual = Parse("123456\tabc.def", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace \\t) (token (id abc.def)))", actual);
        }

        [Test]
        public void ReturnOneTest()
        {
            var actual = Parse("123456\rabc.def", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace \\r) (token (id abc.def)))", actual);
        }

        [Test]
        public void NewlineOneTest()
        {
            var actual = Parse("123456\nabc.def", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace \\n) (token (id abc.def)))", actual);
        }
        #endregion

        #region List
        [Test]
        public void ListOneTest()
        {
            var actual = Parse("123456", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)))", actual);
        }

        [Test]
        public void ListTwoTest()
        {
            var actual = Parse("123456 abc.def", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace  ) (token (id abc.def)))", actual);
        }

        [Test]
        public void ListThreeTest()
        {
            var actual = Parse("123456 abc.def -123.456", p => p.list());
            Assert.AreEqual("(list (token (numeric 123456)) (whiteSpace  ) (token (id abc.def)) (whiteSpace  ) (token (numeric -123.456)))", actual);
        }
        #endregion

        #region Expression
        [Test]
        public void ExpressionOneValueTest()
        {
            var actual = Parse("(123456)", p => p.expression());
            Assert.AreEqual("(expression (bracketLeft () (list (token (numeric 123456))) (bracketRight )))", actual);
        }

        [Test]
        public void ExpressionTwoValuesTest()
        {
            var actual = Parse("(123456 abc.def)", p => p.expression());
            Assert.AreEqual("(expression (bracketLeft () (list (token (numeric 123456)) (whiteSpace  ) (token (id abc.def))) (bracketRight )))", actual);
        }

        [Test]
        public void ExpressionNestedValuesTest()
        {
            var actual = Parse("(123456 (abc.def -123.456))", p => p.expression());
            Assert.AreEqual("(expression (bracketLeft () (list (token (numeric 123456)) (whiteSpace  ) (token (expression (bracketLeft () (list (token (id abc.def)) (whiteSpace  ) (token (numeric -123.456))) (bracketRight ))))) (bracketRight )))", actual);
        }
        #endregion
    }
}
