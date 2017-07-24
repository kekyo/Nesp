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
        private sealed class MethodBinder : INespMemberBinder
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

            var parser = new NespParser(new MethodBinder());
            parser.AddMembers(NespBaseExtension.CreateMemberProducer());
            parser.AddMembers(NespStandardExtension.CreateMemberProducer());
            return parser.Visit(grammarParser.list());
        }

        #region Numeric
        [Test]
        public void ByteConstantTest()
        {
            var expr = ParseAndVisit("123");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual((byte)123, constExpr.Value);
        }

        [Test]
        public void Int16ConstantTest()
        {
            var expr = ParseAndVisit("12345");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual((short)12345, constExpr.Value);
        }

        [Test]
        public void Int32ConstantTest()
        {
            var expr = ParseAndVisit("1234567");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(1234567, constExpr.Value);
        }

        [Test]
        public void Int64ConstantTest()
        {
            var expr = ParseAndVisit("12345678901234");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(12345678901234L, constExpr.Value);
        }

        [Test]
        public void DoubleConstantTest()
        {
            var expr = ParseAndVisit("123.45678901234567");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(123.45678901234567, constExpr.Value);
        }

        [Test]
        public void PlusValueConstantTest()
        {
            var expr = ParseAndVisit("+123456");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(123456, constExpr.Value);
        }

        [Test]
        public void MinusValueConstantTest()
        {
            var expr = ParseAndVisit("-123456");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(-123456, constExpr.Value);
        }
        #endregion

        #region String
        [Test]
        public void StringConstantTest()
        {
            var expr = ParseAndVisit("\"abcdef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abcdef", constExpr.Value);
        }

        [Test]
        public void EscapedCharStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\\"def\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\"def", constExpr.Value);
        }

        [Test]
        public void EscapedBStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\bdef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\bdef", constExpr.Value);
        }

        [Test]
        public void EscapedFStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\fdef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\fdef", constExpr.Value);
        }

        [Test]
        public void EscapedTStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\tdef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\tdef", constExpr.Value);
        }

        [Test]
        public void EscapedRStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\rdef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\rdef", constExpr.Value);
        }

        [Test]
        public void EscapedNStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\ndef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\ndef", constExpr.Value);
        }

        [Test]
        public void EscapedVStringConstantTest()
        {
            var expr = ParseAndVisit("\"abc\\vdef\"");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual("abc\vdef", constExpr.Value);
        }
        #endregion

        #region Id
        [Test]
        public void FieldIdTest()
        {
            var expr = ParseAndVisit("System.DBNull.Value");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(DBNull.Value, constExpr.Value);
        }

        [Test]
        public void EnumIdTest()
        {
            var expr = ParseAndVisit("System.DateTimeKind.Local");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(DateTimeKind.Local, constExpr.Value);
        }

        [Test]
        public void PropertyIdTest()
        {
            var expr = ParseAndVisit("System.IntPtr.Size");
            var propertyExpr = (NespPropertyExpression)expr;
            var pi = propertyExpr.Property;
            Assert.IsNull(propertyExpr.Instance);
            Assert.AreEqual(typeof(IntPtr), pi.DeclaringType);
            Assert.AreEqual(typeof(int), pi.PropertyType);
            Assert.AreEqual("Size", pi.Name);
        }

        [Test]
        public void FunctionIdTest()
        {
            var expr = ParseAndVisit("System.Guid.NewGuid");
            var FunctionCallExpr = (NespApplyFunctionExpression)expr;
            var mi = FunctionCallExpr.Function;
            Assert.IsNull(FunctionCallExpr.Instance);
            Assert.AreEqual(typeof(Guid), mi.DeclaringType);
            Assert.AreEqual(typeof(Guid), mi.ReturnType);
            Assert.AreEqual("NewGuid", mi.Name);
        }

        [Test]
        public void FunctionWithArgsIdTest()
        {
            var expr = ParseAndVisit("System.String.Format \"ABC{0}DEF\" 123");
            var FunctionCallExpr = (NespApplyFunctionExpression)expr;
            var mi = FunctionCallExpr.Function;
            Assert.IsNull(FunctionCallExpr.Instance);
            Assert.AreEqual(typeof(string), mi.DeclaringType);
            Assert.AreEqual(typeof(string), mi.ReturnType);
            Assert.IsTrue(
                new[] { typeof(string), typeof(object) }
                .SequenceEqual(mi.GetParameters().Select(pi => pi.ParameterType)));
            Assert.IsTrue(
                new[] { typeof(string), typeof(object) }
                    .SequenceEqual(FunctionCallExpr.Arguments.Select(arg => arg.CandidateType)));
            Assert.IsTrue(
                new object[] { "ABC{0}DEF", (byte)123 }
                .SequenceEqual(new[] {
                    ((NespConstantExpression)FunctionCallExpr.Arguments.ElementAt(0)).Value,
                    ((NespConstantExpression)((NespConvertExpression)FunctionCallExpr.Arguments.ElementAt(1)).Operand).Value }));
            Assert.AreEqual("Format", mi.Name);
        }

        [Test]
        public void ReservedIdTest()
        {
            var expr = ParseAndVisit("int.MinValue");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(int.MinValue, constExpr.Value);
        }
        #endregion

        #region List
        [Test]
        public void NoValuesListTest()
        {
            var expr = ParseAndVisit("");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(Unit.Value, constExpr.Value);
        }

        [Test]
        public void ListWithValuesTest()
        {
            var expr = ParseAndVisit("123 456 789");
            var newArrayExpr = (NewArrayExpression)expr;
            Assert.AreEqual(typeof(object[]), newArrayExpr.CandidateType);
            Assert.IsTrue(
                new object[] { (byte)123, (short)456, (short)789 }
                    .SequenceEqual(newArrayExpr.InitialValues.Select(iexpr => 
                        ((NespConstantExpression)((NespConvertExpression)iexpr).Operand).Value)));
        }
        #endregion

        #region Expression
        [Test]
        public void NoValuesExpressionTest()
        {
            var expr = ParseAndVisit("()");
            var constExpr = (NespConstantExpression)expr;
            Assert.AreEqual(Unit.Value, constExpr.Value);
        }

        [Test]
        public void ExpressionWithValuesTest()
        {
            var expr = ParseAndVisit("(123 456 789)");
            var newArrayExpr = (NewArrayExpression)expr;
            Assert.AreEqual(typeof(object[]), newArrayExpr.CandidateType);
            Assert.IsTrue(
                new object[] { (byte)123, (short)456, (short)789 }
                    .SequenceEqual(newArrayExpr.InitialValues.Select(iexpr =>
                        ((NespConstantExpression)((NespConvertExpression)iexpr).Operand).Value)));
        }
        #endregion

        #region Compilation
        [Test]
        public void CompileFieldIdTest()
        {
            var expr = ParseAndVisit("System.DBNull.Value");
            var lambda = NespExpression.Lambda<Func<DBNull>>(expr, "CompileFieldId", Enumerable.Empty<NespParameterExpression>());
            var compiled = lambda.Compile();
            var value = compiled();
            Assert.AreEqual("CompileFieldId", compiled.Method.Name);
            Assert.AreEqual(DBNull.Value, value);
        }

        [Test]
        public void CompileEnumIdTest()
        {
            var expr = ParseAndVisit("System.DateTimeKind.Local");
            var lambda = NespExpression.Lambda<Func<DateTimeKind>>(expr, "CompileEnumId", Enumerable.Empty<NespParameterExpression>());
            var compiled = lambda.Compile();
            var value = compiled();
            Assert.AreEqual("CompileEnumId", compiled.Method.Name);
            Assert.AreEqual(DateTimeKind.Local, value);
        }

        [Test]
        public void CompilePropertyIdTest()
        {
            var expr = ParseAndVisit("System.IntPtr.Size");
            var lambda = NespExpression.Lambda<Func<int>>(expr, "CompilePropertyId", Enumerable.Empty<NespParameterExpression>());
            var compiled = lambda.Compile();
            var size = compiled();
            Assert.AreEqual("CompilePropertyId", compiled.Method.Name);
            Assert.AreEqual(IntPtr.Size, size);
        }

        [Test]
        public void CompileFunctionIdTest()
        {
            var expr = ParseAndVisit("System.Guid.NewGuid");
            var lambda = NespExpression.Lambda<Func<Guid>>(expr, "CompileFunctionId", Enumerable.Empty<NespParameterExpression>());
            var compiled = lambda.Compile();
            var value = compiled();
            Assert.AreEqual("CompileFunctionId", compiled.Method.Name);
            Assert.IsFalse(value.Equals(Guid.Empty));
        }
        #endregion
    }
}
