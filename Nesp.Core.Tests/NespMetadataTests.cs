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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Nesp.Metadatas;

using NUnit.Framework;

namespace Nesp
{
    [TestFixture]
    public sealed class NespMetadataTests
    {
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
    }
}
