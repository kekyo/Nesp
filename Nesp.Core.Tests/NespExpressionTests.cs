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
using System.Linq;
using System.Reflection;
using Antlr4.Runtime;
using NUnit.Framework;

using Nesp.Extensions;
using Nesp.Internals;
using Nesp.Expressions;
using Nesp.Expressions.Resolved;

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

        private NespAbstractExpression ParseAndVisit(string replLine)
        {
            var inputStream = new AntlrInputStream(replLine);
            var lexer = new NespGrammarLexer(inputStream);
            var commonTokenStream = new CommonTokenStream(lexer);
            var grammarParser = new NespGrammarParser(commonTokenStream);

            var parser = new NespParser(new MemberBinder());
            parser.AddMembers(NespBaseExtension.CreateMemberProducer());
            parser.AddMembers(NespStandardExtension.CreateMemberProducer());
            return (NespAbstractExpression)parser.Visit(grammarParser.body());
        }

        #region Id (Field)
        public sealed class FieldIdTestType
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
        public void InitOnlyBoolFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.BoolInitOnly");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var boolExpr = (NespBoolExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.BoolInitOnly, boolExpr.Value);
        }

        [Test]
        public void InitOnlyStringFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.StringInitOnly");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var stringExpr = (NespStringExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.StringInitOnly, stringExpr.Value);
        }

        [Test]
        public void InitOnlyCharFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.CharInitOnly");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var charExpr = (NespCharExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.CharInitOnly, charExpr.Value);
        }

        [Test]
        public void InitOnlyFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.GuidInitOnly");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var charExpr = (NespConstantExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.GuidInitOnly, charExpr.Value);
        }

        [Test]
        public void InitOnlyNumericFieldIdTest()
        {
            foreach (var entry in new Dictionary<string, object>
                {
                    {"ByteInitOnly", FieldIdTestType.ByteInitOnly},
                    {"SByteInitOnly", FieldIdTestType.SByteInitOnly},
                    {"Int16InitOnly", FieldIdTestType.Int16InitOnly},
                    {"UInt16InitOnly", FieldIdTestType.UInt16InitOnly},
                    {"Int32InitOnly", FieldIdTestType.Int32InitOnly},
                    {"UInt32InitOnly", FieldIdTestType.UInt32InitOnly},
                    {"Int64InitOnly", FieldIdTestType.Int64InitOnly},
                    {"UInt64InitOnly", FieldIdTestType.UInt64InitOnly},
                    {"FloatInitOnly", FieldIdTestType.FloatInitOnly},
                    {"DoubleInitOnly", FieldIdTestType.DoubleInitOnly},
                    {"DecimalInitOnly", FieldIdTestType.DecimalInitOnly},
                }
                .Select(entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.FieldIdTestType.{entry.Key}");

                    var context = new NespMetadataResolverContext();
                    context.AddCandidate(typeof(FieldIdTestType));
                    var typedExprs = untypedExpr.ResolveMetadata(context);

                    var numericExpr = (NespNumericExpression)typedExprs.Single();
                    return new { entry.Value, Result = numericExpr.Value };
                }))
            {
                Assert.AreEqual(entry.Value, entry.Result);
            }
        }
        
        [Test]
        public void LiteralBoolFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.BoolLiteral");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var boolExpr = (NespBoolExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.BoolLiteral, boolExpr.Value);
        }

        [Test]
        public void LiteralStringFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.StringLiteral");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var stringExpr = (NespStringExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.StringLiteral, stringExpr.Value);
        }

        [Test]
        public void LiteralCharFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.CharLiteral");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var charExpr = (NespCharExpression)typedExprs.Single();
            Assert.AreEqual(FieldIdTestType.CharLiteral, charExpr.Value);
        }

        [Test]
        public void LiteralNumericFieldIdTest()
        {
            foreach (var entry in new Dictionary<string, object>
                {
                    {"ByteLiteral", FieldIdTestType.ByteLiteral},
                    {"SByteLiteral", FieldIdTestType.SByteLiteral},
                    {"Int16Literal", FieldIdTestType.Int16Literal},
                    {"UInt16Literal", FieldIdTestType.UInt16Literal},
                    {"Int32Literal", FieldIdTestType.Int32Literal},
                    {"UInt32Literal", FieldIdTestType.UInt32Literal},
                    {"Int64Literal", FieldIdTestType.Int64Literal},
                    {"UInt64Literal", FieldIdTestType.UInt64Literal},
                    {"FloatLiteral", FieldIdTestType.FloatLiteral},
                    {"DoubleLiteral", FieldIdTestType.DoubleLiteral},
                    {"DecimalLiteral", FieldIdTestType.DecimalLiteral},
                }
                .Select(entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.FieldIdTestType.{entry.Key}");

                    var context = new NespMetadataResolverContext();
                    context.AddCandidate(typeof(FieldIdTestType));
                    var typedExprs = untypedExpr.ResolveMetadata(context);

                    var numericExpr = (NespNumericExpression) typedExprs.Single();
                    return new {entry.Value, Result = numericExpr.Value};
                }))
            {
                Assert.AreEqual(entry.Value, entry.Result);
            }
        }

        [Test]
        public void BoolFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.BoolField");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var fieldExpr = (NespFieldExpression)typedExprs.Single();
            Assert.AreSame(typeof(FieldIdTestType).GetField("BoolField"), fieldExpr.Field);
        }

        [Test]
        public void StringFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.StringField");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var fieldExpr = (NespFieldExpression)typedExprs.Single();
            Assert.AreSame(typeof(FieldIdTestType).GetField("StringField"), fieldExpr.Field);
        }

        [Test]
        public void CharFieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.CharField");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var fieldExpr = (NespFieldExpression)typedExprs.Single();
            Assert.AreSame(typeof(FieldIdTestType).GetField("CharField"), fieldExpr.Field);
        }

        [Test]
        public void FieldIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.FieldIdTestType.GuidField");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(FieldIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var fieldExpr = (NespFieldExpression)typedExprs.Single();
            Assert.AreSame(typeof(FieldIdTestType).GetField("GuidField"), fieldExpr.Field);
        }

        [Test]
        public void NumericFieldIdTest()
        {
            foreach (var entry in new Dictionary<string, object>
                {
                    {"ByteField", FieldIdTestType.ByteField},
                    {"SByteField", FieldIdTestType.SByteField},
                    {"Int16Field", FieldIdTestType.Int16Field},
                    {"UInt16Field", FieldIdTestType.UInt16Field},
                    {"Int32Field", FieldIdTestType.Int32Field},
                    {"UInt32Field", FieldIdTestType.UInt32Field},
                    {"Int64Field", FieldIdTestType.Int64Field},
                    {"UInt64Field", FieldIdTestType.UInt64Field},
                    {"FloatField", FieldIdTestType.FloatField},
                    {"DoubleField", FieldIdTestType.DoubleField},
                    {"DecimalField", FieldIdTestType.DecimalField},
                }
                .Select(entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.FieldIdTestType.{entry.Key}");

                    var context = new NespMetadataResolverContext();
                    context.AddCandidate(typeof(FieldIdTestType));
                    var typedExprs = untypedExpr.ResolveMetadata(context);

                    var fieldExpr = (NespFieldExpression)typedExprs.Single();
                    return new { entry.Key, fieldExpr.Field };
                }))
            {
                Assert.AreSame(typeof(FieldIdTestType).GetField(entry.Key), entry.Field);
            }
        }
        #endregion

        #region Id (Property)
        public sealed class PropertyIdTestType
        {
            public static bool Bool => true;
            public static string String => "abc";
            public static char Char => 'a';
            public static Guid Guid => Guid.NewGuid();

            public static byte Byte => 123;
            public static sbyte SByte => 123;
            public static short Int16 => 12345;
            public static ushort UInt16 => 12345;
            public static int Int32 => 12345678;
            public static uint UInt32 => 12345678;
            public static long Int64 => 12345678901234567;
            public static ulong UInt64 => 12345678901234567;
            public static float Float => 123.45f;
            public static double Double => 123.45d;
            public static decimal Decimal => 1234567890123456789012345678m;
        }

        [Test]
        public void BoolPropertyIdTest()
        {
            foreach (var entry in new Dictionary<string, object>
                {
                    {"Bool", PropertyIdTestType.Bool},
                    {"String", PropertyIdTestType.String},
                    {"Char", PropertyIdTestType.Char},
                    {"Guid", PropertyIdTestType.Guid},
                    {"Byte", PropertyIdTestType.Byte},
                    {"SByte", PropertyIdTestType.SByte},
                    {"Int16", PropertyIdTestType.Int16},
                    {"UInt16", PropertyIdTestType.UInt16},
                    {"Int32", PropertyIdTestType.Int32},
                    {"UInt32", PropertyIdTestType.UInt32},
                    {"Int64", PropertyIdTestType.Int64},
                    {"UInt64", PropertyIdTestType.UInt64},
                    {"Float", PropertyIdTestType.Float},
                    {"Double", PropertyIdTestType.Double},
                    {"Decimal", PropertyIdTestType.Decimal},
                }
                .Select(entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.PropertyIdTestType.{entry.Key}");

                    var context = new NespMetadataResolverContext();
                    context.AddCandidate(typeof(PropertyIdTestType));
                    var typedExprs = untypedExpr.ResolveMetadata(context);

                    var propertyExpr = (NespPropertyExpression)typedExprs.Single();
                    return new { entry.Key, propertyExpr.Property };
                }))
            {
                Assert.AreSame(typeof(PropertyIdTestType).GetProperty(entry.Key), entry.Property);
            }
        }
        #endregion

        #region Id (Enum)
        public enum EnumIdTestType
        {
            AAA = 0,
            BBB = 1,
            CCC = 17,
            DDD = 25,
            EEE = 43
        }

        [Test]
        public void EnumIdTest()
        {
            foreach (var entry in new Dictionary<string, object>
                {
                    {"AAA", EnumIdTestType.AAA},
                    {"BBB", EnumIdTestType.BBB},
                    {"CCC", EnumIdTestType.CCC},
                    {"DDD", EnumIdTestType.DDD},
                    {"EEE", EnumIdTestType.EEE},
                }
                .Select(entry =>
                {
                    var untypedExpr = ParseAndVisit($"Nesp.NespExpressionTests.EnumIdTestType.{entry.Key}");

                    var context = new NespMetadataResolverContext();
                    context.AddCandidate(typeof(EnumIdTestType));
                    var typedExprs = untypedExpr.ResolveMetadata(context);

                    var enumExpr = (NespEnumExpression)typedExprs.Single();
                    return new { entry.Value, ExprValue = enumExpr.Value };
                }))
            {
                Assert.AreEqual(entry.Value, entry.ExprValue);
            }
        }
        #endregion

        #region Id (Function)
        public sealed class SimpleFunctionIdTestType
        {
            public static string GetString0()
            {
                return "ABC";
            }

            public static string GetString1(int value)
            {
                return value.ToString();
            }

            public static string GetString1()
            {
                return "ABC";
            }

            public static string GetString2(object value0)
            {
                return $"{value0}";
            }

            public static string GetString2(int value0)
            {
                return $"{value0}";
            }

            public static string GetString2(string value0)
            {
                return $"{value0}";
            }

            public static string GetString2()
            {
                return "ABC";
            }

            public static string GetString2(params int[] args)
            {
                return string.Join(",", args);
            }

            public static string GetString2(int arg0, params long[] args)
            {
                return arg0 + "," + string.Join(",", args);
            }
        }

        [Test]
        public void MethodArgument0IdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString0");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType).GetMethod("GetString0");
            Assert.AreEqual(expected, functionExpr.Method);
        }

        [Test]
        public void MethodArgument0OverloadedIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString1");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType)
                .GetMethods()
                .First(method => (method.Name == "GetString1") && (method.GetParameters().Length == 0));
            Assert.AreEqual(expected, functionExpr.Method);
        }

        [Test]
        public void MethodArgument0ParamsOverloadedIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString2");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType)
                .GetMethods()
                .First(method => (method.Name == "GetString2") && (method.GetParameters().Length == 0));
            Assert.AreEqual(expected, functionExpr.Method);
        }

        [Test]
        public void MethodArgument1Int32OverloadedCompletedIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString2 12345678");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType)
                .GetMethods()
                .First(method =>
                    (method.Name == "GetString2") &&
                    (method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new [] { typeof(int) })));
            Assert.AreEqual(expected, functionExpr.Method);
            var argExprs = functionExpr.Arguments
                .Select(iexpr => (NespNumericExpression)iexpr)
                .ToArray();
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Type).SequenceEqual(new [] { typeof(int) }));
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Value).SequenceEqual(new object[] { 12345678 }));
        }

        [Test]
        public void MethodArgument1StringOverloadedCompletedIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString2 \"abcdefg\"");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType)
                .GetMethods()
                .First(method =>
                    (method.Name == "GetString2") &&
                    (method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(string) })));
            Assert.AreEqual(expected, functionExpr.Method);
            var argExprs = functionExpr.Arguments
                .Select(iexpr => (NespStringExpression)iexpr)
                .ToArray();
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Type).SequenceEqual(new[] { typeof(string) }));
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Value).SequenceEqual(new [] { "abcdefg" }));
        }

        [Test]
        public void MethodParamArgumentsInt32OverloadedCompletedIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString2 12345678 23456789");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType)
                .GetMethods()
                .First(method =>
                    (method.Name == "GetString2") &&
                    (method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(int[]) })));
            Assert.AreEqual(expected, functionExpr.Method);
            var argExprs = functionExpr.Arguments
                .Select(iexpr => (NespNumericExpression)iexpr)
                .ToArray();
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Type).SequenceEqual(new[] { typeof(int), typeof(int) }));
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Value).SequenceEqual(new object[] { 12345678, 23456789 }));
        }

        [Test]
        public void MethodParamArgumentsInt32AndLongOverloadedCompletedIdTest()
        {
            var untypedExpr = ParseAndVisit("Nesp.NespExpressionTests.SimpleFunctionIdTestType.GetString2 12345678 2345678901234567");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(SimpleFunctionIdTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var functionExpr = (NespApplyFunctionExpression)typedExprs.Single();
            var expected = typeof(SimpleFunctionIdTestType)
                .GetMethods()
                .First(method =>
                    (method.Name == "GetString2") &&
                    (method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(int), typeof(long[]) })));
            Assert.AreEqual(expected, functionExpr.Method);
            var argExprs = functionExpr.Arguments
                .Select(iexpr => (NespNumericExpression)iexpr)
                .ToArray();
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Type).SequenceEqual(new[] { typeof(int), typeof(long) }));
            Assert.IsTrue(argExprs.Select(iexpr => iexpr.Value).SequenceEqual(new object[] { 12345678, 2345678901234567 }));
        }
        #endregion

        //[Test]
        //public void ReservedIdTest()
        //{
        //    var expr = ParseAndVisit("int.MinValue");
        //    var constExpr = (ConstantExpression)expr;
        //    Assert.AreEqual(int.MinValue, constExpr.Value);
        //}
        //#endregion

        #region List
        [Test]
        public void NoValuesListTest()
        {
            var untypedExpr = ParseAndVisit("");

            var context = new NespMetadataResolverContext();
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var unitExpr = (NespUnitExpression)typedExprs.Single();
            Assert.IsNotNull(unitExpr);
        }

        [Test]
        public void ListWithNumericValuesTest()
        {
            var untypedExpr = ParseAndVisit("123 456.789 12345ul \"abc\"");

            var context = new NespMetadataResolverContext();
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var listExpr = (NespResolvedListExpression)typedExprs.Single();
            Assert.IsTrue(
                new object[] { (byte)123, 456.789, 12345UL, "abc" }
                    .SequenceEqual(listExpr.List.Select(iexpr =>
                        ((NespTokenExpression)iexpr).Value)));
        }

        [Test]
        public void ListWithExpressionTest()
        {
            var untypedExpr = ParseAndVisit("123 (456.789 'z') 12345ul \"abc\"");

            var context = new NespMetadataResolverContext();
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var listExpr = (NespResolvedListExpression)typedExprs.Single();
            Assert.AreEqual(4, listExpr.List.Length);
            Assert.AreEqual((byte)123, ((NespTokenExpression)listExpr.List[0]).Value);

            var nestedExpr = (NespResolvedListExpression)listExpr.List[1];
            Assert.AreEqual(2, nestedExpr.List.Length);
            Assert.AreEqual(456.789, ((NespTokenExpression)nestedExpr.List[0]).Value);
            Assert.AreEqual('z', ((NespTokenExpression)nestedExpr.List[1]).Value);

            Assert.AreEqual(12345UL, ((NespTokenExpression)listExpr.List[2]).Value);
            Assert.AreEqual("abc", ((NespTokenExpression)listExpr.List[3]).Value);
        }
        #endregion

        #region Expression
        [Test]
        public void NoValuesExpressionTest()
        {
            var untypedExpr = ParseAndVisit("()");

            var context = new NespMetadataResolverContext();
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var listExpr = (NespResolvedListExpression)typedExprs.Single();
            Assert.IsNotNull(listExpr);
            Assert.AreEqual(0, listExpr.List.Length);
        }

        [Test]
        public void ExpressionWithNumericValuesTest()
        {
            var untypedExpr = ParseAndVisit("(123 456.789 12345ul \"abc\")");

            var context = new NespMetadataResolverContext();
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var listExpr = (NespResolvedListExpression)typedExprs.Single();
            Assert.IsTrue(
                new object[] { (byte)123, 456.789, 12345UL, "abc" }
                    .SequenceEqual(listExpr.List.Select(iexpr =>
                        ((NespTokenExpression)iexpr).Value)));
        }

        [Test]
        public void ExpressionWithExpressionTest()
        {
            var untypedExpr = ParseAndVisit("(123 (456.789 'z') 12345ul \"abc\")");

            var context = new NespMetadataResolverContext();
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var listExpr = (NespResolvedListExpression)typedExprs.Single();
            Assert.AreEqual(4, listExpr.List.Length);
            Assert.AreEqual((byte)123, ((NespTokenExpression)listExpr.List[0]).Value);

            var nestedExpr = (NespResolvedListExpression)listExpr.List[1];
            Assert.AreEqual(2, nestedExpr.List.Length);
            Assert.AreEqual(456.789, ((NespTokenExpression)nestedExpr.List[0]).Value);
            Assert.AreEqual('z', ((NespTokenExpression)nestedExpr.List[1]).Value);

            Assert.AreEqual(12345UL, ((NespTokenExpression)listExpr.List[2]).Value);
            Assert.AreEqual("abc", ((NespTokenExpression)listExpr.List[3]).Value);
        }
        #endregion

        #region Lambda

        public static class LambdaTestType
        {
            public static string GetString0()
            {
                return "ABC";
            }

            public static int ParseInt32(string value)
            {
                return int.Parse(value);
            }
        }

        [Test]
        public void DefineEmptyArgumentListLambdaTest()
        {
            var untypedExpr = ParseAndVisit("define getString0 () Nesp.NespExpressionTests.LambdaTestType.GetString0");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(LambdaTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var lambdaExpr = (NespDefineLambdaExpression)typedExprs.Single();
            Assert.AreEqual("getString0", lambdaExpr.Name);
            Assert.AreEqual(typeof(string), lambdaExpr.Type);
            Assert.AreEqual(0, lambdaExpr.Parameters.Length);
            var bodyExpr = (NespApplyFunctionExpression)lambdaExpr.Body;
            Assert.AreEqual(typeof(LambdaTestType).GetMethod("GetString"), bodyExpr.Method);
        }

        [Test]
        public void DefineOneArgumentLambdaTest()
        {
            var untypedExpr = ParseAndVisit("define parseInt32 (value) (Nesp.NespExpressionTests.LambdaTestType.ParseInt32 value)");

            var context = new NespMetadataResolverContext();
            context.AddCandidate(typeof(LambdaTestType));
            var typedExprs = untypedExpr.ResolveMetadata(context);

            var lambdaExpr = (NespDefineLambdaExpression)typedExprs.Single();
            Assert.AreEqual("parseInt32", lambdaExpr.Name);
            Assert.AreEqual(typeof(int), lambdaExpr.Type);
            Assert.AreEqual(1, lambdaExpr.Parameters.Length);
            Assert.AreEqual("value", lambdaExpr.Parameters[0].Symbol);
            Assert.AreEqual(typeof(string), lambdaExpr.Parameters[0].Type);
            var bodyExpr = (NespApplyFunctionExpression)lambdaExpr.Body;
            Assert.AreEqual(typeof(LambdaTestType).GetMethod("ParseInt32"), bodyExpr.Method);
        }
        #endregion

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
