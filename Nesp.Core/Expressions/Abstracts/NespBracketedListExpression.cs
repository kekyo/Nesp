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

using System.Linq;

namespace Nesp.Expressions.Abstracts
{
    public sealed class NespBracketedListExpression : NespAbstractListExpression
    {
        internal NespBracketedListExpression(NespExpression[] list, NespSourceInformation source)
            : base(list, source)
        {
        }

        internal override NespResolvedExpression[] OnResolveMetadata(NespMetadataResolverContext context)
        {
            return context.ResolveByBracketedList(this.List, this);
        }

        public override string ToString()
        {
            return $"({string.Join(" ", this.List.Select(iexpr => iexpr.ToString()))})";
        }
    }
}
