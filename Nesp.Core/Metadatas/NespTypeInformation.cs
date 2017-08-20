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
using System.Linq;
using System.Reflection;

using Nesp.Internals;

#pragma warning disable 659
#pragma warning disable 661

namespace Nesp.Metadatas
{
    public sealed class NespPolymorphicTypeConstraint
    {
        internal NespPolymorphicTypeConstraint(
            bool isValueType, bool isReference, bool isDefaultConstractor, NespTypeInformation[] constraintTypes)
        {
            this.IsValueType = isValueType;
            this.IsReference = isReference;
            this.IsDefaultConstractor = isDefaultConstractor;
            this.ConstraintTypes = constraintTypes;
        }

        public bool IsValueType { get; }

        public bool IsReference { get; }

        public bool IsDefaultConstractor { get; }

        public NespTypeInformation[] ConstraintTypes { get; }
    }

    public abstract class NespTypeInformation
    {
        private static readonly NespTypeInformation objectType = NespMetadataContext.UnsafeFromType<object>();

        internal NespTypeInformation()
        {
        }

        public abstract string FullName { get; }

        public abstract string Name { get; }

        public abstract bool IsPrimitiveType { get; }

        public abstract bool IsBasicType { get; }

        public abstract bool IsInterfaceType { get; }

        public abstract bool IsEnumType { get; }

        public abstract NespTypeInformation GetBaseType(NespMetadataContext context);

        public abstract NespTypeInformation[] GetBaseInterfaces(NespMetadataContext context);

        public abstract bool IsPolymorphicType { get; }

        public abstract NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context);

        public abstract NespPolymorphicTypeConstraint[] GetPolymorphicTypeConstraints(NespMetadataContext context);

        public abstract bool IsAssignableFrom(NespTypeInformation type);

        public NespTypeInformation CalculateNarrowing(NespTypeInformation targetType, NespMetadataContext context)
        {
            // Shortcut calculation by hard-coded assignable decision (Low cost)
            if (this.IsAssignableFrom(targetType))
            {
                return targetType;
            }
            if (targetType.IsAssignableFrom(this))
            {
                return this;
            }

            // Turn to polymorphic type
            return new NespPolymorphicTypeInformation(context.CreatePolymorphicName(), this, targetType);
        }

        public abstract bool Equals(NespTypeInformation obj);

        public override bool Equals(object obj)
        {
            return this.Equals(obj as NespTypeInformation);
        }

        private static bool Equals(NespTypeInformation lhs, NespTypeInformation rhs)
        {
            if (object.ReferenceEquals(lhs, rhs))
            {
                return true;
            }
            return lhs?.Equals(rhs) ?? false;
        }

        public static bool operator ==(NespTypeInformation lhs, NespTypeInformation rhs)
        {
            return Equals(lhs, rhs);
        }

