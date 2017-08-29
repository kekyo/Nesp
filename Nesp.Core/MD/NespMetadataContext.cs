using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.MD
{
    public abstract class NespTypeInformation : IEquatable<NespTypeInformation>
    {
        internal NespTypeInformation()
        {
        }

        public abstract string FullName { get; }
        public abstract string Name { get; }

        internal abstract NespTypeInformation CalculateCombinedTypeWith(NespMetadataContext context, NespTypeInformation type);

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

        internal override NespTypeInformation CalculateCombinedTypeWith(NespMetadataContext context, NespTypeInformation type)
        {
            /////////////////////////////////////////////
            // type is runtime (Both runtime type)

            var rt = type as NespRuntimeTypeInformation;
            if (rt != null)
            {
                if (typeInfo.IsAssignableFrom(rt.typeInfo))
                {
                    // MethodBase ------+-- MethodInfo
                    //            vvvvvvvvv
                    //                  +-- MethodInfo [Widen: MethodBase]
                    return rt;
                }
                if (rt.typeInfo.IsAssignableFrom(typeInfo))
                {
                    // MethodInfo ------+-- MethodBase
                    //            vvvvvvvvv
                    // MethodInfo ------+              [Widen: MethodBase]
                    return this;
                }

                // MethodBase ------+-- int
                //            vvvvvvvvv
                //                  +-- int
                //                  +-- MethodBase [Combined: MethodBase]
                var name = context.GeneratePolymorphicTypeName();
                return new NespPolymorphicTypeInformation(name, this, rt);
            }

            /////////////////////////////////////////////
            // type is polymorphic (runtime vs polymorphic)

            var pt = (NespPolymorphicTypeInformation)type;
            if (pt.types.Any(t => typeInfo.IsAssignableFrom(t.typeInfo)))
            {
                // MethodBase ------+-- int
                //                  +-- MethodInfo
                //            vvvvvvvvv
                //                  +-- int
                //                  +-- MethodInfo [Widen: MethodBase]
                return type;
            }

            var widen = pt.types
                .Select(t => t.typeInfo.IsAssignableFrom(typeInfo) ? this : t)
                .Distinct()
                .ToArray();
            if (widen.SequenceEqual(pt.types) == false)
            {
                // MethodInfo ------+-- int
                //                  +-- MethodBase
                //            vvvvvvvvv
                //                  +-- int
                //                  +-- MethodInfo [Widen: MethodBase]

                // Construct new pt
                var name = context.GeneratePolymorphicTypeName();
                return new NespPolymorphicTypeInformation(name, widen);
            }
            else
            {
                // MethodBase ------+-- int
                //                  +-- MethodBase
                //            vvvvvvvvv
                //                  +-- int
                //                  +-- MethodBase

                // Nothing change
                return pt;
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
    }

    public sealed class NespPolymorphicTypeInformation : NespTypeInformation
    {
        internal readonly NespRuntimeTypeInformation[] types;

        internal NespPolymorphicTypeInformation(string name, params NespRuntimeTypeInformation[] types)
        {
            this.Name = name;
            this.types = types;
        }

        public override string FullName => "'" + this.Name;
        public override string Name { get; }

        public int Count => types.Length;

        internal override NespTypeInformation CalculateCombinedTypeWith(NespMetadataContext context, NespTypeInformation type)
        {
            /////////////////////////////////////////////
            // type is runtime (polymorphic vs runtime)

            var rt = type as NespRuntimeTypeInformation;
            if (rt != null)
            {
                if (types.Any(t => rt.typeInfo.IsAssignableFrom(t.typeInfo)))
                {
                    // int        --+------ MethodBase
                    // MethodInfo --+
                    //            vvvvvvvvv
                    // int        --+
                    // MethodInfo --+                  [Widen: MethodBase]
                    return this;
                }

                var widen = types
                    .Select(t => t.typeInfo.IsAssignableFrom(rt.typeInfo) ? rt : t)
                    .Distinct()
                    .ToArray();
                if (widen.SequenceEqual(types) == false)
                {
                    // int        --+------ MethodInfo
                    // MethodBase --+
                    //            vvvvvvvvv
                    // int        --+
                    // MethodInfo --+                  [Widen: MethodBase]

                    var name = context.GeneratePolymorphicTypeName();
                    return new NespPolymorphicTypeInformation(name, widen);
                }
                else
                {
                    // int        --+------ string
                    // MethodBase --+
                    //            vvvvvvvvv
                    // int        --+
                    // MethodBase --+
                    // string     --+              [Combined: string]

                    var name = context.GeneratePolymorphicTypeName();
                    return new NespPolymorphicTypeInformation(name, widen.Concat(new[] { rt }).ToArray());
                }
            }

            /////////////////////////////////////////////
            // type is polymorphic (both polymorphic type)

            var pt = (NespPolymorphicTypeInformation)type;
            var widen1 = types
                .Select(t1 => t1.CalculateCombinedTypeWith(context, pt));
            var widen2 = pt.types
                .Select(t2 => t2.CalculateCombinedTypeWith(context, this));
            var combined = widen1
                .Concat(widen2)
                .Distinct()
                .ToArray();
            if (combined.SequenceEqual(types) == false)
            {
                // TODO:
                // IE<int>    --+---+-- MethodInfo
                // MethodBase --+   +-- int[]
                //            vvvvvvvvv
                // int[]      --+                  [Widen: IE<int>]
                // MethodInfo --+                  [Widen: MethodBase]

                // Construct new pt
                var name = context.GeneratePolymorphicTypeName();
                return new NespPolymorphicTypeInformation(
                    name,
                    combined.SelectMany(t =>
                    {
                        var r = t as NespRuntimeTypeInformation;
                        return (r != null) ? new[] {r} : ((NespPolymorphicTypeInformation)t).types;
                    })
                    .Distinct()
                    .ToArray());
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
    }

    public sealed class NespMetadataContext
    {
        private readonly Dictionary<TypeInfo, NespTypeInformation> types =
            new Dictionary<TypeInfo, NespTypeInformation>();

        private int polymorphicTypeNameIndex = 0;

        public NespTypeInformation TypeFrom(TypeInfo typeInfo)
        {
            lock (types)
            {
                if (types.TryGetValue(typeInfo, out var type) == false)
                {
                    type = new NespRuntimeTypeInformation(typeInfo);
                    types.Add(typeInfo, type);
                }
                return type;
            }
        }

        internal string GeneratePolymorphicTypeName()
        {
            var index = ++polymorphicTypeNameIndex;
            return "T" + index;
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
