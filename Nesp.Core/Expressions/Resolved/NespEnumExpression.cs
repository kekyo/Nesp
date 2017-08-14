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
using Nesp.Internals;

namespace Nesp.Expressions.Resolved
{
    public sealed class NespEnumExpression : NespTokenExpression<Enum>
    {
        internal NespEnumExpression(Enum value, NespSourceInformation source)
            : base(source)
        {
            this.FixedType = value.GetType();
            this.Value = value;
        }

        public override Type FixedType { get; }
        public override Enum Value { get; }

        public override string ToString()
        {
            return $"{NespUtilities.FormatReservedReadableString(this.Value)}";
        }
    }
}
