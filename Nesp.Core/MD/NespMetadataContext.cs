using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.MD
{
    public abstract class NespTypeInformation
        : IEquatable<NespTypeInformation>, IComparable<NespTypeInformation>
    {
        internal NespTypeInformation()
        {
        }

        public abstract string FullName { get; }
        public abstract string Name { get; }

        internal abstract NespTypeInformation CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation type);

        internal abstract bool IsAssignableFrom(NespTypeInformation type);

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

        internal override NespTypeInformation CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation type)
        {
            /////////////////////////////////////////////
            // type is runtime (Both runtime type)

            var rt = type as NespRuntimeTypeInformation;
            if (rt != null)
            {
                if (typeInfo.IsAssignableFrom(rt.typeInfo))
                {
                    // BaseX ------+-- DerivedY
                    //            vvvvvvvvv
                    //             +-- DerivedY [Widen: BaseX]
                    return rt;
                }
                if (rt.typeInfo.IsAssignableFrom(typeInfo))
                {
                    // DerivedY ------+-- BaseX
                    //            vvvvvvvvv
                    // DerivedY ------+         [Widen: BaseX]
                    return this;
                }

                // BaseX ------+-- int
                //         vvvvvvvvv
                //             +-- int
                //             +-- BaseX [Combined: BaseX]
                return context.GetOrAddPolymorphicType(new [] { this, rt });
            }

            /////////////////////////////////////////////
            // type is polymorphic (runtime vs polymorphic)

            var pt = (NespPolymorphicTypeInformation)type;
            if (pt.types.Any(t => typeInfo.IsAssignableFrom(t.typeInfo)))
            {
                // BaseX ------+-- int
                //             +-- DerivedY
                //         vvvvvvvvv
                //             +-- int
                //             +-- DerivedY [Widen: BaseX]
                return type;
            }

            var widen = pt.types
                .Select(t => t.typeInfo.IsAssignableFrom(typeInfo) ? this : t)
                .Distinct()
                .ToArray();
            if (widen.SequenceEqual(pt.types) == false)
            {
                // DerivedY ------+-- int
                //                +-- BaseX
                //            vvvvvvvvv
                //                +-- int
                //                +-- DerivedY [Widen: BaseX]
                return context.GetOrAddPolymorphicType(widen);
            }
            else
            {
                // BaseX ------+-- int
                //             +-- string
                //         vvvvvvvvv
                //             +-- int
                //             +-- string
                //             +-- BaseX  [Combined: BaseX]
                return context.GetOrAddPolymorphicType(widen.Concat(new[] { this }));
            }
        }

        internal override bool IsAssignableFrom(NespTypeInformation type)
        {
            var runtimeType = type as NespRuntimeTypeInformation;
            if (runtimeType != null)
            {
                return typeInfo.IsAssignableFrom(runtimeType.typeInfo);
            }

            var polymorphicType = (NespPolymorphicTypeInformation)type;
            if (polymorphicType.types.All(rt => typeInfo.IsAssignableFrom(rt.typeInfo)))
            {
                return true;
            }

            return false;
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
        internal readonly NespRuntimeTypeInformation[] types;

        internal NespPolymorphicTypeInformation(string name, NespRuntimeTypeInformation[] types)
        {
            this.Name = name;
            this.types = types;
        }

        public override string FullName => "'" + this.Name;
        public override string Name { get; }

        public NespRuntimeTypeInformation[] RuntimeTypes => types;

        internal override NespTypeInformation CalculateCombinedTypeWith(
            NespMetadataContext context, NespTypeInformation type)
        {
            /////////////////////////////////////////////
            // type is runtime (polymorphic vs runtime)

            var pt = type as NespPolymorphicTypeInformation;
            if (pt == null)
            {
                // swap lhs and rhs
                return type.CalculateCombinedTypeWith(context, this);
            }

            /////////////////////////////////////////////
            // type is polymorphic (both polymorphic type)

            var widen1 = types
                .Select(t1 => t1.CalculateCombinedTypeWith(context, pt));
            var widen2 = pt.types
                .Select(t2 => t2.CalculateCombinedTypeWith(context, this));
            var combined = widen1
                .Concat(widen2)
                .Distinct()     // LINQ to Object's distinct is stable.
                .ToArray();
            if (combined.SequenceEqual(types) == false)
            {
                // TODO:
                // IE<int>   --+---+-- DerivedY
                // BaseX     --+   +-- int[]
                //           vvvvvvvvv
                // int[]     --+                 [Widen: IE<int>]
                // DerivedY  --+                 [Widen: BaseX]

                // Construct new pt
                return context.GetOrAddPolymorphicType(
                    combined.SelectMany(t =>
                    {
                        var r = t as NespRuntimeTypeInformation;
                        return (r != null) ? new[] {r} : ((NespPolymorphicTypeInformation)t).types;
                    })
                    .Distinct());
            }

            return this;
        }

        internal override bool IsAssignableFrom(NespTypeInformation type)
        {
            return types.Any(t => t.IsAssignableFrom(type));
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
            var normalized = types.OrderBy(type => type).ToArray();

            lock (polymorphicTypes)
            {
                if (polymorphicTypes.TryGetValue(normalized, out var type) == false)
                {
                    var name = this.GeneratePolymorphicTypeName();
                    type = new NespPolymorphicTypeInformation(name, normalized);
                    polymorphicTypes.Add(normalized, type);
                }
                return type;
            }
        }

        public NespTypeInformation CalculateCombinedType(NespTypeInformation type1, NespTypeInformation type2)
        {
            return type1.CalculateCombinedTypeWith(this, type2);
        }

        public bool IsAssignableType(NespTypeInformation toType, NespTypeInformation fromType)
        {
            return toType.IsAssignableFrom(fromType);
        }
    }
}
