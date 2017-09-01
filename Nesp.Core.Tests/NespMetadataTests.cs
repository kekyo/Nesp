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

        #region CalculateCombined (Generic)
        public abstract class BaseClassType<T>
        { }

        public class DerivedClassType1<T> : BaseClassType<T>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionTypeAndGenericInt32Test()
        {
            // DerivedClassType1<T>    ---+--- BaseClassType<int>
            //            vvvvvvvvv
            // DerivedClassType1<int>  ---+                       [Widen: int]

            // BaseClassType<int>    ---+--- DerivedClassType1<T>
            //            vvvvvvvvv
            //                          +--- DerivedClassType1<int>  [Widen: int]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType1<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(BaseClassType<int>).GetTypeInfo());
            var derivedInt32Type = context.FromType(typeof(DerivedClassType1<int>).GetTypeInfo());

            var combinedType1 = context.CalculateCombinedType(derivedType, baseInt32Type);
            var combinedType2 = context.CalculateCombinedType(baseInt32Type, derivedType);

            Assert.AreSame(combinedType1, combinedType2);

            Assert.AreSame(derivedInt32Type, combinedType1);
        }

        public class DerivedClassType2 : BaseClassType<int>
        { }

        [Test]
        public void CalculateCombinedNonGenericTypeAndGenericDefinitionTypeTest()
        {
            // DerivedClassType2  ---+--- BaseClassType<T>
            //            vvvvvvvvv
            // DerivedClassType2  ---+                       [Widen]

            // BaseClassType<T>  ---+--- DerivedClassType2
            //            vvvvvvvvv
            //                      +--- DerivedClassType2   [Widen]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType2).GetTypeInfo());
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());

            var combinedType1 = context.CalculateCombinedType(derivedType, baseType);
            var combinedType2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(combinedType1, combinedType2);

            Assert.AreSame(derivedType, combinedType1);
        }

        public class DerivedClassType3<T> : BaseClassType<int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionDerivedTypeAndGenericInt32TypeTest()
        {
            // DerivedClassType3<T>  ---+--- BaseClassType<int>
            //            vvvvvvvvv
            // DerivedClassType3<T>  ---+                       [Widen]

            // BaseClassType<int>  ---+--- DerivedClassType3<T>
            //            vvvvvvvvv
            //                        +--- DerivedClassType3<T> [Widen]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType3<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(BaseClassType<int>).GetTypeInfo());

            var combinedType1 = context.CalculateCombinedType(derivedType, baseInt32Type);
            var combinedType2 = context.CalculateCombinedType(baseInt32Type, derivedType);

            Assert.AreSame(combinedType1, combinedType2);

            Assert.AreSame(derivedType, combinedType1);
        }
        #endregion
    }
}
