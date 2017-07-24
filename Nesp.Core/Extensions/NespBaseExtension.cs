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
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Nesp.Extensions
{
    public sealed class NespBaseExtension : NespExtensionBase
    {
        public static readonly INespExtension Instance = new NespBaseExtension();

        private NespBaseExtension()
        {
        }

        internal static INespMemberProducer CreateMemberProducer()
        {
            return new NespMemberExtractor(
                new[] {typeof(object), typeof(Uri), typeof(Enumerable)}
                .SelectMany(type => type.GetTypeInfo().Assembly.DefinedTypes)
                .Where(typeInfo => typeInfo.IsPublic)
                .Concat(new[] { typeof(Unit).GetTypeInfo() }));
        }

        protected override Task<INespMemberProducer> CreateMemberProducerAsync()
        {
            return Task.Run(() => CreateMemberProducer());
        }
    }
}
