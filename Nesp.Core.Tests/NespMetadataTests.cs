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

            var result = context.CalculateCombinedType(stringType, int32Type);

            Assert.AreEqual("'T1", result.Combined.FullName);
            Assert.AreEqual("T1", result.Combined.Name);
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
        public void CalculateCombinedSameTypesTest()
        {
            // BaseX ------+-- DerivedY
            //            vvvvvvvvv
            //             +-- DerivedY [Widen: BaseX]

            var context = new NespMetadataContext();
            var stringType = context.FromType(typeof(string).GetTypeInfo());

            var result = context.CalculateCombinedType(stringType, stringType);

            Assert.AreSame(stringType, result.LeftFixed);
            Assert.AreSame(stringType, result.RightFixed);
            Assert.AreSame(stringType, result.Combined);
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

            var result1 = context.CalculateCombinedType(stringType, objectType);
            var result2 = context.CalculateCombinedType(objectType, stringType);

            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);
            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(stringType, result1.LeftFixed);
            Assert.AreSame(objectType, result1.RightFixed);
            Assert.AreSame(stringType, result1.Combined);
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

            var result1 = context.CalculateCombinedType(int32Type, stringType);
            var result2 = context.CalculateCombinedType(stringType, int32Type);

            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            var polymorphicType1 = (NespPolymorphicTypeInformation)result1.Combined;
            var polymorphicType2 = (NespPolymorphicTypeInformation)result2.Combined;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.AreSame(int32Type, result1.LeftFixed);
            Assert.AreSame(stringType, result1.RightFixed);
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

            var result11 = context.CalculateCombinedType(int32Type, stringType);
            var result12 = context.CalculateCombinedType(result11.Combined, uriType);

            var result21 = context.CalculateCombinedType(int32Type, stringType);
            var result22 = context.CalculateCombinedType(uriType, result21.Combined);

            Assert.AreSame(result12.LeftFixed, result22.RightFixed);
            Assert.AreSame(result12.RightFixed, result22.LeftFixed);

            var polymorphicType1 = (NespPolymorphicTypeInformation)result12.Combined;
            var polymorphicType2 = (NespPolymorphicTypeInformation)result22.Combined;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.AreSame(result11.Combined, result12.LeftFixed);
            Assert.AreSame(uriType, result12.RightFixed);
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

            var result11 = context.CalculateCombinedType(int32Type, methodInfoType);
            var result12 = context.CalculateCombinedType(result11.Combined, methodBaseType);

            var result21 = context.CalculateCombinedType(int32Type, methodInfoType);
            var result22 = context.CalculateCombinedType(methodBaseType, result21.Combined);

            Assert.AreSame(result12.LeftFixed, result22.RightFixed);
            Assert.AreSame(result12.RightFixed, result22.LeftFixed);

            var polymorphicType1 = (NespPolymorphicTypeInformation)result12.Combined;
            var polymorphicType2 = (NespPolymorphicTypeInformation)result22.Combined;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.AreSame(result11.Combined, result12.LeftFixed);
            Assert.AreSame(methodBaseType, result12.RightFixed);
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

            var result11 = context.CalculateCombinedType(int32Type, methodBaseType);
            var result12 = context.CalculateCombinedType(result11.Combined, methodInfoType);

            var result21 = context.CalculateCombinedType(int32Type, methodBaseType);
            var result22 = context.CalculateCombinedType(methodInfoType, result21.Combined);

            Assert.AreSame(result12.LeftFixed, result22.RightFixed);
            Assert.AreSame(result12.RightFixed, result22.LeftFixed);

            var polymorphicType1 = (NespPolymorphicTypeInformation)result12.Combined;
            var polymorphicType2 = (NespPolymorphicTypeInformation)result22.Combined;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.AreSame(result11.Combined, result12.LeftFixed);
            Assert.AreSame(methodInfoType, result12.RightFixed);
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

            var result11 = context.CalculateCombinedType(int32ArrayType, methodInfoType);
            var result12 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var result13 = context.CalculateCombinedType(result11.Combined, result12.Combined);

            var result21 = context.CalculateCombinedType(int32ArrayType, methodBaseType);
            var result22 = context.CalculateCombinedType(methodInfoType, int32ArrayType);
            var result23 = context.CalculateCombinedType(result21.Combined, result22.Combined);

            Assert.AreSame(result13.LeftFixed, result23.RightFixed);
            Assert.AreSame(result13.RightFixed, result23.LeftFixed);

            var polymorphicType1 = (NespPolymorphicTypeInformation)result13.Combined;
            var polymorphicType2 = (NespPolymorphicTypeInformation)result23.Combined;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.AreSame(result11.Combined, result13.LeftFixed);
            Assert.AreSame(result12.Combined, result13.RightFixed);
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

            var result11 = context.CalculateCombinedType(enumerableInt32Type, methodInfoType);
            var result12 = context.CalculateCombinedType(result11.Combined, stringType);
            var result13 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var result14 = context.CalculateCombinedType(result12.Combined, result13.Combined);

            var result21 = context.CalculateCombinedType(enumerableInt32Type, methodInfoType);
            var result22 = context.CalculateCombinedType(methodBaseType, int32ArrayType);
            var result23 = context.CalculateCombinedType(stringType, result22.Combined);
            var result24 = context.CalculateCombinedType(result23.Combined, result21.Combined);

            Assert.AreSame(result14.LeftFixed, result12.Combined);
            Assert.AreSame(result14.RightFixed, result13.Combined);
            Assert.AreSame(result24.LeftFixed, result23.Combined);
            Assert.AreSame(result24.RightFixed, result21.Combined);

            var polymorphicType1 = (NespPolymorphicTypeInformation)result14.Combined;
            var polymorphicType2 = (NespPolymorphicTypeInformation)result24.Combined;

            Assert.AreSame(polymorphicType1, polymorphicType2);

            Assert.AreSame(result12.Combined, result14.LeftFixed);
            Assert.AreSame(result13.Combined, result14.RightFixed);
            Assert.IsTrue(polymorphicType1.RuntimeTypes.SequenceEqual(
                new[] { int32ArrayType, methodInfoType, stringType }.OrderBy(t => t)));
        }
        #endregion

        #region CalculateCombined (Generic class)
        public abstract class BaseClassType<T>
        { }

        [Test]
        public void CalculateCombinedSameGenericDefinitionTypesTest()
        {
            // BaseClassType<T> ------+-- BaseClassType<T2>
            //            vvvvvvvvv
            // BaseClassType<T> ------+                     [Normalized]

            var context = new NespMetadataContext();
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());

            var result = context.CalculateCombinedType(baseType, baseType);

            Assert.AreSame(baseType, result.LeftFixed);
            Assert.AreSame(baseType, result.RightFixed);
            Assert.AreSame(baseType, result.Combined);
        }

        public class DerivedClassType1<T> : BaseClassType<T>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionTypeAndGenericDefinitionTypeTest()
        {
            // DerivedClassType1<T>   ---+--- BaseClassType<T2>
            //            vvvvvvvvv
            // DerivedClassType1<T>   ---+                       [Widen]

            // BaseClassType<T2>    ---+--- DerivedClassType1<T>
            //            vvvvvvvvv
            //                         +--- DerivedClassType1<T> [Widen]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType1<>).GetTypeInfo());
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseType);
            var result2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(derivedType, result1.Combined);
        }

        [Test]
        public void CalculateCombinedGenericDefinitionTypeAndGenericInt32Test()
        {
            // DerivedClassType1<T>    ---+--- BaseClassType<int>
            //            vvvvvvvvv
            // DerivedClassType1<int>  ---+                          [Widen: int]

            // BaseClassType<int>    ---+--- DerivedClassType1<T>
            //            vvvvvvvvv
            //                          +--- DerivedClassType1<int>  [Widen: int]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType1<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(BaseClassType<int>).GetTypeInfo());
            var derivedInt32Type = context.FromType(typeof(DerivedClassType1<int>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseInt32Type);
            var result2 = context.CalculateCombinedType(baseInt32Type, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(derivedInt32Type, result1.Combined);
        }

        public class DerivedClassType2 : BaseClassType<int>
        { }

        [Test]
        public void CalculateCombinedNonGenericTypeAndGenericDefinitionTypeTest()
        {
            // DerivedClassType2  ---+--- BaseClassType<T>
            //            vvvvvvvvv
            // DerivedClassType2  ---+                       [Widen: int]   // TODO: How to tell info about T == int?

            // BaseClassType<T>  ---+--- DerivedClassType2
            //            vvvvvvvvv
            //                      +--- DerivedClassType2   [Widen: int]   // TODO: How to tell info about T == int?

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType2).GetTypeInfo());
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseType);
            var result2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(derivedType, result1.Combined);
        }

        public class DerivedClassType3<T> : BaseClassType<int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionDerivedTypeAndGenericInt32TypeTest()
        {
            // DerivedClassType3<T>  ---+--- BaseClassType<int>
            //            vvvvvvvvv
            // DerivedClassType3<T>  ---+                       [Widen: int]

            // BaseClassType<int>  ---+--- DerivedClassType3<T>
            //            vvvvvvvvv
            //                        +--- DerivedClassType3<T> [Widen: int]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType3<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(BaseClassType<int>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseInt32Type);
            var result2 = context.CalculateCombinedType(baseInt32Type, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(derivedType, result1.Combined);
        }

        [Test]
        public void CalculateCombinedGenericDefinitionDerivedTypeAndGenericDefinitionTypeTest()
        {
            // DerivedClassType3<T>  ---+--- BaseClassType<T2>
            //            vvvvvvvvv
            // DerivedClassType3<T>  ---+                       [Widen]

            // BaseClassType<T2>  ---+--- DerivedClassType3<T>
            //            vvvvvvvvv
            //                       +--- DerivedClassType3<T>  [Widen]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType3<>).GetTypeInfo());
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseType);
            var result2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(derivedType, result1.Combined);
        }

        public class DerivedClassType4<T, U> : BaseClassType<T>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionDerivedTypeWithTwoArgumentsAndGenericDefinitionTypeTest()
        {
            // DerivedClassType4<T, U>   ---+--- BaseClassType<T2>
            //            vvvvvvvvv
            // DerivedClassType4<T, U>   ---+                       [Widen]

            // BaseClassType<T2>    ---+--- DerivedClassType4<T, U>
            //            vvvvvvvvv
            //                         +--- DerivedClassType4<T, U> [Widen]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType4<,>).GetTypeInfo());
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseType);
            var result2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);

            Assert.AreSame(derivedType, result1.Combined);
        }

        public class DerivedClassType5<T> : BaseClassType<int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionTypeAndGenericBaseTypeTest()
        {
            // DerivedClassType5<T>  ---+--- BaseClassType<T2>
            //            vvvvvvvvv
            // DerivedClassType5<T>  ---+                       [Widen: int]

            // BaseClassType<T2>  ---+--- DerivedClassType5<T>
            //            vvvvvvvvv
            //                       +--- DerivedClassType5<T>  [Widen: int]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType5<>).GetTypeInfo());
            var baseType = context.FromType(typeof(BaseClassType<>).GetTypeInfo());
            var baseInt32Type = context.FromType(typeof(BaseClassType<int>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseType);
            var result2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(derivedType, result1.Combined);
            Assert.AreSame(derivedType, result1.LeftFixed);
            Assert.AreSame(baseInt32Type, result1.RightFixed);
        }

        public class DerivedClassType6<T, U> : DerivedClassType4<U, int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionWithComplexArgumentsTypeAndGenericDefinitionTypeTest()
        {
            // DerivedClassType6<T, U>  ---+--- DerivedClassType4<T2, U2>
            //            vvvvvvvvv
            // DerivedClassType6<T, U>  ---+                              [Widen: U, int]

            // DerivedClassType4<T2, U2>  ---+--- DerivedClassType6<T, U>
            //            vvvvvvvvv
            //                               +--- DerivedClassType6<T, U> [Widen: U, int]

            var context = new NespMetadataContext();
            var derivedType = context.FromType(typeof(DerivedClassType6<,>).GetTypeInfo());
            var baseType = context.FromType(typeof(DerivedClassType4<,>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(derivedType, baseType);
            var result2 = context.CalculateCombinedType(baseType, derivedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(derivedType, result1.Combined);
            Assert.AreSame(derivedType, result1.LeftFixed);
            Assert.AreNotSame(baseType, result1.RightFixed);

            var dt = ((NespRuntimeTypeInformation)result1.RightFixed).typeInfo;

            Assert.AreEqual(typeof(DerivedClassType4<,>), dt.GetGenericTypeDefinition());

            var baseTypeArguments = dt.GetTypeInfo().GenericTypeArguments;
            Assert.AreEqual(typeof(DerivedClassType6<,>), baseTypeArguments[0].DeclaringType);
            Assert.AreEqual(typeof(int), baseTypeArguments[1]);
        }
        #endregion

        #region CalculateCombined (Generic interface)
        public interface IInterfaceType<T>
        { }

        [Test]
        public void CalculateCombinedSameGenericDefinitionInterfaceTypesTest()
        {
            // IInterfaceType<T> ------+-- IInterfaceType<T2>
            //            vvvvvvvvv
            // IInterfaceType<T> ------+                     [Normalized]

            var context = new NespMetadataContext();
            var interfaceType = context.FromType(typeof(IInterfaceType<>).GetTypeInfo());

            var result = context.CalculateCombinedType(interfaceType, interfaceType);

            Assert.AreSame(interfaceType, result.LeftFixed);
            Assert.AreSame(interfaceType, result.RightFixed);
            Assert.AreSame(interfaceType, result.Combined);
        }

        public class ImplementedClassType1<T> : IInterfaceType<T>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionTypeAndGenericDefinitionInterfaceTypeTest()
        {
            // ImplementedClassType1<T>   ---+--- IInterfaceType<T2>
            //            vvvvvvvvv
            // ImplementedClassType1<T>   ---+                       [Widen]

            // IInterfaceType<T2>    ---+--- ImplementedClassType1<T>
            //            vvvvvvvvv
            //                          +--- ImplementedClassType1<T> [Widen]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType1<>).GetTypeInfo());
            var interfaceType = context.FromType(typeof(IInterfaceType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceType);
            var result2 = context.CalculateCombinedType(interfaceType, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreNotSame(interfaceType, result1.RightFixed);

            var dt = ((NespRuntimeTypeInformation) result1.RightFixed).typeInfo;

            Assert.AreEqual(typeof(IInterfaceType<>), dt.GetGenericTypeDefinition());
            Assert.AreEqual(
                typeof(ImplementedClassType1<>),
                dt.GetTypeInfo().GenericTypeArguments[0].DeclaringType);
        }

        [Test]
        public void CalculateCombinedGenericDefinitionTypeAndGenericInt32InterfaceTypeTest()
        {
            // ImplementedClassType1<T>   ---+--- IInterfaceType<int>
            //            vvvvvvvvv
            // ImplementedClassType1<int> ---+                           [Widen: int]

            // IInterfaceType<int>    ---+--- ImplementedClassType1<T>
            //            vvvvvvvvv
            //                           +--- ImplementedClassType1<int> [Widen: int]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType1<>).GetTypeInfo());
            var interfaceInt32Type = context.FromType(typeof(IInterfaceType<int>).GetTypeInfo());
            var implementedInt32Type = context.FromType(typeof(ImplementedClassType1<int>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceInt32Type);
            var result2 = context.CalculateCombinedType(interfaceInt32Type, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedInt32Type, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreSame(interfaceInt32Type, result1.RightFixed);
        }

        public class ImplementedClassType2 : IInterfaceType<int>
        { }

        [Test]
        public void CalculateCombinedNonGenericTypeAndGenericDefinitionInterfaceTypeTest()
        {
            // ImplementedClassType2   ---+--- IInterfaceType<T>
            //            vvvvvvvvv
            // ImplementedClassType2   ---+                       [Widen: int]

            // IInterfaceType<T>    ---+--- ImplementedClassType2
            //            vvvvvvvvv
            //                         +--- ImplementedClassType2 [Widen: int]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType2).GetTypeInfo());
            var interfaceType = context.FromType(typeof(IInterfaceType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceType);
            var result2 = context.CalculateCombinedType(interfaceType, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            var dt = ((NespRuntimeTypeInformation)result1.RightFixed).typeInfo;

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreSame(typeof(int), dt.GenericTypeArguments[0]);
        }

        public class ImplementedClassType3<T> : IInterfaceType<int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionImplementedTypeAndGenericInt32InterfaceTypeTest()
        {
            // ImplementedClassType3<T>   ---+--- IInterfaceType<int>
            //            vvvvvvvvv
            // ImplementedClassType3<T>   ---+                         [Widen: int]

            // IInterfaceType<int>    ---+--- ImplementedClassType3<T>
            //            vvvvvvvvv
            //                           +--- ImplementedClassType3<T> [Widen: int]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType3<>).GetTypeInfo());
            var interfaceInt32Type = context.FromType(typeof(IInterfaceType<int>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceInt32Type);
            var result2 = context.CalculateCombinedType(interfaceInt32Type, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreSame(interfaceInt32Type, result1.RightFixed);
        }

        [Test]
        public void CalculateCombinedGenericDefinitionImplementedTypeAndGenericDefinitionInterfaceTypeTest()
        {
            // ImplementedClassType3<T>  ---+--- IInterfaceType<T2>
            //            vvvvvvvvv
            // ImplementedClassType3<T>  ---+                        [Widen]

            // IInterfaceType<T2>  ---+--- ImplementedClassType3<T>
            //            vvvvvvvvv
            //                        +--- ImplementedClassType3<T>  [Widen]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType3<>).GetTypeInfo());
            var interfaceType = context.FromType(typeof(IInterfaceType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceType);
            var result2 = context.CalculateCombinedType(interfaceType, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreNotSame(interfaceType, result1.RightFixed);

            var dt = ((NespRuntimeTypeInformation)result1.RightFixed).typeInfo;

            Assert.AreEqual(typeof(IInterfaceType<>), dt.GetGenericTypeDefinition());
            Assert.AreEqual(typeof(int), dt.GetTypeInfo().GenericTypeArguments[0]);
        }

        public class ImplementedClassType4<T, U> : IInterfaceType<T>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionImplementedTypeWithTwoArgumentsAndGenericDefinitionInterfaceTypeTest()
        {
            // ImplementedClassType4<T, U>   ---+--- IInterfaceType<T2>
            //            vvvvvvvvv
            // ImplementedClassType4<T, U>   ---+                       [Widen]

            // IInterfaceType<T2>    ---+--- ImplementedClassType4<T, U>
            //            vvvvvvvvv
            //                          +--- ImplementedClassType4<T, U> [Widen]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType4<,>).GetTypeInfo());
            var interfaceType = context.FromType(typeof(IInterfaceType<>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceType);
            var result2 = context.CalculateCombinedType(interfaceType, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreNotSame(interfaceType, result1.RightFixed);

            var dt = ((NespRuntimeTypeInformation)result1.RightFixed).typeInfo;

            Assert.AreEqual(typeof(IInterfaceType<>), dt.GetGenericTypeDefinition());
            Assert.AreEqual(
                typeof(ImplementedClassType4<,>),
                dt.GetTypeInfo().GenericTypeArguments[0].DeclaringType);
        }

        public class ImplementedClassType5<T> : IInterfaceType<int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionImplementedTypeAndGenericInterfaceTypeTest()
        {
            // ImplementedClassType5<T>  ---+--- IInterfaceType<T2>
            //            vvvvvvvvv
            // ImplementedClassType5<T>  ---+                        [Widen: int]

            // IInterfaceType<T2>  ---+--- ImplementedClassType5<T>
            //            vvvvvvvvv
            //                        +--- ImplementedClassType5<T>  [Widen: int]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType5<>).GetTypeInfo());
            var interfaceType = context.FromType(typeof(IInterfaceType<>).GetTypeInfo());
            var interfaceInt32Type = context.FromType(typeof(IInterfaceType<int>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceType);
            var result2 = context.CalculateCombinedType(interfaceType, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreSame(interfaceInt32Type, result1.RightFixed);
        }

        public interface IInterfaceType<T, U>
        { }

        public class ImplementedClassType6<T, U> : IInterfaceType<U, int>
        { }

        [Test]
        public void CalculateCombinedGenericDefinitionInterfaceWithComplexArgumentsTypeAndGenericDefinitionInterfaceTypeTest()
        {
            // ImplementedClassType6<T, U>  ---+--- IInterfaceType<T2, U2>
            //            vvvvvvvvv
            // ImplementedClassType6<T, U>  ---+                           [Widen: U, int]

            // IInterfaceType<T2, U2>  ---+--- ImplementedClassType6<T, U>
            //            vvvvvvvvv
            //                            +--- ImplementedClassType6<T, U> [Widen: U, int]

            var context = new NespMetadataContext();
            var implementedType = context.FromType(typeof(ImplementedClassType6<,>).GetTypeInfo());
            var interfaceType = context.FromType(typeof(IInterfaceType<,>).GetTypeInfo());

            var result1 = context.CalculateCombinedType(implementedType, interfaceType);
            var result2 = context.CalculateCombinedType(interfaceType, implementedType);

            Assert.AreSame(result1.Combined, result2.Combined);
            Assert.AreSame(result1.LeftFixed, result2.RightFixed);
            Assert.AreSame(result1.RightFixed, result2.LeftFixed);

            Assert.AreSame(implementedType, result1.Combined);
            Assert.AreSame(implementedType, result1.LeftFixed);
            Assert.AreNotSame(interfaceType, result1.RightFixed);

            var dt = ((NespRuntimeTypeInformation)result1.RightFixed).typeInfo;

            Assert.AreEqual(typeof(IInterfaceType<,>), dt.GetGenericTypeDefinition());

            var baseTypeArguments = dt.GetTypeInfo().GenericTypeArguments;
            Assert.AreEqual(typeof(ImplementedClassType6<,>), baseTypeArguments[0].DeclaringType);
            Assert.AreEqual(typeof(int), baseTypeArguments[1]);
        }
        #endregion
    }
}
