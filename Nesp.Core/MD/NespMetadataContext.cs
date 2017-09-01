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
        internal readonly TypeInfo typeInfo;

        internal NespRuntimeTypeInformation(TypeInfo typeInfo)
        {
            this.typeInfo = typeInfo;
        }

        public override string FullName => NespUtilities.GetReadableTypeName(typeInfo);
        public override string Name => NespUtilities.GetReservedReadableTypeName(typeInfo)
            .Split('.')
            .Last();

        public TypeInfo RuntimeType => this.typeInfo;

        private static TypeInfo GetEquatableTypeInfo(TypeInfo type)
        {
            return (type.IsGenericType && !type.IsGenericTypeDefinition)
                ? type.GetGenericTypeDefinition().GetTypeInfo()
                : type;
        }

        private static Type[] GetGenericArguments(TypeInfo type)
        {
            return type.IsGenericTypeDefinition
                ? type.GenericTypeParameters
                : type.GenericTypeArguments;
        }

        private static NespCalculateCombinedResult CalculateCombinedRuntimeTypes(
            NespMetadataContext context, NespRuntimeTypeInformation lhsType, NespRuntimeTypeInformation rhsType)
        {
            var lhsTypeInfo = lhsType.typeInfo;
            var rhsTypeInfo = rhsType.typeInfo;

            if (lhsTypeInfo.IsAssignableFrom(rhsTypeInfo))
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

            if (rhsTypeInfo.IsAssignableFrom(lhsTypeInfo))
            {
                // DerivedY ------+-- BaseX
                //            vvvvvvvvv
                // DerivedY ------+         [Widen: BaseX]

                // DerivedClassType3<T>  ---+--- BaseClassType<int>
                //            vvvvvvvvv
                // DerivedClassType3<T>  ---+                       [Widen: int]

                // ImplementedClassType1<T>   ---+--- IInterfaceType<int>
                //            vvvvvvvvv
                // ImplementedClassType1<T>   ---+                         [Widen: int]

                return new NespCalculateCombinedResult(lhsType, rhsType, lhsType);
            }

            var rhsEquatableTypeInfo = GetEquatableTypeInfo(rhsTypeInfo);
            var lhsBaseTypeInfo = lhsTypeInfo
                .Traverse(t => t.BaseType?.GetTypeInfo())
                .Concat(lhsTypeInfo.ImplementedInterfaces.Select(t => t.GetTypeInfo()))
                .FirstOrDefault(t => GetEquatableTypeInfo(t).Equals(rhsEquatableTypeInfo));
            if (lhsBaseTypeInfo != null)
            {
                // Calculate T --> T2 relation result
                var rhsFixedType = (rhsTypeInfo.IsGenericTypeDefinition)
                    ? context.FromType(rhsTypeInfo.MakeGenericType(GetGenericArguments(lhsBaseTypeInfo)).GetTypeInfo())
                    : rhsType;

                if (lhsTypeInfo.IsGenericTypeDefinition)
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

                    // ImplementedClassType1<T>   ---+--- IInterfaceType<T2>
                    //            vvvvvvvvv
                    // ImplementedClassType1<T>   ---+                       [Widen]

                    // ImplementedClassType1<T>   ---+--- IInterfaceType<int>
                    //            vvvvvvvvv
                    // ImplementedClassType1<int> ---+                           [Widen: int]

                    // ImplementedClassType4<T, U>   ---+--- IInterfaceType<T2>
                    //            vvvvvvvvv
                    // ImplementedClassType4<T, U>   ---+                       [Widen]

                    var argumentTypeMap = GetGenericArguments(lhsBaseTypeInfo)
                        .Zip(GetGenericArguments(rhsTypeInfo),
                            (lhsArgument, rhsArgument) => new { lhsArgument, rhsArgument })
                        .Where(entry => entry.rhsArgument.IsGenericParameter == false)
                        .ToDictionary(entry => entry.lhsArgument, entry => entry.rhsArgument);
                    var lhsMappedArguments = GetGenericArguments(lhsTypeInfo)
                        .Select(t => argumentTypeMap.TryGetValue(t, out var r) ? r : t)
                        .ToArray();
                    var lhsFixedTypeInfo = lhsTypeInfo.MakeGenericType(lhsMappedArguments).GetTypeInfo();

                    return new NespCalculateCombinedResult(
                        lhsType, rhsFixedType, context.FromType(lhsFixedTypeInfo));
                }
                else
                {
                    // DerivedClassType2  ---+--- BaseClassType<T>
                    //            vvvvvvvvv
                    // DerivedClassType2  ---+                       [Widen: int]

                    // ImplementedClassType2   ---+--- IInterfaceType<T>
                    //            vvvvvvvvv
                    // ImplementedClassType2   ---+                       [Widen: int]

                    return new NespCalculateCombinedResult(lhsType, rhsFixedType, lhsType);
                }
            }

            var lhsEquatableTypeInfo = GetEquatableTypeInfo(lhsTypeInfo);
            var rhsBaseTypeInfo = rhsTypeInfo
                .Traverse(t => t.BaseType?.GetTypeInfo())
                .Concat(rhsTypeInfo.ImplementedInterfaces.Select(t => t.GetTypeInfo()))
                .FirstOrDefault(t => lhsEquatableTypeInfo.Equals(GetEquatableTypeInfo(t)));
            if (rhsBaseTypeInfo != null)
            {
                // Calculate T --> T2 relation result
                var lhsFixedType = (lhsTypeInfo.IsGenericTypeDefinition)
                    ? context.FromType(lhsTypeInfo.MakeGenericType(GetGenericArguments(rhsBaseTypeInfo)).GetTypeInfo())
                    : lhsType;

                if (rhsTypeInfo.IsGenericTypeDefinition)
                {
                    // BaseClassType<T2>    ---+--- DerivedClassType1<T>
                    //            vvvvvvvvv
                    //                         +--- DerivedClassType1<T> [Widen]

                    // BaseClassType<int>    ---+--- DerivedClassType1<T>
                    //            vvvvvvvvv
                    //                          +--- DerivedClassType1<int>  [Widen: int]

                    // BaseClassType<T2>    ---+--- DerivedClassType4<T, U>
                    //            vvvvvvvvv
                    //                         +--- DerivedClassType4<T, U>  [Widen]

                    // BaseClassType<T2>  ---+--- DerivedClassType5<T>
                    //            vvvvvvvvv
                    //                       +--- DerivedClassType5<T>  [Widen: int]

                    // IInterfaceType<T2>    ---+--- ImplementedClassType1<T>
                    //            vvvvvvvvv
                    //                          +--- ImplementedClassType1<T> [Widen]

                    // IInterfaceType<int>    ---+--- ImplementedClassType1<T>
                    //            vvvvvvvvv
                    //                           +--- ImplementedClassType1<int> [Widen: int]

                    // IInterfaceType<T2>    ---+--- ImplementedClassType4<T, U>
                    //            vvvvvvvvv
                    //                          +--- ImplementedClassType4<T, U> [Widen]

                    var argumentTypeMap = GetGenericArguments(rhsBaseTypeInfo)
                        .Zip(GetGenericArguments(lhsTypeInfo),
                            (rhsArgument, lhsArgument) => new { rhsArgument, lhsArgument })
                        .Where(entry => entry.lhsArgument.IsGenericParameter == false)
                        .ToDictionary(entry => entry.rhsArgument, entry => entry.lhsArgument);
                    var rhsMappedArguments = GetGenericArguments(rhsTypeInfo)
                        .Select(t => argumentTypeMap.TryGetValue(t, out var r) ? r : t)
                        .ToArray();
                    var rshFixedTypeInfo = rhsTypeInfo.MakeGenericType(rhsMappedArguments).GetTypeInfo();

                    return new NespCalculateCombinedResult(
                        lhsFixedType, rhsType, context.FromType(rshFixedTypeInfo));
                }
                else
                {
                    // BaseClassType<T>  ---+--- DerivedClassType2
                    //            vvvvvvvvv
                    //                      +--- DerivedClassType2   [Widen: int]

                    // IInterfaceType<T>    ---+--- ImplementedClassType2
                    //            vvvvvvvvv
                    //                         +--- ImplementedClassType2 [Widen: int]

                    return new NespCalculateCombinedResult(lhsFixedType, rhsType, rhsType);
                }
            }

            // BaseX ------+-- int
            //         vvvvvvvvv
            //             +-- int
            //             +-- BaseX [Combined: BaseX]
            return new NespCalculateCombinedResult(lhsType, rhsType, 
                context.GetOrAddPolymorphicType(new[] { lhsType, rhsType }.OrderBy(t => t)));
        }

        internal override NespCalculateCombinedResult CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation rhsType)
        {
            /////////////////////////////////////////////
            // type is runtime (Both runtime type)

            var rt = rhsType as NespRuntimeTypeInformation;
            if (rt != null)
            {
                return CalculateCombinedRuntimeTypes(context, this, rt);
            }

            /////////////////////////////////////////////
            // type is polymorphic (runtime vs polymorphic)

            var pt = (NespPolymorphicTypeInformation)rhsType;
            if (pt.types.Any(t => typeInfo.IsAssignableFrom(t.typeInfo)))
            {
                // BaseX ------+-- int
                //             +-- DerivedY
                //         vvvvvvvvv
                //             +-- int
                //             +-- DerivedY [Widen: BaseX]
                return new NespCalculateCombinedResult(this, rhsType, rhsType);
            }

            var widen = pt.types
                .Select(t => t.typeInfo.IsAssignableFrom(typeInfo) ? this : t)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            if (widen.SequenceEqual(pt.types) == false)
            {
                // DerivedY ------+-- int
                //                +-- BaseX
                //            vvvvvvvvv
                //                +-- int
                //                +-- DerivedY [Widen: BaseX]
                return new NespCalculateCombinedResult(
                    this, rhsType, context.GetOrAddPolymorphicType(widen));
            }
            else
            {
                // BaseX ------+-- int
                //             +-- string
                //         vvvvvvvvv
                //             +-- int
                //             +-- string
                //             +-- BaseX  [Combined: BaseX]
                return new NespCalculateCombinedResult(
                    this, rhsType, context.GetOrAddPolymorphicType(widen.Concat(new[] { this }).OrderBy(t => t)));
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
        private static readonly NespRuntimeTypeInformation[] empty = new NespRuntimeTypeInformation[0];

        internal readonly NespRuntimeTypeInformation[] types;

        internal NespPolymorphicTypeInformation(string name, NespRuntimeTypeInformation[] types)
        {
            this.Name = name;
            this.types = types;
        }

        public override string FullName => "'" + this.Name;
        public override string Name { get; }

        public NespRuntimeTypeInformation[] RuntimeTypes => types;

        internal override NespCalculateCombinedResult CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation rhsType)
        {
            /////////////////////////////////////////////
            // type is runtime (polymorphic vs runtime)

            var pt = rhsType as NespPolymorphicTypeInformation;
            if (pt == null)
            {
                // swap lhs and rhs
                var result = rhsType.CalculateCombinedTypeWith(context, this);
                return new NespCalculateCombinedResult(
                    result.RightFixed, result.LeftFixed, result.Combined);
            }

            /////////////////////////////////////////////
            // type is polymorphic (both polymorphic type)

            // TODO: Reduce calculation costs
            var assignableWiden = types
                .SelectMany(t1 => pt.types
                    .SelectMany(t2 => t1.typeInfo.IsAssignableFrom(t2.typeInfo)
                        ? new[] { t2 }
                        : t2.typeInfo.IsAssignableFrom(t1.typeInfo)
                            ? new [] { t1 }
                            : empty));
            var combinedUnassignablesLeft = types
                .SelectMany(t1 => pt.types
                    .All(t2 => !t1.typeInfo.IsAssignableFrom(t2.typeInfo) && !t2.typeInfo.IsAssignableFrom(t1.typeInfo))
                        ? new[] { t1 }
                        : empty);
            var combinedUnassignablesRight = pt.types
                .SelectMany(t2 => types
                    .All(t1 => !t1.typeInfo.IsAssignableFrom(t2.typeInfo) && !t2.typeInfo.IsAssignableFrom(t1.typeInfo))
                        ? new[] { t2 }
                        : empty);
            var combined = assignableWiden
                .Concat(combinedUnassignablesLeft)
                .Concat(combinedUnassignablesRight)
                .Distinct()
                .OrderBy(t => t)
                .ToArray();
            if (combined.SequenceEqual(types) == false)
            {
                // IE<int>   --+---+-- BaseX
                // DerivedY  --+   +-- int[]
                // string    --+
                //           vvvvvvvvv
                // int[]     --+                 [Widen: IE<int>]
                // DerivedY  --+                 [Widen: BaseX]
                // string    --+                 [Combined: string]
                return new NespCalculateCombinedResult(
                    this, rhsType, context.GetOrAddPolymorphicType(combined));
            }

            // int[]    --+---+-- BaseX
            // DerivedY --+   +-- int[]
            //           vvvvvvvvv
            // int[]    --+
            // DerivedY --+
            return new NespCalculateCombinedResult(this, rhsType, this);
        }

        public override string ToString()
        {
            var t = string.Join(",", this.types.Select(rt => rt.ToString()));
            return $"{this.FullName} [{t}]";
        }

        public override int GetHashCode()
        {
            return types.Aggregate(
                this.FullName.GetHashCode(),
                (last, type) => last ^ type.GetHashCode());
        }
    }

    public sealed class NespMetadataContext
    {
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

        public NespCalculateCombinedResult CalculateCombinedType(
            NespTypeInformation lhsType, NespTypeInformation rhsType)
        {
            return lhsType.CalculateCombinedTypeWith(this, rhsType);
        }
    }
}
