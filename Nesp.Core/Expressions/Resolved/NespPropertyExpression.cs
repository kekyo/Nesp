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

using Nesp.Metadatas;

namespace Nesp.Expressions.Resolved
{
    public sealed class NespPropertyExpression : NespResolvedExpression
    {
        internal NespPropertyExpression(NespPropertyInformation property, NespSourceInformation source)
            : base(source)
        {
            this.Property = property;
        }

        public override NespTypeInformation Type => this.Property.PropertyType;

        public NespPropertyInformation Property { get; }

        public override string ToString()
        {
            return $"[{this.Property}]:property";
        }
    }
}
