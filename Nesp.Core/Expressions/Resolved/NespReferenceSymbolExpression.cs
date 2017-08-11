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
using System.Diagnostics;

namespace Nesp.Expressions.Resolved
{
    /// <summary>
    /// This is a expression for symbol id maybe parameter or bound expression.
    /// </summary>
    /// <remarks>This expression temporary usage, will correct another expression by resolver.</remarks>
    public sealed class NespReferenceSymbolExpression : NespSymbolExpression
    {
        internal NespReferenceSymbolExpression(string symbol, NespSourceInformation source)
            : base(symbol, source)
        {
        }

        public override Type Type => this.Related?.Type;

        public NespSymbolExpression Related { get; private set; }

        internal void SetRelated(NespSymbolExpression related)
        {
            Debug.Assert(related.Symbol == this.Symbol);

            this.Related = related;
        }

        public override string ToString()
        {
            return $"{this.Symbol}";
        }
    }
}
