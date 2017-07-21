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

using System.Linq.Expressions;
using System.Reflection;

namespace Nesp.Internals
{
    internal sealed class CandidateInfo
    {
        public readonly CandidatesDictionary<ConstantExpression> Types;
        public readonly CandidatesDictionary<Expression> Fields;
        public readonly CandidatesDictionary<MemberExpression> Properties;
        public readonly CandidatesDictionary<MethodInfo> Methods;
        public readonly CandidatesDictionary<Expression> Locals;

        public CandidateInfo() : this(
            new CandidatesDictionary<ConstantExpression>(),
            new CandidatesDictionary<Expression>(),
            new CandidatesDictionary<MemberExpression>(),
            new CandidatesDictionary<MethodInfo>(),
            new CandidatesDictionary<Expression>())
        {
        }

        private CandidateInfo(
            CandidatesDictionary<ConstantExpression> types,
            CandidatesDictionary<Expression> fields,
            CandidatesDictionary<MemberExpression> properties,
            CandidatesDictionary<MethodInfo> methods,
            CandidatesDictionary<Expression> locals)
        {
            this.Types = types;
            this.Fields = fields;
            this.Properties = properties;
            this.Methods = methods;
            this.Locals = locals;
        }

        public CandidateInfo Clone()
        {
            return new CandidateInfo(
                this.Types.Clone(),
                this.Fields.Clone(),
                this.Properties.Clone(),
                this.Methods.Clone(),
                this.Locals.Clone());
        }
    }
}
