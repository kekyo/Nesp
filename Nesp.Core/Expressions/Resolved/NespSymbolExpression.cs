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

namespace Nesp.Expressions.Resolved
{
    /// <summary>
    /// This is UNRESOLVED symbol id expression.
    /// </summary>
    /// <remarks>This expression temporary usage, will correct another expression by resolver.</remarks>
    public sealed class NespSymbolExpression : NespResolvedExpression
    {
        internal NespSymbolExpression(string symbol, NespSourceInformation source)
            : base(source)
        {
            this.Symbol = symbol;
        }

        public override Type Type => null;

        public string Symbol { get; }

        public override string ToString()
        {
            return $"{this.Symbol}";
        }
    }
}
