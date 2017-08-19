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

        private NespTypeInformation InternalGetBaseType(NespMetadataContext context)
        {
            // Exclude object type
            var type = this.GetBaseType(context);
            return (type != objectType) ? type : null;
        }

        private static IEnumerable<NespTypeInformation[]> CrawlBaseInterfaceHierarchies(
            NespTypeInformation targetType, List<NespTypeInformation> hierarchyList, NespMetadataContext context)
        {
            // Add type to hierarchy list if type is interface.
            if (targetType.IsInterfaceType)
            {
                hierarchyList.Add(targetType);
            }

            // If available base interface types, traverse by recursivity.
            var interfaceTypes = targetType.GetBaseInterfaces(context);
            switch (interfaceTypes.Length)
            {
                case 0:
                    // Bottom of base interface.
                    // Base to derives
                    hierarchyList.Reverse();
                    return new[] { hierarchyList.ToArray() };

                case 1:
                    // Only a child interface type
                    // (Suppress list copy cost)
                    return CrawlBaseInterfaceHierarchies(interfaceTypes[0], hierarchyList, context);

                default:
                    // More children interface types
                    return interfaceTypes
                        .SelectMany(interfaceType =>
                        {
                            // Copy current hierarchy list.
                            var currentList = hierarchyList.ToList();

                            // 
                            return CrawlBaseInterfaceHierarchies(interfaceType, currentList, context);
                        })
                        .ToArray();
            }
        }

        public NespTypeInformation CalculateNarrowing(NespTypeInformation targetType, NespMetadataContext context)
        {
            /////////////////////////////////////////////////////
            // Step 1: Shortcut calculation by hard-coded assignable decision (Low cost)

            if (this.IsAssignableFrom(targetType))
            {
                return targetType;
            }
            if (targetType.IsAssignableFrom(this))
            {
                return this;
            }

            /////////////////////////////////////////////////////
            // Step 2: Calculate narrowing by class hierarchy

            var thisDeriveTypes = this.Traverse(type => type.InternalGetBaseType(context)).Reverse();
            var targetDeriveTypes = targetType.Traverse(type => type.InternalGetBaseType(context)).Reverse();

            // Get most deep derived class type
            var narrowDeriveTypeEntry = thisDeriveTypes
                .Zip(targetDeriveTypes, (thisDeriveType, targetDerive) => new { thisDeriveType, targetDerive })
                .LastOrDefault(entry => entry.thisDeriveType == entry.targetDerive);

            if (narrowDeriveTypeEntry != null)
            {
                // Commonly used class type found (Exclude object type)
                return narrowDeriveTypeEntry.thisDeriveType;
            }

            /////////////////////////////////////////////////////
            // Step 3: Calculate narrowing by interface hierarchy

            var thisInterfaceTypeSets = CrawlBaseInterfaceHierarchies(this, new List<NespTypeInformation>(), context);
            var targetInterfaceTypeSets = CrawlBaseInterfaceHierarchies(targetType, new List<NespTypeInformation>(), context);

            // Get most deep derived interface type
            var narrowInterfaceType =
                (from thisInterfaceTypes in thisInterfaceTypeSets
                 from targetInterfaceTypes in targetInterfaceTypeSets
                 let zipped = thisInterfaceTypes
                     .Zip(targetInterfaceTypes,
                         (thisInterfaceType, targetInterfaceType) => new {thisInterfaceType, targetInterfaceType})
                     .Where(entry => entry.thisInterfaceType == entry.targetInterfaceType)
                     .ToArray()
                 where zipped.Length >= 1
                 let last = zipped.Last()
                 orderby
                    last.thisInterfaceType.IsPolymorphicType ? 1 : 0 descending,
                    zipped.Length descending
                 select last.thisInterfaceType)
                .FirstOrDefault();

            // If final result not found, apply wildcard (object type)
            return narrowInterfaceType ?? objectType;
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
            return (baseTypeInfo != null) ? context.FromType(baseTypeInfo) : null;
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
                .Select(type => context.FromType(type.GetTypeInfo()))
                .ToArray();
        }

        public override bool IsPolymorphicType => typeInfo.IsGenericType;

        public override NespTypeInformation[] GetPolymorphicParameters(NespMetadataContext context)
        {
            return typeInfo.GenericTypeParameters
                .Select(type => context.FromType(type.GetTypeInfo()))
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
                            .Select(type => context.FromType(type.GetTypeInfo()))
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
        internal NespPolymorphicTypeInformation(string name)
        {
            this.Name = name;
        }

        public override string FullName => $"'{this.Name}";

        public override string Name { get; }

        public override bool IsPrimitiveType => false;

        public override bool IsBasicType => false;

        public override bool IsInterfaceType => false;

        public override bool IsEnumType => false; // TODO: Apply constraints

        public override NespTypeInformation GetBaseType(NespMetadataContext context)
        {
            // TODO:
            return null;
        }

        public override NespTypeInformation[] GetBaseInterfaces(NespMetadataContext context)
        {
            // TODO:
            return null;
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
