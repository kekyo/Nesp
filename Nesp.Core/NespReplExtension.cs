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

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace Nesp
{
    public sealed class NespReplExit
    {
        public static NespReplExit Value = new NespReplExit();

        private NespReplExit()
        {
        }

        public override string ToString()
        {
            return "exit";
        }
    }

    public sealed class NespReplExtension : INespExtension
    {
        public static readonly INespExtension Instance = new NespReplExtension();

        private NespReplExtension()
        {
        }

        public Task<IReadOnlyDictionary<string, MemberInfo[]>> GetMembersAsync()
        {
            return Task.FromResult(
                (IReadOnlyDictionary<string, MemberInfo[]>)
                new Dictionary<string, MemberInfo[]>
                {
                    { "exit", new[] { typeof(NespReplExit).GetRuntimeField("Value") } }
                });
        }
    }
}
