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
        public void IsGenericTypeFromRuntimeTypeTest()
        {
            var stringType = new NespRuntimeTypeInformation(typeof(string).GetTypeInfo());
            Assert.IsFalse(stringType.IsGenericType);

            var intType = new NespRuntimeTypeInformation(typeof(int).GetTypeInfo());
            Assert.IsFalse(intType.IsGenericType);

            var dateTimeKindType = new NespRuntimeTypeInformation(typeof(DateTimeKind).GetTypeInfo());
            Assert.IsFalse(dateTimeKindType.IsGenericType);

            var listIntType = new NespRuntimeTypeInformation(typeof(List<int>).GetTypeInfo());
            Assert.IsTrue(listIntType.IsGenericType);

            var enumerableIntType = new NespRuntimeTypeInformation(typeof(IEnumerable<int>).GetTypeInfo());
            Assert.IsTrue(enumerableIntType.IsGenericType);
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

            var genericArguments = testType.GetPolymorphicParameters(context);
            Assert.AreEqual(1, genericArguments.Length);

            var genericArgument0 = genericArguments[0];
            Assert.IsFalse(genericArgument0.IsValueTypeConstraint);
            Assert.IsTrue(genericArgument0.IsReferenceConstraint);
            Assert.IsTrue(genericArgument0.IsDefaultConstractorConstraint);

            var constraints = genericArgument0.GetPolymorphicParameterConstraints(context);
            Assert.AreEqual(1, constraints.Length);

            var enumerableIntType = new NespRuntimeTypeInformation(typeof(IEnumerable<int>).GetTypeInfo());
            Assert.AreEqual(enumerableIntType, constraints[0]);
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

        #region Inference
        public class SelectBranchedBaseType0TestType
        { }

        public class SelectBranchedBaseType1TestType : SelectBranchedBaseType0TestType
        { }

        public class SelectBranchedBaseType21TestType : SelectBranchedBaseType1TestType
        { }

        public class SelectBranchedBaseType22TestType : SelectBranchedBaseType1TestType
        { }

        [Test]
        public void SelectDerivedTypeForCalculateNarrowingTest()
        {
            var context = new NespMetadataContext();

            var test0Type = new NespRuntimeTypeInformation(typeof(SelectBranchedBaseType0TestType).GetTypeInfo());
            var test1Type = new NespRuntimeTypeInformation(typeof(SelectBranchedBaseType1TestType).GetTypeInfo());

            var result = test0Type.CalculateNarrowing(test1Type, context);

            Assert.AreEqual(test1Type, result);
        }

        [Test]
        public void SelectBranchedBaseTypeForCalculateNarrowingTest()
        {
            var context = new NespMetadataContext();

            var test1Type = new NespRuntimeTypeInformation(typeof(SelectBranchedBaseType1TestType).GetTypeInfo());
            var test21Type = new NespRuntimeTypeInformation(typeof(SelectBranchedBaseType21TestType).GetTypeInfo());
            var test22Type = new NespRuntimeTypeInformation(typeof(SelectBranchedBaseType22TestType).GetTypeInfo());

            var result = test21Type.CalculateNarrowing(test22Type, context);

            Assert.AreEqual(test1Type, result);
        }
        #endregion
    }
}
