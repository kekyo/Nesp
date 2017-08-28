using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.MD
{
    public abstract class NespTypeInformation
    {
        internal NespTypeInformation()
        {
        }

        public abstract string FullName { get; }
        public abstract string Name { get; }

        internal abstract NespRuntimeTypeInformation[] NormalizedRuntimeTypes { get; }

        internal abstract bool IsAssignableFrom(NespTypeInformation type);

        public override string ToString()
        {
            return this.FullName;
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

        internal override NespRuntimeTypeInformation[] NormalizedRuntimeTypes => new[] {this};

        internal override bool IsAssignableFrom(NespTypeInformation type)
        {
            var runtimeType = type as NespRuntimeTypeInformation;
            if (runtimeType != null)
            {
                return typeInfo.IsAssignableFrom(runtimeType.typeInfo);
            }

            var polymorphicType = (NespPolymorphicTypeInformation)type;
            if (polymorphicType.NormalizedRuntimeTypes.All(rt => typeInfo.IsAssignableFrom(rt.typeInfo)))
            {
                return true;
            }

            return false;
        }
    }

    public sealed class NespPolymorphicTypeInformation : NespTypeInformation
    {
        internal NespPolymorphicTypeInformation(string name, NespRuntimeTypeInformation[] types)
        {
            this.Name = name;
            this.NormalizedRuntimeTypes = types;
        }

        public override string FullName => "'" + this.Name;
        public override string Name { get; }

        public int Count => this.NormalizedRuntimeTypes.Length;

        internal override NespRuntimeTypeInformation[] NormalizedRuntimeTypes { get; }

        internal override bool IsAssignableFrom(NespTypeInformation type)
        {
            return this.NormalizedRuntimeTypes.Any(t => t.IsAssignableFrom(type));
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
            var replacedIfAssignable = type1.NormalizedRuntimeTypes
                .Select(rt => rt.IsAssignableFrom(type2) ? type2 : rt)    // Widen [MethodBase <--- MethodInfo]
                .Distinct()
                .ToArray();

            if (replacedIfAssignable.Any(rt => type2.IsAssignableFrom(rt)))  // [MethodInfo <--- MethodBase]
            {
                if (type1.NormalizedRuntimeTypes.SequenceEqual(replacedIfAssignable))
                {
                    return type1;
                }
                else
                {
                    var name = this.GeneratePolymorphicTypeName();
                    return new NespPolymorphicTypeInformation(
                        name,
                        replacedIfAssignable.SelectMany(t => t.NormalizedRuntimeTypes).ToArray());
                }
            }
            else
            {
                var name = this.GeneratePolymorphicTypeName();
                return new NespPolymorphicTypeInformation(
                    name,
                    replacedIfAssignable
                        .SelectMany(t => t.NormalizedRuntimeTypes)
                        .Concat(type2.NormalizedRuntimeTypes)
                        .Distinct()
                        .ToArray());
            }
        }

        public bool IsAssignableType(NespTypeInformation toType, NespTypeInformation fromType)
        {
            return toType.IsAssignableFrom(fromType);
        }
    }
}
