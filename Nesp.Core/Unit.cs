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
using Nesp.Extensions;

namespace Nesp
{
    [MemberBind("unit")]
    public struct Unit : IEquatable<Unit>
    {
        public static readonly object Value = new Unit();

        public bool Equals(Unit unit)
        {
            return true;
        }

        bool IEquatable<Unit>.Equals(Unit unit)
        {
            return true;
        }

        public override bool Equals(object obj)
        {
            return obj is Unit;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "unit";
        }

        public static bool operator ==(Unit lhs, Unit rhs)
        {
            return true;
        }

        public static bool operator !=(Unit lhs, Unit rhs)
        {
            return false;
        }
    }
}
