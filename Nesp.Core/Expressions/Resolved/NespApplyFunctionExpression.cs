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

using System;
using System.Linq;
using System.Reflection;

using Nesp.Internals;

namespace Nesp.Expressions.Resolved
{
    public sealed class NespApplyFunctionExpression : NespResolvedExpression
    {
        internal NespApplyFunctionExpression(MethodInfo method, NespExpression[] arguments, NespSourceInformation source)
            : base(source)
        {
            this.Method = method;
            this.Arguments = arguments;
        }

        public override Type FixedType => this.Method.ReturnType;

        public MethodInfo Method { get; }
        public NespExpression[] Arguments { get; }

        public override string ToString()
        {
            return $"{NespUtilities.GetReservedReadableTypeName(this.Method.DeclaringType)}.{this.Method.Name}({string.Join(",", this.Method.GetParameters().Select(parameter => NespUtilities.GetReservedReadableTypeName(parameter.ParameterType)))})";
        }
    }
}
