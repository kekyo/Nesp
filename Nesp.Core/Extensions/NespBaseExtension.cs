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

namespace Nesp.Extensions
{
    public sealed class NespBaseExtension : INespExtension
    {
        public static readonly INespExtension Instance = new NespBaseExtension();

        private IReadOnlyDictionary<string, MemberInfo[]> cached;

        private NespBaseExtension()
        {
        }

        internal static IReadOnlyDictionary<string, MemberInfo[]> CreateMembers()
        {
            var extractor = new MemberExtractor(
                new[] { typeof(object), typeof(Uri), typeof(Enumerable) }
                .Select(type => type.GetTypeInfo().Assembly));
            return extractor.MembersByName;
        }

        public Task<IReadOnlyDictionary<string, MemberInfo[]>> GetMembersAsync()
        {
            if (cached != null)
            {
                return Task.FromResult(cached);
            }

            return Task.Run(() =>
            {
                cached = CreateMembers();
                return cached;
            });
        }
    }
}
