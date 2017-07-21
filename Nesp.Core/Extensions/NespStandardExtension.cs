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
using System.Threading.Tasks;

namespace Nesp.Extensions
{
    public sealed class NespStandardExtension : NespExtensionBase
    {
        public static readonly IReadOnlyDictionary<Type, string> ReservedTypeNames =
            new Dictionary<Type, string>
            {
                { typeof(object), "object" },
                { typeof(byte), "byte" },
                { typeof(sbyte), "sbyte" },
                { typeof(short), "short" },
                { typeof(ushort), "ushort" },
                { typeof(int), "int" },
                { typeof(uint), "uint" },
                { typeof(long), "long" },
                { typeof(ulong), "ulong" },
                { typeof(float), "float" },
                { typeof(double), "double" },
                { typeof(decimal), "decimal" },
                { typeof(bool), "bool" },
                { typeof(string), "string" },
                { typeof(DateTime), "datetime" },
                { typeof(TimeSpan), "timespan" },
                { typeof(Guid), "guid" },
                { typeof(Math), "math" },
                { typeof(Enum), "enum" },
                { typeof(Type), "type" },
            };

        public static readonly INespExtension Instance = new NespStandardExtension();

        private NespStandardExtension()
        {
        }

        internal static IMemberProducer CreateMemberProducer()
        {
            var extractor = new MemberExtractor(
                ReservedTypeNames.Keys.Concat(new[] { typeof(NespStandardOperators) }));
            return new NespStandardMemberProducer(extractor);
        }

        protected override Task<IMemberProducer> CreateMemberProducerAsync()
        {
            return Task.Run(() => CreateMemberProducer());
        }
    }
}
