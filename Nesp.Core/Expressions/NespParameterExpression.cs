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
using System.Linq.Expressions;
using System.Reflection;

namespace Nesp.Expressions
{
    public sealed class NespParameterExpression : NespExpression
    {
        private Type candidateType;

        internal NespParameterExpression(Type annotateType, string name)
        {
            this.AnnotateType = annotateType;
            candidateType = annotateType;
            this.Name = name;
        }

        public override Type CandidateType => candidateType ?? typeof(object);

        public Type AnnotateType { get; }
        public string Name { get; }

        public void InflateType(Type type)
        {
            if (this.AnnotateType != null)
            {
                if (type.GetTypeInfo().IsAssignableFrom(this.AnnotateType.GetTypeInfo()) == false)
                {
                    throw new ArgumentException(
                        $"Cannot cast implicitly: {NespEngine.GetReadableTypeName(this.AnnotateType)} --> {NespEngine.GetReadableTypeName(type)}");
                }
            }
            // Candidate type not set (Not given annotate type)
            else if (candidateType == null)
            {
                candidateType = type;
            }
            // Suggested type can inflate.
            else if (candidateType.GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                candidateType = type;
            }
        }

        internal override Expression OnCreate()
        {
            return Expression.Parameter(this.CandidateType, this.Name);
        }
    }
}
