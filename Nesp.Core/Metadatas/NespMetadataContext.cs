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

namespace Nesp.Metadatas
{
    public sealed class NespMetadataContext
    {
        private struct RuntimeTypeInformations
        {
            public readonly TypeInfo RuntimeTypeInfo;
            public readonly NespTypeInformation Type;

            public RuntimeTypeInformations(TypeInfo typeInfo)
            {
                this.RuntimeTypeInfo = typeInfo;
                this.Type = new NespRuntimeTypeInformation(typeInfo);
            }
        }

        private static class RuntimeTypeInformationFactory<T>
        {
            public static readonly RuntimeTypeInformations Informations =
                new RuntimeTypeInformations(typeof(T).GetTypeInfo());
        }

        private static readonly Dictionary<TypeInfo, NespTypeInformation> standardTypes;

        static NespMetadataContext()
        {
            standardTypes = new[]
                {
                    RuntimeTypeInformationFactory<bool>.Informations,
                    RuntimeTypeInformationFactory<byte>.Informations,
                    RuntimeTypeInformationFactory<sbyte>.Informations,
                    RuntimeTypeInformationFactory<short>.Informations,
                    RuntimeTypeInformationFactory<ushort>.Informations,
                    RuntimeTypeInformationFactory<int>.Informations,
                    RuntimeTypeInformationFactory<uint>.Informations,
                    RuntimeTypeInformationFactory<long>.Informations,
                    RuntimeTypeInformationFactory<ulong>.Informations,
                    RuntimeTypeInformationFactory<float>.Informations,
                    RuntimeTypeInformationFactory<double>.Informations,
                    RuntimeTypeInformationFactory<decimal>.Informations,
                    RuntimeTypeInformationFactory<char>.Informations,
                    RuntimeTypeInformationFactory<string>.Informations,
                    RuntimeTypeInformationFactory<DateTime>.Informations,
                    RuntimeTypeInformationFactory<Guid>.Informations,
                    RuntimeTypeInformationFactory<IntPtr>.Informations,
                    RuntimeTypeInformationFactory<UIntPtr>.Informations,
                    RuntimeTypeInformationFactory<Unit>.Informations,
                }
                .ToDictionary(entry => entry.RuntimeTypeInfo, entry => entry.Type);

            standardTypes.Add(
                typeof(void).GetTypeInfo(),
                new NespRuntimeTypeInformation(typeof(Unit).GetTypeInfo()));
        }

        public static NespTypeInformation UnsafeFromType<T>()
        {
            return RuntimeTypeInformationFactory<T>.Informations.Type;
        }

        private readonly Dictionary<TypeInfo, NespTypeInformation> runtimeTypes;
        private readonly Dictionary<MethodInfo, NespFunctionInformation> runtimeMethods =
            new Dictionary<MethodInfo, NespFunctionInformation>();

        public NespMetadataContext()
            : this(new Dictionary<TypeInfo, NespTypeInformation>(standardTypes))
        {
        }

        private NespMetadataContext(Dictionary<TypeInfo, NespTypeInformation> runtimeTypes)
        {
            this.runtimeTypes = runtimeTypes;
        }

        public NespTypeInformation FromType(TypeInfo runtimeTypeInfo)
        {
            if (runtimeTypes.TryGetValue(runtimeTypeInfo, out var type) == false)
            {
                type = new NespRuntimeTypeInformation(runtimeTypeInfo);
                runtimeTypes.Add(runtimeTypeInfo, type);
            }
            return type;
        }

        public NespFunctionInformation FromMethodInfo(MethodInfo runtimeMethod)
        {
            if (runtimeMethods.TryGetValue(runtimeMethod, out var function) == false)
            {
                function = new NespFunctionInformation(runtimeMethod, this);
                runtimeMethods.Add(runtimeMethod, function);
            }
            return function;
        }
    }
}
