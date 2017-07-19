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
            return graphContext.ToStringTree();
        }

        #region Id
        [Test]
        public void SimpleIdTest()
        {
            var actual = Parse("abc", p => p.id());
            Assert.AreEqual("([] abc)", actual);
        }

        [Test]
        public void SimpleIdWithHeadUnderlineTest()
        {
            var actual = Parse("_abc", p => p.id());
            Assert.AreEqual("([] _abc)", actual);
        }

        [Test]
        public void SimpleIdWithCenterUnderlineTest()
        {
            var actual = Parse("ab_c", p => p.id());
            Assert.AreEqual("([] ab_c)", actual);
        }

        [Test]
        public void SimpleIdWithTailUnderlineTest()
        {
            var actual = Parse("abc_", p => p.id());
            Assert.AreEqual("([] abc_)", actual);
        }

        [Test]
        public void SimpleIdWithCenterNumberTest()
        {
            var actual = Parse("ab4c", p => p.id());
            Assert.AreEqual("([] ab4c)", actual);
        }

        [Test]
        public void SimpleIdWithTailNumberTest()
        {
            var actual = Parse("abc7", p => p.id());
            Assert.AreEqual("([] abc7)", actual);
        }

        [Test]
        public void ComplexIdTest()
        {
            var actual = Parse("abc.def.ghi", p => p.id());
            Assert.AreEqual("([] abc.def.ghi)", actual);
        }
        #endregion

        #region Number
        [Test]
        public void IntegerNumberTest()
        {
            var actual = Parse("123456", p => p.numeric());
            Assert.AreEqual("([] 123456)", actual);
        }

        [Test]
        public void FloatingPointNumberTest()
        {
            var actual = Parse("123.456", p => p.numeric());
            Assert.AreEqual("([] 123.456)", actual);
        }
        #endregion
    }
}
