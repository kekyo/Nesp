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

        #region CalculateAssignableType
        [Test]
        public void CalculateAssignableTypeInt32TypeToObjectType()
        {
            var context = new NespMetadataContext();
            var objectType = context.FromType(typeof(object).GetTypeInfo());
            var int32Type = context.FromType(typeof(int).GetTypeInfo());

            Assert.IsTrue(context.CalculateAssignableType(objectType, int32Type));
            Assert.IsFalse(context.CalculateAssignableType(int32Type, objectType));
        }

        [Test]
        public void CalculateAssignableTypeInt32ArrayTypeToEnumerableInt32Type()
        {
            var context = new NespMetadataContext();
            var int32ArrayType = context.FromType(typeof(int[]).GetTypeInfo());
            var enumerableInt32Type = context.FromType(typeof(IEnumerable<int>).GetTypeInfo());

            Assert.IsTrue(context.CalculateAssignableType(enumerableInt32Type, int32ArrayType));
            Assert.IsFalse(context.CalculateAssignableType(int32ArrayType, enumerableInt32Type));
        }

        [Test]
        public void CalculateAssignableTypeInt32TypeAndStringTypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var polymorphicType = context.CalculateCombinedType(int32Type, stringType);

            Assert.IsTrue(context.CalculateAssignableType(polymorphicType, int32Type));
            Assert.IsFalse(context.CalculateAssignableType(int32Type, polymorphicType));

            Assert.IsTrue(context.CalculateAssignableType(polymorphicType, stringType));
            Assert.IsFalse(context.CalculateAssignableType(stringType, polymorphicType));
        }

        [Test]
        public void CalculateAssignableTypeObjectTypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var objectType = context.FromType(typeof(object).GetTypeInfo());

            var polymorphicType = context.CalculateCombinedType(int32Type, stringType);

            Assert.IsTrue(context.CalculateAssignableType(objectType, polymorphicType));
            Assert.IsFalse(context.CalculateAssignableType(polymorphicType, objectType));
        }

        [Test]
        public void CalculateAssignableTypeAnotherTypeToPolymorphicType()
        {
            var context = new NespMetadataContext();
            var int32Type = context.FromType(typeof(int).GetTypeInfo());
            var stringType = context.FromType(typeof(string).GetTypeInfo());
            var uriType = context.FromType(typeof(Uri).GetTypeInfo());

            var polymorphicType = context.CalculateCombinedType(stringType, int32Type);

            Assert.IsFalse(context.CalculateAssignableType(polymorphicType, uriType));
            Assert.IsFalse(context.CalculateAssignableType(uriType, polymorphicType));
        }
        #endregion

        #region CalculateAssignable (Generic)
        public abstract class TestBaseClass1<T>
        { }

        public sealed class TestDeriveClass1<T> : TestBaseClass1<T>
        { }

        [Test]
        public void CalculateAssignableTypeFromDeriveInt32ToBaseGenericType()
        {
            // TestBaseClass1<T> <--- TestDeriveClass1<int>
            //     vvvvvvvvv
            // TestBaseClass1<int> ,  TestDeriveClass1<int>    [MakeGenericType: int]

            var context = new NespMetadataContext();
            var deriveInt32Type = context.FromType(typeof(TestDeriveClass1<int>).GetTypeInfo());
            var baseType = context.FromType(typeof(TestBaseClass1<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(TestBaseClass1<int>).GetTypeInfo());

            var result1 = context.CalculateAssignableType(baseType, deriveInt32Type);

            Assert.IsTrue(result1);
            Assert.AreSame(deriveInt32Type, result1.From);
            Assert.AreSame(baseInt32Type, result1.To);

            Assert.IsFalse(context.CalculateAssignableType(deriveInt32Type, baseType));
        }

        [Test]
        public void CalculateAssignableTypeFromDeriveGenericToBaseInt32Type()
        {
            // TestBaseClass1<int> <--- TestDeriveClass1<T>
            //     vvvvvvvvv
            // TestBaseClass1<int> ,  TestDeriveClass1<int>    [MakeGenericType: int]

            var context = new NespMetadataContext();
            var deriveType = context.FromType(typeof(TestDeriveClass1<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(TestBaseClass1<int>).GetTypeInfo());
            var deriveInt32Type = context.FromType(typeof(TestDeriveClass1<int>).GetTypeInfo());

            var result1 = context.CalculateAssignableType(baseInt32Type, deriveType);

            Assert.IsTrue(result1);
            Assert.AreSame(deriveInt32Type, result1.From);
            Assert.AreSame(baseInt32Type, result1.To);

            Assert.IsFalse(context.CalculateAssignableType(deriveType, baseInt32Type));
        }

        public sealed class TestDeriveClass2<T, U> : TestBaseClass1<U>
        { }

        [Test]
        public void CalculateAssignableTypeFromDeriveInt32StringToBaseGenericType()
        {
            // TestBaseClass1<T>   <--- TestDeriveClass1<int, string>
            //     vvvvvvvvv
            // TestBaseClass1<string>                [MakeGenericType: string]

            var context = new NespMetadataContext();
            var deriveInt32StringType = context.FromType(typeof(TestDeriveClass2<int, string>).GetTypeInfo());
            var baseType = context.FromType(typeof(TestBaseClass1<>).GetTypeInfo());
            var baseStringType = context.FromType(typeof(TestBaseClass1<string>).GetTypeInfo());

            var result1 = context.CalculateAssignableType(baseType, deriveInt32StringType);

            Assert.IsTrue(result1);
            Assert.AreSame(deriveInt32StringType, result1.From);
            Assert.AreSame(baseStringType, result1.To);

            Assert.IsFalse(context.CalculateAssignableType(deriveInt32StringType, baseType));
        }
        #endregion

        #region CalculateCombined
        [Test]
        public void CalculateCombinedBothEqualTypesTest()
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
        public void CalculateCombinedAssignableTypesTest()
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
        public void CalculateCombinedUnassignableTypesTest()
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
                new[] { int32Type, stringType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedUnassignableTypeAndPolymorphicTypeTest()
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
        public void CalculateCombinedAssignableTypeFromPolymorphicTypeTest()
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
        public void CalculateCombinedAssignableTypeToPolymorphicTypeTest()
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
        public void CalculateCombinedPolymorphicTypeFromAssignablePolymorphicTypeTest()
        {
            // int[]    --+---+-- BaseX
            // DerivedY --+   +-- int[]
            //           vvvvvvvvv
            // int[]    --+
            // DerivedY --+

            var context = new NespMetadataContext();
            var methodBaseType = context.FromType(typeof(MethodBase).GetTypeInfo());
            var methodInfoType = context.FromType(typeof(MethodInfo).GetTypeInfo());
            var int32ArrayType = context.FromType(typeof(int[]).GetTypeInfo());

            var combinedType11 = context.CalculateCombinedType(int32ArrayType, methodInfoType);
            var combinedType12 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var combinedType13 = context.CalculateCombinedType(combinedType11, combinedType12);

            var combinedType21 = context.CalculateCombinedType(int32ArrayType, methodBaseType);
            var combinedType22 = context.CalculateCombinedType(methodInfoType, int32ArrayType);
            var combinedType23 = context.CalculateCombinedType(combinedType21, combinedType22);

            var polymorphicType1 = (NespPolymorphicTypeInformation)combinedType13;
            var polymorphicType2 = (NespPolymorphicTypeInformation)combinedType23;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32ArrayType, methodInfoType }.OrderBy(t => t)));
        }

        [Test]
        public void CalculateCombinedBothAssignablePolymorphicTypesAndUnassignableType()
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
    }
}
