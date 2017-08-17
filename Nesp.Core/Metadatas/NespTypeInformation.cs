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

using System.Linq;
using System.Reflection;

using Nesp.Internals;

#pragma warning disable 659
#pragma warning disable 661

namespace Nesp.Metadatas
{
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

        public abstract bool IsEnumType { get; }

        public abstract NespTypeInformation GetBaseType(NespMetadataContext context);

        public abstract NespTypeInformation[] GetInterfaces(NespMetadataContext context);

        public abstract bool IsGenericType { get; }

        public abstract NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context);

        public abstract bool IsValueTypeConstraint { get; }

        public abstract bool IsReferenceConstraint { get; }

        public abstract bool IsDefaultConstractorConstraint { get; }

        public abstract NespTypeInformation[] GetPolymorphicParameterConstraints(NespMetadataContext context);

        public abstract bool IsAssignableFrom(NespTypeInformation type);

        private NespTypeInformation InternalGetBaseType(NespMetadataContext context)
        {
            var type = this.GetBaseType(context);
            return (type != objectType) ? type : null;
        }

        public NespTypeInformation CalculateNarrowing(NespTypeInformation targetType, NespMetadataContext context)
        {
            if (this.IsAssignableFrom(targetType))
            {
                return targetType;
            }
            if (targetType.IsAssignableFrom(this))
            {
                return this;
            }

            var thisDeriveTypes = this
                .Traverse(type => type.InternalGetBaseType(context))
                .Reverse();
            var targetDeriveTypes = targetType
                .Traverse(type => type.InternalGetBaseType(context))
                .Reverse();
            var narrowTypes = thisDeriveTypes
                .Zip(targetDeriveTypes, (thisDeriveType, targetDerive) => new { thisDeriveType, targetDerive })
                .LastOrDefault(entry => entry.thisDeriveType == entry.targetDerive);

            if (narrowTypes != null)
            {
                return narrowTypes.thisDeriveType;
            }

            return objectType;
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

        private readonly TypeInfo typeInfo;

        internal NespRuntimeTypeInformation(TypeInfo typeInfo)
        {
            this.typeInfo = typeInfo;
        }

        public override string FullName => NespUtilities.GetReadableTypeName(typeInfo);

        public override string Name => this.FullName.Split('.').Last();

        public override bool IsPrimitiveType => typeInfo.IsPrimitive;

        public override bool IsBasicType => typeInfo.IsPrimitive
            || (typeInfo == stringTypeInfo)
            || (typeInfo == decimalTypeInfo);

        public override bool IsEnumType => typeInfo.IsEnum;

        public override NespTypeInformation GetBaseType(NespMetadataContext context)
        {
            var baseType = typeInfo.BaseType;
            var baseTypeInfo = baseType?.GetTypeInfo();
            return (baseTypeInfo != null) ? context.FromType(baseTypeInfo) : null;
        }

        public override NespTypeInformation[] GetInterfaces(NespMetadataContext context)
        {
            return typeInfo.ImplementedInterfaces
                .Select(type => context.FromType(type.GetTypeInfo()))
                .ToArray();
        }

        public override bool IsGenericType => typeInfo.IsGenericType;

        public override NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context)
        {
            return typeInfo.GenericTypeParameters
                .Select(type => context.FromType(type.GetTypeInfo()))
                .ToArray();
        }

        public override bool IsValueTypeConstraint =>
            (typeInfo.GenericParameterAttributes & GenericParameterAttributes.NotNullableValueTypeConstraint) == GenericParameterAttributes.NotNullableValueTypeConstraint;

        public override bool IsReferenceConstraint =>
            (typeInfo.GenericParameterAttributes & GenericParameterAttributes.ReferenceTypeConstraint) == GenericParameterAttributes.ReferenceTypeConstraint;

        public override bool IsDefaultConstractorConstraint =>
            (typeInfo.GenericParameterAttributes & GenericParameterAttributes.DefaultConstructorConstraint) == GenericParameterAttributes.DefaultConstructorConstraint;

        public override NespTypeInformation[] GetPolymorphicParameterConstraints(NespMetadataContext context)
        {
            return typeInfo.GetGenericParameterConstraints()
                .Select(type => context.FromType(type.GetTypeInfo()))
                .ToArray();
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
        internal NespPolymorphicTypeInformation(string name)
        {
            this.Name = name;
        }

        public override string FullName => $"'{this.Name}";

        public override string Name { get; }

        public override bool IsPrimitiveType => false;

        public override bool IsBasicType => false;

        public override bool IsEnumType => false; // TODO: Apply constraints

        public override NespTypeInformation GetBaseType(NespMetadataContext context)
        {
            // TODO:
            return null;
        }

        public override NespTypeInformation[] GetInterfaces(NespMetadataContext context)
        {
            // TODO:
            return null;
        }

        public override bool IsGenericType => true;

        public override NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context)
        {
            return null;
        }

        public override bool IsValueTypeConstraint => false;

        public override bool IsReferenceConstraint => false;

        public override bool IsDefaultConstractorConstraint => false;

        public override NespTypeInformation[] GetPolymorphicParameterConstraints(NespMetadataContext context)
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
