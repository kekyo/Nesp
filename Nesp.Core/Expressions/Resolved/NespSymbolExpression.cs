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

using Nesp.Internals;

namespace Nesp.Expressions.Resolved
{
    public abstract class NespSymbolExpression : NespResolvedExpression
    {
        private Type type;

        internal NespSymbolExpression(string symbol, NespSourceInformation source)
            : this(symbol, null, source)
        {
        }

        internal NespSymbolExpression(string symbol, Type annotatedType, NespSourceInformation source)
            : base(source)
        {
            this.Symbol = symbol;
            this.type = annotatedType;
        }

        public override Type FixedType => type;

        public string Symbol { get; }

        internal void InferenceByType(Type type)
        {
            Debug.Assert(type != null);
            Debug.Assert(this.type == null);

            this.type = type;
        }

        public override string ToString()
        {
            if (type != null)
            {
                return $"{this.Symbol}:{NespUtilities.GetReadableTypeName(type)}";
            }
            else
            {
                return $"{this.Symbol}:?";
            }
        }
    }
}
