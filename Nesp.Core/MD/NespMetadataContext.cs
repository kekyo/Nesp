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
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.MD
{
    public struct NespCalculateCombinedResult
    {
        public readonly NespTypeInformation LeftFixed;
        public readonly NespTypeInformation RightFixed;
        public readonly NespTypeInformation Combined;

        internal NespCalculateCombinedResult(
            NespTypeInformation left, NespTypeInformation right, NespTypeInformation combined)
        {
            this.LeftFixed = left;
            this.RightFixed = right;
            this.Combined = combined;
        }

        public override string ToString()
        {
            return $"{this.Combined} [{this.LeftFixed}, {this.RightFixed}]";
        }
    }

    public abstract class NespTypeInformation
        : IEquatable<NespTypeInformation>, IComparable<NespTypeInformation>
    {
        internal NespTypeInformation()
        {
        }

        public abstract string FullName { get; }
        public abstract string Name { get; }

        internal abstract NespCalculateCombinedResult CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation rhsType);

        public bool Equals(NespTypeInformation rhs)
        {
            return rhs != null && object.ReferenceEquals(this, rhs);
        }

        public override bool Equals(object rhs)
        {
            return this.Equals(rhs as NespTypeInformation);
        }

        public override int GetHashCode()
        {
            return this.FullName.GetHashCode();
        }

        public int CompareTo(NespTypeInformation rhs)
        {
            return StringComparer.Ordinal.Compare(this.FullName, rhs.FullName);
        }
    }

    public sealed class NespRuntimeTypeInformation : NespTypeInformation
    {
        private readonly TypeInfo typeInfo;

        internal NespRuntimeTypeInformation(TypeInfo typeInfo)
        {
            this.typeInfo = typeInfo;
        }

        public override string FullName => NespUtilities.GetReadableTypeName(typeInfo);
        public override string Name => NespUtilities.GetReservedReadableTypeName(typeInfo)
            .Split('.')
            .Last();

        public bool IsGenericTypeDefinition => this.typeInfo.IsGenericTypeDefinition;
        public bool IsGenericParameter => this.typeInfo.IsGenericParameter;

        public bool IsEqualsOfType(Type type) =>
            (type != null) && typeInfo.Equals(type.GetTypeInfo());

        public bool IsEqualsOfType(TypeInfo typeInfo) =>
            (typeInfo != null) && this.typeInfo.Equals(typeInfo);

        public NespRuntimeTypeInformation GetDeclaringType(NespMetadataContext context)
        {
            var declaringTypeInfo = typeInfo.DeclaringType?.GetTypeInfo();
            return (declaringTypeInfo != null)
                ? (NespRuntimeTypeInformation)context.FromType(declaringTypeInfo)
                : null;
        }

        public NespRuntimeTypeInformation[] GetAllRelatedTypes(NespMetadataContext context)
        {
            return typeInfo
                .Traverse(t => t.BaseType?.GetTypeInfo())
                .Concat(typeInfo.ImplementedInterfaces.Select(t => t.GetTypeInfo()))
                .Select(ti => (NespRuntimeTypeInformation)context.FromType(ti))
                .ToArray();
        }

        public NespRuntimeTypeInformation GetGenericTypeDefinition(NespMetadataContext context)
        {
            return (NespRuntimeTypeInformation)context.FromType(
                typeInfo.GetGenericTypeDefinition().GetTypeInfo());
        }

        public NespRuntimeTypeInformation[] GetGenericArguments(NespMetadataContext context)
        {
            return (typeInfo.IsGenericTypeDefinition
                ? typeInfo.GenericTypeParameters
                : typeInfo.GenericTypeArguments)
                .Select(t => (NespRuntimeTypeInformation)context.FromType(t.GetTypeInfo()))
                .ToArray();
        }

        public NespRuntimeTypeInformation MakeGenericType(
            NespMetadataContext context, IEnumerable<NespRuntimeTypeInformation> types)
        {
            var genericTypeInfo = typeInfo.MakeGenericType(types
                .Select(t => t.typeInfo.AsType())
                .ToArray())
                .GetTypeInfo();
            return (NespRuntimeTypeInformation)context.FromType(genericTypeInfo);
        }

        internal bool IsAssignableFrom(NespRuntimeTypeInformation fromType)
        {
            return typeInfo.IsAssignableFrom(fromType.typeInfo);
        }

        internal NespRuntimeTypeInformation GetEquatableType(NespMetadataContext context)
        {
            return (NespRuntimeTypeInformation)context.FromType(
                (typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition)
                ? typeInfo.GetGenericTypeDefinition().GetTypeInfo()
                : typeInfo);
        }

        internal override NespCalculateCombinedResult CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation rhsType)
        {
            var rt = rhsType as NespRuntimeTypeInformation;
            if (rt != null)
            {
                /////////////////////////////////////////////
                // type is runtime (Both runtime type)

                return context.CalculateCombinedBothRuntimeTypes(this, rt);
            }
            else
            {
                /////////////////////////////////////////////
                // type is polymorphic (runtime vs polymorphic)

                var pt = (NespPolymorphicTypeInformation)rhsType;
                return context.CalculateCombinedDifferentTypes(this, pt);
            }
        }

        public override string ToString()
        {
            return this.FullName;
        }

        public override int GetHashCode()
        {
            return typeInfo.GetHashCode();
        }
    }

    public sealed class NespPolymorphicTypeInformation : NespTypeInformation
    {
        internal NespPolymorphicTypeInformation(string name, NespRuntimeTypeInformation[] types)
        {
            this.Name = name;
            this.RuntimeTypes = types;
        }

        public override string FullName => "'" + this.Name;
        public override string Name { get; }

        public readonly NespRuntimeTypeInformation[] RuntimeTypes;

        internal override NespCalculateCombinedResult CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation rhsType)
        {
            /////////////////////////////////////////////
            // type is runtime (polymorphic vs runtime)

            var rt = rhsType as NespRuntimeTypeInformation;
            if (rt != null)
            {
                // swap lhs and rhs
                var result = context.CalculateCombinedDifferentTypes(rt, this);
                return new NespCalculateCombinedResult(
                    result.RightFixed, result.LeftFixed, result.Combined);
            }

            /////////////////////////////////////////////
            // type is polymorphic (both polymorphic type)

            var pt = (NespPolymorphicTypeInformation)rhsType;
            return context.CalculateCombinedBothPolymorphicTypes(this, pt);
        }

        public override string ToString()
        {
            var t = string.Join(",", this.RuntimeTypes.Select(rt => rt.ToString()));
            return $"{this.FullName} [{t}]";
        }

        public override int GetHashCode()
        {
            return this.RuntimeTypes.Aggregate(
                this.FullName.GetHashCode(),
                (last, type) => last ^ type.GetHashCode());
        }
    }

    public sealed class NespMetadataContext
    {
        internal static readonly NespRuntimeTypeInformation[] emptyRuntimeTypes =
            new NespRuntimeTypeInformation[0];

        private sealed class RuntimeTypesComparer : IEqualityComparer<NespRuntimeTypeInformation[]>
        {
            public static readonly RuntimeTypesComparer Instance = new RuntimeTypesComparer();

            private RuntimeTypesComparer()
            {
            }

            public bool Equals(NespRuntimeTypeInformation[] lhs, NespRuntimeTypeInformation[] rhs)
            {
                return lhs.SequenceEqual(rhs);
            }

            public int GetHashCode(NespRuntimeTypeInformation[] types)
            {
                return types.Aggregate(0, (last, type) => last ^ type.GetHashCode());
            }
        }

        private readonly Dictionary<TypeInfo, NespRuntimeTypeInformation> runtimeTypes =
            new Dictionary<TypeInfo, NespRuntimeTypeInformation>();
        private readonly Dictionary<NespRuntimeTypeInformation[], NespPolymorphicTypeInformation> polymorphicTypes =
            new Dictionary<NespRuntimeTypeInformation[], NespPolymorphicTypeInformation>(RuntimeTypesComparer.Instance);

        private int polymorphicTypeNameIndex = 0;

        public NespTypeInformation FromType(TypeInfo typeInfo)
        {
            lock (runtimeTypes)
            {
                if (runtimeTypes.TryGetValue(typeInfo, out var type) == false)
                {
                    type = new NespRuntimeTypeInformation(typeInfo);
                    runtimeTypes.Add(typeInfo, type);
                }
                return type;
            }
        }

        internal string GeneratePolymorphicTypeName()
        {
            var index = ++polymorphicTypeNameIndex;
            return "T" + index;
        }

        internal NespPolymorphicTypeInformation GetOrAddPolymorphicType(
            IEnumerable<NespRuntimeTypeInformation> types)
        {
            var memo = types.ToArray();
            Debug.Assert(memo.SequenceEqual(memo.OrderBy(t => t)));

            lock (polymorphicTypes)
            {
                if (polymorphicTypes.TryGetValue(memo, out var type) == false)
                {
                    var name = this.GeneratePolymorphicTypeName();
                    type = new NespPolymorphicTypeInformation(name, memo);
                    polymorphicTypes.Add(memo, type);
                }
                return type;
            }
        }

        #region Calculate by both runtime types
        private NespCalculateCombinedResult? TryCalculateCombinedComplexTypes(
            NespRuntimeTypeInformation lhsType, NespRuntimeTypeInformation rhsType)
        {
            var rhsEquatableType = rhsType.GetEquatableType(this);
            var lhsBaseType = lhsType
                .GetAllRelatedTypes(this)
                .FirstOrDefault(t => t.GetEquatableType(this).Equals(rhsEquatableType));
            if (lhsBaseType == null)
            {
                return null;
            }

            if (lhsType.IsGenericTypeDefinition)
            {
                // DerivedClassType1<T>   ---+--- BaseClassType<T2>
                //            vvvvvvvvv
                // DerivedClassType1<T>   ---+                        [Widen]

                // DerivedClassType1<T>    ---+--- BaseClassType<int>
                //            vvvvvvvvv
                // DerivedClassType1<int>  ---+                       [Widen: int]

                // DerivedClassType4<T, U>   ---+--- BaseClassType<T2>
                //            vvvvvvvvv
                // DerivedClassType4<T, U>   ---+                     [Widen]

                // DerivedClassType5<T>  ---+--- BaseClassType<T2>
                //            vvvvvvvvv
                // DerivedClassType5<T>  ---+                       [Widen: int]

                // DerivedClassType6<T, U>  ---+--- DerivedClassType4<T2, U2>
                //            vvvvvvvvv
                // DerivedClassType6<T, U>  ---+                              [Widen: U, int]

                // ImplementedClassType1<T>   ---+--- IInterfaceType<T2>
                //            vvvvvvvvv
                // ImplementedClassType1<T>   ---+                       [Widen]

                // ImplementedClassType1<T>   ---+--- IInterfaceType<int>
                //            vvvvvvvvv
                // ImplementedClassType1<int> ---+                           [Widen: int]

                // ImplementedClassType4<T, U>   ---+--- IInterfaceType<T2>
                //            vvvvvvvvv
                // ImplementedClassType4<T, U>   ---+                       [Widen]

                // ImplementedClassType5<T>  ---+--- IInterfaceType<T2>
                //            vvvvvvvvv
                // ImplementedClassType5<T>  ---+                        [Widen: int]

                // Calculate T --> T2 relation result
                var rhsFixedType = rhsType.IsGenericTypeDefinition
                    ? rhsType.MakeGenericType(this, lhsBaseType.GetGenericArguments(this))
                    : rhsType;

                var argumentTypeMap = lhsBaseType.GetGenericArguments(this)
                    .Zip(rhsType.GetGenericArguments(this),
                        (lhsArgument, rhsArgument) => new { lhsArgument, rhsArgument })
                    .Where(entry => entry.rhsArgument.IsGenericParameter == false)
                    .ToDictionary(entry => entry.lhsArgument, entry => entry.rhsArgument);
                var lhsMappedArguments = lhsType.GetGenericArguments(this)
                    .Select(t => argumentTypeMap.TryGetValue(t, out var r) ? r : t)
                    .ToArray();
                var combinedType = lhsType.MakeGenericType(this, lhsMappedArguments);

                return new NespCalculateCombinedResult(
                    lhsType, rhsFixedType, combinedType);
            }

            if (rhsType.IsGenericTypeDefinition)
            {
                // DerivedClassType2  ---+--- BaseClassType<T>
                //            vvvvvvvvv
                // DerivedClassType2  ---+                       [Widen: int]

                // ImplementedClassType2   ---+--- IInterfaceType<T>
                //            vvvvvvvvv
                // ImplementedClassType2   ---+                       [Widen: int]

                // Calculate T --> T2 relation result
                var rhsFixedType = rhsType.MakeGenericType(
                    this, lhsBaseType.GetGenericArguments(this));

                return new NespCalculateCombinedResult(lhsType, rhsFixedType, lhsType);
            }

            return null;
        }

        internal NespCalculateCombinedResult CalculateCombinedBothRuntimeTypes(
            NespRuntimeTypeInformation lhsType, NespRuntimeTypeInformation rhsType)
        {
            if (rhsType.IsAssignableFrom(lhsType))
            {
                // DerivedY ------+-- BaseX
                //            vvvvvvvvv
                // DerivedY ------+         [Widen: BaseX]

                // BaseClassType<T> ------+-- BaseClassType<T2>
                //            vvvvvvvvv
                // BaseClassType<T> ------+                     [Normalized]

                // IInterfaceType<T> ------+-- IInterfaceType<T2>
                //            vvvvvvvvv
                // IInterfaceType<T> ------+                     [Normalized]

                // DerivedClassType3<T>  ---+--- BaseClassType<int>
                //            vvvvvvvvv
                // DerivedClassType3<T>  ---+                       [Widen: int]

                // ImplementedClassType1<T>   ---+--- IInterfaceType<int>
                //            vvvvvvvvv
                // ImplementedClassType1<T>   ---+                         [Widen: int]

                return new NespCalculateCombinedResult(lhsType, rhsType, lhsType);
            }

            if (lhsType.IsAssignableFrom(rhsType))
            {
                // BaseX ------+-- DerivedY
                //            vvvvvvvvv
                //             +-- DerivedY [Widen: BaseX]

                // BaseClassType<int>  ---+--- DerivedClassType3<T>
                //            vvvvvvvvv
                //                        +--- DerivedClassType3<T> [Widen: int]

                // IInterfaceType<int>    ---+--- ImplementedClassType1<T>
                //            vvvvvvvvv
                //                           +--- ImplementedClassType1<T> [Widen: int]

                return new NespCalculateCombinedResult(lhsType, rhsType, rhsType);
            }

            var result = this.TryCalculateCombinedComplexTypes(lhsType, rhsType);
            if (result.HasValue)
            {
                return result.Value;
            }

            result = this.TryCalculateCombinedComplexTypes(rhsType, lhsType);
            if (result.HasValue)
            {
                return new NespCalculateCombinedResult(
                    result.Value.RightFixed, result.Value.LeftFixed, result.Value.Combined);
            }

            // BaseX ------+-- int
            //         vvvvvvvvv
            //             +-- int
            //             +-- BaseX [Combined: BaseX]
            return new NespCalculateCombinedResult(lhsType, rhsType,
                this.GetOrAddPolymorphicType(new[] { lhsType, rhsType }.OrderBy(t => t)));
        }
        #endregion

        #region Calculate by different types
        private static IEnumerable<NespRuntimeTypeInformation> UnwrapRuntimeTypes<T>(
            IEnumerable<T> enumerable, Func<T, NespTypeInformation> extractor)
        {
            foreach (var value in enumerable)
            {
                var type = extractor(value);
                var pt = type as NespPolymorphicTypeInformation;
                if (pt != null)
                {
                    foreach (var inner in pt.RuntimeTypes)
                    {
                        yield return inner;
                    }
                }
                else
                {
                    yield return (NespRuntimeTypeInformation)type;
                }
            }
        }

        private NespTypeInformation GetOrAddTypeFromTypes(NespRuntimeTypeInformation[] types)
        {
            return (types.Length >= 2)
                ? (NespTypeInformation)this.GetOrAddPolymorphicType(types)
                : types[0];
        }

        private NespTypeInformation RecalculateByCombinedRuntimeType(
            NespPolymorphicTypeInformation rhsType,
            NespRuntimeTypeInformation[] combinedRuntimeTypes,
            Func<NespCalculateCombinedResult, NespTypeInformation> extractor)
        {
            var recomputedResultsByRuntimeType =
                (from combinedRuntimeType in combinedRuntimeTypes
                 from rt in rhsType.RuntimeTypes
                 select this.CalculateCombinedType(combinedRuntimeType, rt))
                .ToArray();

            var recomputedCombinedTypes =
                UnwrapRuntimeTypes(recomputedResultsByRuntimeType, extractor)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();

            return this.GetOrAddTypeFromTypes(recomputedCombinedTypes);
        }

        internal NespCalculateCombinedResult CalculateCombinedDifferentTypes(
            NespRuntimeTypeInformation lhsType, NespPolymorphicTypeInformation rhsType)
        {
            var results = rhsType.RuntimeTypes
                .Select(rt => this.CalculateCombinedType(lhsType, rt))
                .ToArray();

            var combinedRuntimeTypes = results
                .Collect(result => result.Combined as NespRuntimeTypeInformation)
                .Distinct()
                .ToArray();
            if (combinedRuntimeTypes.Length >= 1)
            {
                // BaseX ------+-- int
                //             +-- DerivedY
                //         vvvvvvvvv
                //             +-- int
                //             +-- DerivedY [Widen: BaseX]

                // BaseX ------+-- int
                //             +-- DerivedY
                //             +-- DerivedZ
                //         vvvvvvvvv
                //             +-- int
                //             +-- DerivedY [Widen: BaseX]
                //             +-- DerivedZ [Widen: BaseX]

                var combinedType = this.RecalculateByCombinedRuntimeType(
                    rhsType, combinedRuntimeTypes, result => result.Combined);
                var leftFixedType = this.RecalculateByCombinedRuntimeType(
                    rhsType, combinedRuntimeTypes, result => result.LeftFixed);
                var rightFixedType = this.RecalculateByCombinedRuntimeType(
                    rhsType, combinedRuntimeTypes, result => result.RightFixed);

                return new NespCalculateCombinedResult(leftFixedType, rightFixedType, combinedType);
            }

            // DerivedY ------+-- int
            //                +-- BaseX
            //            vvvvvvvvv
            //                +-- int
            //                +-- DerivedY [Widen: BaseX]

            // BaseX ------+-- int
            //             +-- string
            //         vvvvvvvvv
            //             +-- int
            //             +-- string
            //             +-- BaseX  [Combined: BaseX]

            var combinedResults =
                UnwrapRuntimeTypes(results, result => result.Combined)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            var combinedResultType = this.GetOrAddTypeFromTypes(combinedResults);

            var leftFixedResults =
                UnwrapRuntimeTypes(results, result => result.LeftFixed)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            var leftFixedResultType = this.GetOrAddTypeFromTypes(leftFixedResults);

            var rightFixedResults =
                UnwrapRuntimeTypes(results, result => result.RightFixed)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            var rightFixedResultType = this.GetOrAddTypeFromTypes(rightFixedResults);

            return new NespCalculateCombinedResult(leftFixedResultType, rightFixedResultType, combinedResultType);
        }
        #endregion

        #region Calculate by both polymorphic types
        internal NespCalculateCombinedResult CalculateCombinedBothPolymorphicTypes(
            NespPolymorphicTypeInformation lhsType, NespPolymorphicTypeInformation rhsType)
        {
            // TODO: Reduce calculation costs
            var assignableWiden = lhsType.RuntimeTypes
                .SelectMany(t1 => rhsType.RuntimeTypes
                    .SelectMany(t2 => t1.IsAssignableFrom(t2)
                        ? new[] { t2 }
                        : t2.IsAssignableFrom(t1)
                            ? new[] { t1 }
                            : emptyRuntimeTypes));
            var combinedUnassignablesLeft = lhsType.RuntimeTypes
                .SelectMany(t1 => rhsType.RuntimeTypes
                    .All(t2 => !t1.IsAssignableFrom(t2) && !t2.IsAssignableFrom(t1))
                    ? new[] { t1 }
                    : emptyRuntimeTypes);
            var combinedUnassignablesRight = rhsType.RuntimeTypes
                .SelectMany(t2 => lhsType.RuntimeTypes
                    .All(t1 => !t1.IsAssignableFrom(t2) && !t2.IsAssignableFrom(t1))
                    ? new[] { t2 }
                    : emptyRuntimeTypes);
            var combined = assignableWiden
                .Concat(combinedUnassignablesLeft)
                .Concat(combinedUnassignablesRight)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            if (combined.SequenceEqual(lhsType.RuntimeTypes) == false)
            {
                // IE<int>   --+---+-- BaseX
                // DerivedY  --+   +-- int[]
                // string    --+
                //           vvvvvvvvv
                // int[]     --+                 [Widen: IE<int>]
                // DerivedY  --+                 [Widen: BaseX]
                // string    --+                 [Combined: string]
                return new NespCalculateCombinedResult(
                    lhsType, rhsType, this.GetOrAddPolymorphicType(combined));
            }

            // int[]    --+---+-- BaseX
            // DerivedY --+   +-- int[]
            //           vvvvvvvvv
            // int[]    --+
            // DerivedY --+
            return new NespCalculateCombinedResult(lhsType, rhsType, lhsType);
        }
        #endregion

        public NespCalculateCombinedResult CalculateCombinedType(
            NespTypeInformation lhsType, NespTypeInformation rhsType)
        {
            return lhsType.CalculateCombinedTypeWith(this, rhsType);
        }
    }
}
