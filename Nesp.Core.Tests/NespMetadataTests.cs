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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Nesp.MD
{
    [TestFixture]
    public sealed class NespMetadataTests
    {
#if true
        #region Name
        [Test]
        public void GetNameFromStringTypeTest()
        {
            var context = new NespMetadataContext();
            var type = context.FromType(typeof(string).GetTypeInfo());

            Assert.AreEqual("System.String", type.FullName);
            Assert.AreEqual("string", type.Name);
        }

        [Test]
        public void GetNameFromInt32TypeTest()
        {
            var context = new NespMetadataContext();
            var type = context.FromType(typeof(int).GetTypeInfo());

            Assert.AreEqual("System.Int32", type.FullName);
            Assert.AreEqual("int", type.Name);
        }

        [Test]
        public void GetNameFromUriTypeTest()
        {
            var context = new NespMetadataContext();
            var type = context.FromType(typeof(Uri).GetTypeInfo());

            Assert.AreEqual("System.Uri", type.FullName);
            Assert.AreEqual("Uri", type.Name);
        }

        public static class InnerClassTestType
        {
        }

        [Test]
        public void GetNameFromInnerClassTypeTest()
        {
            var context = new NespMetadataContext();
            var type = context.FromType(typeof(InnerClassTestType).GetTypeInfo());

            Assert.AreEqual("Nesp.MD.NespMetadataTests.InnerClassTestType", type.FullName);
            Assert.AreEqual("InnerClassTestType", type.Name);
        }

        [Test]
        public void GetCombinedNameStringOrInt32TypeTest()
        {
            var context = new NespMetadataContext();
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var int32Type = context.FromType(typeof(int).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(stringType, int32Type);

            Assert.AreEqual("'T1", combinedType.FullName);
            Assert.AreEqual("T1", combinedType.Name);
        }

        [Test]
        public void GeneratePolymorphicTypeNameTest()
        {
            var context = new NespMetadataContext();
            var results = Enumerable.Range(0, 100)
                .Select(_ => context.GeneratePolymorphicTypeName())
                .ToArray();

            Assert.AreEqual("T1", results[0]);
            Assert.AreEqual("T100", results[99]);
        }
        #endregion

        #region CalculateCombined
        [Test]
        public void CalculateCombinedStringTypeAndStringType()
        {
            // BaseX ------+-- DerivedY
            //            vvvvvvvvv
            //             +-- DerivedY [Widen: BaseX]

            var context = new NespMetadataContext();
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(stringType, stringType);

            Assert.AreSame(stringType, combinedType);
        }

        [Test]
        public void CalculateCombinedStringTypeAndObjectType()
        {
            // BaseX ------+-- DerivedY
            //            vvvvvvvvv
            //             +-- DerivedY [Widen: BaseX]

            // DerivedY ------+-- BaseX
            //            vvvvvvvvv
            // DerivedY ------+         [Widen: BaseX]

            var context = new NespMetadataContext();
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var objectType = context.FromType(typeof(object).GetTypeInfo());

            var combinedType1 = context.CalculateCombinedType(stringType, objectType);
            var combinedType2 = context.CalculateCombinedType(objectType, stringType);

            Assert.AreSame(combinedType1, combinedType2);

            Assert.AreSame(stringType, combinedType1);
        }

        [Test]
        public void CalculateCombinedInt32TypeAndStringType()
        {
            // BaseX ------+-- int
            //         vvvvvvvvv
            //             +-- int
            //             +-- BaseX [Combined: BaseX]

            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var combinedType1 = context.CalculateCombinedType(int32Type, stringType);
            var combinedType2 = context.CalculateCombinedType(stringType, int32Type);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType1;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType2;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new [] { int32Type, stringType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedInt32TypeAndStringTypeAndUriType()
        {
            // BaseX ------+-- int
            //             +-- string
            //         vvvvvvvvv
            //             +-- int
            //             +-- string
            //             +-- BaseX  [Combined: BaseX]

            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var uriType = context.FromType(typeof(Uri).GetTypeInfo());

            var combinedType11 = context.CalculateCombinedType(int32Type, stringType);
            var combinedType12 = context.CalculateCombinedType(combinedType11, uriType);

            var combinedType21 = context.CalculateCombinedType(int32Type, stringType);
            var combinedType22 = context.CalculateCombinedType(uriType, combinedType21);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType12;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType22;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32Type, stringType, uriType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedInt32TypeAndMethodInfoTypeAndMethodBaseType()
        {
            // BaseX ------+-- int
            //             +-- DerivedY
            //         vvvvvvvvv
            //             +-- int
            //             +-- DerivedY [Widen: BaseX]

            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var methodInfoType = context.FromType(typeof(MethodInfo).GetTypeInfo());
            var methodBaseType = context.FromType(typeof(MethodBase).GetTypeInfo());

            var combinedType11 = context.CalculateCombinedType(int32Type, methodInfoType);
            var combinedType12 = context.CalculateCombinedType(combinedType11, methodBaseType);

            var combinedType21 = context.CalculateCombinedType(int32Type, methodInfoType);
            var combinedType22 = context.CalculateCombinedType(methodBaseType, combinedType21);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType12;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType22;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32Type, methodInfoType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedInt32TypeAndMethodBaseTypeAndMethodInfoType()
        {
            // DerivedY ------+-- int
            //                +-- BaseX
            //            vvvvvvvvv
            //                +-- int
            //                +-- DerivedY [Widen: BaseX]

            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var methodBaseType = context.FromType(typeof(MethodBase).GetTypeInfo());
            var methodInfoType = context.FromType(typeof(MethodInfo).GetTypeInfo());

            var combinedType11 = context.CalculateCombinedType(int32Type, methodBaseType);
            var combinedType12 = context.CalculateCombinedType(combinedType11, methodInfoType);

            var combinedType21 = context.CalculateCombinedType(int32Type, methodBaseType);
            var combinedType22 = context.CalculateCombinedType(methodInfoType, combinedType21);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType12;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType22;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32Type, methodInfoType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedInt32ArrayTypeAndMethodBaseType()
        {
            // int[]   --+---+-- BaseX
            // BaseX   --+   +-- int[]
            //           vvvvvvvvv
            // int[]   --+---+-- BaseX
            // BaseX   --+   +-- int[]

            var context = new NespMetadataContext();
            var methodBaseType = context.FromType(typeof(MethodBase).GetTypeInfo());
            var int32ArrayType = context.FromType(typeof(int[]).GetTypeInfo());

            var combinedType11 = context.CalculateCombinedType(int32ArrayType, methodBaseType);
            var combinedType12 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var combinedType13 = context.CalculateCombinedType(combinedType11, combinedType12);

            var combinedType21 = context.CalculateCombinedType(int32ArrayType, methodBaseType);
            var combinedType22 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var combinedType23 = context.CalculateCombinedType(combinedType22, combinedType21);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType13;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType23;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32ArrayType, methodBaseType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedEnumerableInt32TypeAndMethodBaseTypeVersusMethodInfoTypeAndInt32ArrayType()
        {
            // IE<int>   --+---+-- BaseX
            // DerivedY  --+   +-- int[]
            // string    --+
            //           vvvvvvvvv
            // int[]     --+                 [Widen: IE<int>]
            // DerivedY  --+                 [Widen: BaseX]
            // string    --+                 [Combined: string]

            // IE<int>   --+---+-- BaseX
            // DerivedY  --+   +-- int[]
            //                 +-- string
            //           vvvvvvvvv
            // int[]     --+                 [Widen: IE<int>]
            // DerivedY  --+                 [Widen: BaseX]
            // string    --+                 [Combined: string]

            var context = new NespMetadataContext();
            var enumerableInt32Type = context.FromType(typeof(IEnumerable<int>).GetTypeInfo());
            var methodBaseType = context.FromType(typeof(MethodBase).GetTypeInfo());
            var methodInfoType = context.FromType(typeof(MethodInfo).GetTypeInfo());
            var int32ArrayType = context.FromType(typeof(int[]).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var combinedType11 = context.CalculateCombinedType(enumerableInt32Type, methodInfoType);
            var combinedType12 = context.CalculateCombinedType(combinedType11, stringType);
            var combinedType13 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var combinedType14 = context.CalculateCombinedType(combinedType12, combinedType13);

            var combinedType21 = context.CalculateCombinedType(enumerableInt32Type, methodInfoType);
            var combinedType22 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var combinedType23 = context.CalculateCombinedType(stringType, combinedType22);
            var combinedType24 = context.CalculateCombinedType(combinedType23, combinedType21);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType14;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType24;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32ArrayType, methodInfoType, stringType }.OrderBy(t => t)));
        }
        #endregion

        #region IsAssignable
        [Test]
        public void IsAssignableFromInt32TypeToObjectType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var objectType = context.FromType(typeof(object).GetTypeInfo());

            Assert.IsTrue(context.IsAssignableType(objectType, int32Type));
        }

        [Test]
        public void IsAssignableFromObjectTypeToInt32Type()
        {
            var context = new NespMetadataContext();
            var objectType = context.FromType(typeof(object).GetTypeInfo());
            var int32Type = context.FromType(typeof(int).GetTypeInfo());

            Assert.IsFalse(context.IsAssignableType(int32Type, objectType));
        }

        [Test]
        public void TrueIsAssignableFromInt32TypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(int32Type, stringType);

            Assert.IsTrue(context.IsAssignableType(combinedType, int32Type));
        }

        [Test]
        public void TrueIsAssignableFromStringTypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(int32Type, stringType);

            Assert.IsTrue(context.IsAssignableType(combinedType, stringType));
        }

        [Test]
        public void TrueIsAssignableFromPolymorphicTypeToObjectType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var objectType = context.FromType(typeof(object).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(int32Type, stringType);

            Assert.IsTrue(context.IsAssignableType(objectType, combinedType));
        }

        [Test]
        public void FalseIsAssignableFromPolymorphicTypeToInt32Type()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(int32Type, stringType);

            // Not compatible string type'd polymorphic to int32 type.
            Assert.IsFalse(context.IsAssignableType(int32Type, combinedType));
        }

        [Test]
        public void FalseIsAssignableFromObjectTypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var uriType = context.FromType(typeof(Uri).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(stringType, uriType);

            Assert.IsFalse(context.IsAssignableType(combinedType, int32Type));
        }

        [Test]
        public void FalseIsAssignableFromInt32TypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var objectType = context.FromType(typeof(object).GetTypeInfo());

            var combinedType = context.CalculateCombinedType(int32Type, stringType);

            Assert.IsFalse(context.IsAssignableType(combinedType, objectType));
        }
        #endregion
#else
        #region RuntimeType
        [Test]
        public void FromRuntimeTypeTest()
        {
            var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());

            Assert.AreEqual("System.String", stringType.FullName);
            Assert.AreEqual("String", stringType.Name);
        }

        [Test]
        public void IsPrimitiveTypeFromRuntimeTypeTest()
        {
            var types = new[]
                {
                    typeof(bool),
                    typeof(byte), typeof(sbyte),
                    typeof(short), typeof(ushort),
                    typeof(int), typeof(uint),
                    typeof(long), typeof(ulong),
                    typeof(float), typeof(double),
                    typeof(char)
                }
                .Select(type => type.GetTypeInfo())
                .ToArray();

            foreach (var typeInfo in types)
            {
                var type = new NespRuntimeTypeInformation(typeInfo);
                Assert.IsTrue(type.IsPrimitiveType);
            }

            var dateTimeKindType = new NespRuntimeTypeInformation(typeof(DateTimeKind).GetTypeInfo());
            Assert.IsFalse(dateTimeKindType.IsBasicType);
        }

        [Test]
        public void IsBasicTypeFromRuntimeTypeTest()
        {
            var types = new[]
            {
                typeof(bool),
                typeof(byte), typeof(sbyte),
                typeof(short), typeof(ushort),
                typeof(int), typeof(uint),
                typeof(long), typeof(ulong),
                typeof(float), typeof(double), typeof(decimal),
                typeof(char), typeof(string)
            }
            .Select(type => type.GetTypeInfo())
            .ToArray();

            foreach (var typeInfo in types)
            {
                var type = new NespRuntimeTypeInformation(typeInfo);
                Assert.IsTrue(type.IsBasicType);
            }

            var dateTimeKindType = new NespRuntimeTypeInformation(typeof(DateTimeKind).GetTypeInfo());
            Assert.IsFalse(dateTimeKindType.IsBasicType);
        }

        [Test]
        public void IsEnumTypeFromRuntimeTypeTest()
        {
            var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());
            Assert.IsFalse(stringType.IsEnumType);

            var intType = new NespRuntimeTypeInformation(typeof(int).GetTypeInfo());
            Assert.IsFalse(intType.IsEnumType);

            var dateTimeKindType = new NespRuntimeTypeInformation(typeof(DateTimeKind).GetTypeInfo());
            Assert.IsTrue(dateTimeKindType.IsEnumType);
        }

        [Test]
        public void IsPolymorphicTypeFromRuntimeTypeTest()
        {
            var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());
            Assert.IsFalse(stringType.IsPolymorphicType);

            var intType = new NespRuntimeTypeInformation(typeof(int).GetTypeInfo());
            Assert.IsFalse(intType.IsPolymorphicType);

            var dateTimeKindType = new NespRuntimeTypeInformation(typeof(DateTimeKind).GetTypeInfo());
            Assert.IsFalse(dateTimeKindType.IsPolymorphicType);

            var listIntType = new NespRuntimeTypeInformation(typeof(List<int>).GetTypeInfo());
            Assert.IsTrue(listIntType.IsPolymorphicType);

            var enumerableIntType = new NespRuntimeTypeInformation(typeof(IEnumerable<int>).GetTypeInfo());
            Assert.IsTrue(enumerableIntType.IsPolymorphicType);
        }

        public sealed class PolymorphicArgumentsTestType<T>
            where T : class, IEnumerable<int>, new()
        {
        }

        [Test]
        public void PolymorphicArgumentsFromRuntimeTypeTest()
        {
            var context = new NespMetadataContext();

            var testType = new NespRuntimeTypeInformation(typeof(PolymorphicArgumentsTestType<>).GetTypeInfo());

            var polymorphicParameters = testType.GetPolymorphicParameters(context);
            Assert.AreEqual(1, polymorphicParameters.Length);

            var polymorphicParameter0 = polymorphicParameters[0];
            var constraints = polymorphicParameter0.GetPolymorphicTypeConstraints(context);
            Assert.AreEqual(1, constraints.Length);

            var constraint0 = constraints[0];

            Assert.IsFalse(constraint0.IsValueType);
            Assert.IsTrue(constraint0.IsReference);
            Assert.IsTrue(constraint0.IsDefaultConstractor);

            var constraintTypes = constraint0.ConstraintTypes;
            Assert.AreEqual(1, constraintTypes.Length);

            var enumerableIntType = new NespRuntimeTypeInformation(typeof(IEnumerable<int>).GetTypeInfo());
            Assert.AreEqual(enumerableIntType, constraintTypes[0]);
        }

        [Test]
        public void IsAssignableFromRuntimeTypeTest()
        {
            var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());

            var objectType = new NespRuntimeTypeInformation(typeof(object).GetTypeInfo());
            Assert.IsTrue(objectType.IsAssignableFrom(stringType));

            var intType = new NespRuntimeTypeInformation(typeof(int).GetTypeInfo());
            Assert.IsFalse(intType.IsAssignableFrom(stringType));

            var enumerableCharType = new NespRuntimeTypeInformation(typeof(IEnumerable<char>).GetTypeInfo());
            Assert.IsTrue(enumerableCharType.IsAssignableFrom(stringType));

            var enumerableIntType = new NespRuntimeTypeInformation(typeof(IEnumerable<int>).GetTypeInfo());
            Assert.IsFalse(enumerableIntType.IsAssignableFrom(stringType));
        }
        #endregion

        #region PolymorphicType
        [Test]
        public void PolymorphicTypeTest()
        {
            var stringType = new NespPolymorphicTypeInformation("abc");

            Assert.AreEqual("'abc", stringType.FullName);
            Assert.AreEqual("abc", stringType.Name);
        }
        #endregion

        #region Narrowing
        public class NarrowingBranchedBaseType0TestType
        { }

        public class NarrowingBranchedBaseType1TestType : NarrowingBranchedBaseType0TestType
        { }

        public class NarrowingBranchedBaseType21TestType : NarrowingBranchedBaseType1TestType
        { }

        public class NarrowingBranchedBaseType22TestType : NarrowingBranchedBaseType1TestType
        { }

        [Test]
        public void SelectDerivedTypeForCalculateNarrowingTest()
        {
            var context = new NespMetadataContext();

            var test0Type = new NespRuntimeTypeInformation(typeof(NarrowingBranchedBaseType0TestType).GetTypeInfo());
            var test1Type = new NespRuntimeTypeInformation(typeof(NarrowingBranchedBaseType1TestType).GetTypeInfo());

            var result0 = test0Type.CalculateNarrowing(test1Type, context);
            Assert.AreEqual(test1Type, result0);

            var result1 = test1Type.CalculateNarrowing(test0Type, context);
            Assert.AreEqual(test1Type, result1);
        }

        //[Test]
        //public void SelectBranchedBaseTypeForCalculateNarrowingTest()
        //{
        //    var context = new NespMetadataContext();

        //    var test21Type = new NespRuntimeTypeInformation(typeof(NarrowingBranchedBaseType21TestType).GetTypeInfo());
        //    var test22Type = new NespRuntimeTypeInformation(typeof(NarrowingBranchedBaseType22TestType).GetTypeInfo());

        //    var result0 = (NespPolymorphicTypeInformation)test21Type.CalculateNarrowing(test22Type, context);
        //    Assert.IsTrue(result0.);

        //    var result1 = (NespPolymorphicTypeInformation)test22Type.CalculateNarrowing(test21Type, context);
        //    Assert.AreEqual(polymorphicType, result1);
        //}

        //[Test]
        //public void SelectCompletelyAnotherTypeForCalculateNarrowingTest()
        //{
        //    var context = new NespMetadataContext();

        //    var objectType = new NespRuntimeTypeInformation(typeof(object).GetTypeInfo());
        //    var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());
        //    var test22Type = new NespRuntimeTypeInformation(typeof(NarrowingBranchedBaseType22TestType).GetTypeInfo());

        //    var result0 = stringType.CalculateNarrowing(test22Type, context);
        //    Assert.AreEqual(objectType, result0);

        //    var result1 = test22Type.CalculateNarrowing(stringType, context);
        //    Assert.AreEqual(objectType, result1);
        //}

        //[Test]
        //public void SelectCompletelyAnotherInterfaceTypeForCalculateNarrowingTest()
        //{
        //    var context = new NespMetadataContext();

        //    var objectType = new NespRuntimeTypeInformation(typeof(object).GetTypeInfo());
        //    var enumerableType = new NespRuntimeTypeInformation(typeof(IEnumerable).GetTypeInfo());
        //    var test22Type = new NespRuntimeTypeInformation(typeof(NarrowingBranchedBaseType22TestType).GetTypeInfo());

        //    var result0 = enumerableType.CalculateNarrowing(test22Type, context);
        //    Assert.AreEqual(objectType, result0);

        //    var result1 = test22Type.CalculateNarrowing(enumerableType, context);
        //    Assert.AreEqual(objectType, result1);
        //}

        //[Test]
        //public void SelectInterfaceTypeForCalculateNarrowingTest()
        //{
        //    var context = new NespMetadataContext();

        //    var enumerableCharType = new NespRuntimeTypeInformation(typeof(IEnumerable<char>).GetTypeInfo());
        //    var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());

        //    var result0 = enumerableCharType.CalculateNarrowing(stringType, context);
        //    Assert.AreEqual(stringType, result0);

        //    var result1 = stringType.CalculateNarrowing(enumerableCharType, context);
        //    Assert.AreEqual(stringType, result1);
        //}

        //[Test]
        //public void SelectCommonInterfaceTypeForCalculateNarrowingTest()
        //{
        //    var context = new NespMetadataContext();

        //    var enumerableCharType = new NespRuntimeTypeInformation(typeof(IEnumerable<char>).GetTypeInfo());
        //    var listCharType = new NespRuntimeTypeInformation(typeof(List<char>).GetTypeInfo());
        //    var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());

        //    var result0 = listCharType.CalculateNarrowing(stringType, context);
        //    Assert.AreEqual(enumerableCharType, result0);

        //    var result1 = stringType.CalculateNarrowing(listCharType, context);
        //    Assert.AreEqual(enumerableCharType, result1);
        //}

        //[Test]
        //public void SelectCommonDeeperInterfaceTypeForCalculateNarrowingTest()
        //{
        //    var context = new NespMetadataContext();

        //    var ilistType = new NespRuntimeTypeInformation(typeof(IList<char>).GetTypeInfo());
        //    var listCharType = new NespRuntimeTypeInformation(typeof(List<char>).GetTypeInfo());
        //    var charArrayType = new NespRuntimeTypeInformation(typeof(char[]).GetTypeInfo());

        //    var result0 = listCharType.CalculateNarrowing(charArrayType, context);
        //    Assert.AreEqual(ilistType, result0);

        //    var result1 = charArrayType.CalculateNarrowing(listCharType, context);
        //    Assert.AreEqual(ilistType, result1);
        //}
        #endregion
#endif
    }
}
