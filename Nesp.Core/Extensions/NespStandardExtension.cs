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
using System.Threading.Tasks;

using Nesp.Internals;

namespace Nesp.Extensions
{
    public sealed class NespStandardExtension : NespExtensionBase
    {
        public static readonly IReadOnlyDictionary<TypeInfo, string> ReservedTypeNames =
            NespUtilities.ReservedTypeNames;

        public static readonly INespExtension Instance = new NespStandardExtension();

        private NespStandardExtension()
        {
        }

        internal static IMemberProducer CreateMemberProducer()
        {
            var extractor = new MemberExtractor(
                ReservedTypeNames.Keys.Concat(new[] { typeof(NespStandardOperators).GetTypeInfo() }));
            return new NespStandardMemberProducer(extractor);
        }

        protected override Task<IMemberProducer> CreateMemberProducerAsync()
        {
            return Task.Run(() => CreateMemberProducer());
        }
    }
}
