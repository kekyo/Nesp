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

namespace Nesp.Expressions
{
    public sealed class NespIdExpression : NespTokenExpression<string>
    {
        internal NespIdExpression(string id, NespSourceInformation source)
            : base(source)
        {
            this.Id = id;
        }

        public override bool IsResolved => false;

        public override Type Type => null;

        public string Id { get; }
        public override string Value => this.Id;

        internal override NespExpression[] OnResolveMetadata(NespMetadataResolverContext context)
        {
            return context.ResolveById(this.Id, this);
        }

        public override string ToString()
        {
            return $"[{this.Id}]";
        }
    }
}