        public static bool operator !=(NespTypeInformation lhs, NespTypeInformation rhs)
        {
            return !Equals(lhs, rhs);
        }
    }

    public sealed class NespRuntimeTypeInformation : NespTypeInformation
    {
        private static readonly TypeInfo stringTypeInfo = typeof(string).GetTypeInfo();
        private static readonly TypeInfo decimalTypeInfo = typeof(decimal).GetTypeInfo();
        private static readonly NespPolymorphicTypeConstraint[] emptyConstraints = new NespPolymorphicTypeConstraint[0];

        private readonly TypeInfo typeInfo;

        internal NespRuntimeTypeInformation(TypeInfo typeInfo)
        {
            this.typeInfo = typeInfo;
        }

        public override string FullName => NespUtilities.GetReadableTypeName(typeInfo);

        public override string Name => this.FullName.Split('.').Last();

        public override bool IsPrimitiveType => typeInfo.IsPrimitive;

        public override bool IsBasicType => typeInfo.IsPrimitive
            || (typeInfo.Equals(stringTypeInfo))
            || (typeInfo.Equals(decimalTypeInfo));

        public override bool IsInterfaceType => typeInfo.IsInterface;

        public override bool IsEnumType => typeInfo.IsEnum;

        public override NespTypeInformation GetBaseType(NespMetadataContext context)
        {
            var baseType = typeInfo.BaseType;
            var baseTypeInfo = baseType?.GetTypeInfo();
            return (baseTypeInfo != null) ? context.FromTypeInfo(baseTypeInfo) : null;
        }

        public override NespTypeInformation[] GetBaseInterfaces(NespMetadataContext context)
        {
            // TODO: cache?

            var interfaces = typeInfo.ImplementedInterfaces.ToArray();

            // The ImplementedInterfaces property returns all implemented interface types.
            // This function returns only directly implemented interface types.

            // A         <--+
            // |            |
            // B            | Remove from candidates
            // |-----+      |
            // C     D   <--+
            // |--+  |
            // E  F  G   <--- Results

            var candidates = new HashSet<Type>(interfaces);
            foreach (var interfaceType in interfaces)
            {
                foreach (var baseInterfaceType in interfaceType.GetTypeInfo().ImplementedInterfaces)
                {
                    candidates.Remove(baseInterfaceType);
                }
            }

            return candidates
                .Select(type => context.FromTypeInfo(type.GetTypeInfo()))
                .ToArray();
        }

        public override bool IsPolymorphicType => typeInfo.IsGenericType;

        public override NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context)
        {
            return typeInfo.GenericTypeParameters
                .Select(type => context.FromTypeInfo(type.GetTypeInfo()))
                .ToArray();
        }

        public override NespPolymorphicTypeConstraint[] GetPolymorphicTypeConstraints(NespMetadataContext context)
        {
            return typeInfo.IsGenericParameter
                ? new[]
                {
                    new NespPolymorphicTypeConstraint(
                        (typeInfo.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) ==
                            GenericParameterAttributes.NotNullableValueTypeConstraint,
                        (typeInfo.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) ==
                            GenericParameterAttributes.ReferenceTypeConstraint,
                        (typeInfo.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) ==
                            GenericParameterAttributes.DefaultConstructorConstraint,
                        typeInfo.GetGenericParameterConstraints()
                            .Select(type => context.FromTypeInfo(type.GetTypeInfo()))
                            .ToArray())
                }
                : emptyConstraints;
        }

        public override bool IsAssignableFrom(NespTypeInformation type)
        {
            var rhsRuntimeType = type as NespRuntimeTypeInformation;
            if (rhsRuntimeType != null)
            {
                return typeInfo.IsAssignableFrom(rhsRuntimeType.typeInfo);
            }
            else
            {
                // TODO: Implement assignable
                return false;
            }
        }

        public bool Equals(NespRuntimeTypeInformation rhs)
        {
            if (object.ReferenceEquals(this, rhs))
            {
                return true;
            }
            return rhs?.typeInfo.Equals(typeInfo) ?? false;
        }

        public override bool Equals(NespTypeInformation obj)
        {
            return this.Equals(obj as NespRuntimeTypeInformation);
        }

        public override int GetHashCode()
        {
            return typeInfo.GetHashCode();
        }

        public override string ToString()
        {
            return NespUtilities.GetReservedReadableTypeName(typeInfo);
        }
    }

    public sealed class NespPolymorphicTypeInformation : NespTypeInformation
    {
        private readonly NespRuntimeTypeInformation[] constraintTypes;

        internal NespPolymorphicTypeInformation(string name, params NespTypeInformation[] targetTypes)
        {
            this.Name = name;
            constraintTypes = targetTypes
                .SelectMany(type =>
                {
                    var pt = type as NespPolymorphicTypeInformation;
                    return pt?.constraintTypes ?? new[] { (NespRuntimeTypeInformation)type };
                })
                .ToArray();
        }

        public override string FullName => $"'{this.Name}";

        public override string Name { get; }

        public override bool IsPrimitiveType => constraintTypes.All(type => type.IsPrimitiveType);

        public override bool IsBasicType => constraintTypes.All(type => type.IsBasicType);

        public override bool IsInterfaceType => constraintTypes.All(type => type.IsInterfaceType);

        public override bool IsEnumType => constraintTypes.All(type => type.IsEnumType);

        public override NespTypeInformation GetBaseType(NespMetadataContext context)
        {
            return constraintTypes
                .Cast<NespTypeInformation>()
                .Aggregate((type0, type1) => type0.CalculateNarrowing(type1, context));
        }

        public override NespTypeInformation[] GetBaseInterfaces(NespMetadataContext context)
        {
            return null;
            //return constraintTypes
            //    .Select(type => type.GetBaseInterfaces(context))
            //    .Aggregate((type0, type1) => type0.CalculateNarrowing(type1, context));
        }

        public override bool IsPolymorphicType => true;

        public override NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context)
        {
            return null;
        }

        public override NespPolymorphicTypeConstraint[] GetPolymorphicTypeConstraints(NespMetadataContext context)
        {
            return null;
        }

        public override bool IsAssignableFrom(NespTypeInformation type)
        {
            // TODO: Implement assignable
            return false;
        }

        public bool Equals(NespPolymorphicTypeInformation rhs)
        {
            if (object.ReferenceEquals(this, rhs))
            {
                return true;
            }
            // TODO: Implement assignable
            return false;
        }

        public override bool Equals(NespTypeInformation obj)
        {
            return this.Equals(obj as NespPolymorphicTypeInformation);
        }

        public override string ToString()
        {
            return this.FullName;
        }
    }
}
