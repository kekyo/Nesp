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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Nesp.Expressions
{

    public class NespLambdaExpression : NespExpression
    {
        internal NespLambdaExpression(NespExpression expression, string name, IEnumerable<NespParameterExpression> parameters)
        {
            this.Expression = expression;
            this.Name = name;
            this.Parameters = parameters;
        }

        public override Type CandidateType => this.Expression.CandidateType;

        public NespExpression Expression { get; }
        public string Name { get; }
        public IEnumerable<NespParameterExpression> Parameters { get; }

        internal override Expression OnCreate()
        {
            // TODO: Support tailcall recursion
            return System.Linq.Expressions.Expression.Lambda(
                this.Expression.Create(),
                this.Name,
                this.Parameters
                    .Select(argExpr => (ParameterExpression)argExpr.Create())
                    .ToArray());
        }
    }

    public sealed class NespLambdaExpression<TDelegate> : NespLambdaExpression
        where TDelegate : class
    {
        private TDelegate cache;

        internal NespLambdaExpression(NespExpression expression, string name, IEnumerable<NespParameterExpression> parameters)
            : base(expression, name, parameters)
        {
        }

        internal override Expression OnCreate()
        {
            // TODO: Support tailcall recursion
            return System.Linq.Expressions.Expression.Lambda<TDelegate>(
                this.Expression.Create(),
                this.Name,
                this.Parameters
                    .Select(argExpr => (ParameterExpression)argExpr.Create())
                    .ToArray());
        }

        public TDelegate Compile()
        {
            if (cache == null)
            {
                var lambdaExpr = (Expression<TDelegate>)this.Create();
                cache = lambdaExpr.Compile();
            }
            return cache;
        }
    }
}
