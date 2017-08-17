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

using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.Metadatas
{
    public abstract class NespTypeInformation
    {
        internal NespTypeInformation()
        {
        }

        public abstract string FullName { get; }

        public abstract string Name { get; }

        public abstract bool IsEnum { get; }

        public abstract bool IsAssignableFrom(NespTypeInformation type);

        public static NespGenericParameterTypeInformation CreateGenericParameter(string name)
        {
            return new NespGenericParameterTypeInformation(name);
        }
    }

    public sealed class NespRuntimeTypeInformation : NespTypeInformation
    {
        private readonly TypeInfo typeInfo;

        internal NespRuntimeTypeInformation(TypeInfo typeInfo)
        {
            Debug.Assert(typeInfo.IsGenericParameter == false);

            this.typeInfo = typeInfo;
        }

        public override string FullName => NespUtilities.GetReadableTypeName(typeInfo);

        public override string Name => this.FullName.Split('.').Last();

        public override bool IsEnum => typeInfo.IsEnum;

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

        public override string ToString()
        {
            return NespUtilities.GetReservedReadableTypeName(typeInfo);
        }
    }

    public sealed class NespGenericParameterTypeInformation : NespTypeInformation
    {
        internal NespGenericParameterTypeInformation(string name)
        {
            this.Name = name;
        }

        public override string FullName => $"'{this.Name}";

        public override string Name { get; }

        public override bool IsEnum => false; // TODO: Apply constraints

        public override bool IsAssignableFrom(NespTypeInformation type)
        {
            // TODO: Implement assignable
            return false;
        }

        public override string ToString()
        {
            return this.FullName;
        }
    }
}
