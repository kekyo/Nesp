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

using System.Threading.Tasks;

namespace Nesp.Expressions
{
    public abstract class NespExpression
    {
        private NespExpressionResolverContext cachedContext;
        private NespExpression cachedExpression;

        internal NespExpression()
        {
        }

        internal static Task<NespExpression> FromResult<T>(T value)
            where T : NespExpression
        {
            return Task.FromResult((NespExpression)value);
        }

        internal virtual Task<NespExpression> OnResolveAsync(NespExpressionResolverContext context)
        {
            return Task.FromResult(this);
        }

        public async Task<NespExpression> ResolveAsync(NespExpressionResolverContext context)
        {
            if (object.ReferenceEquals(context, cachedContext) == false)
            {
                cachedContext = context;
                cachedExpression = await this.OnResolveAsync(context);
            }

            return cachedExpression;
        }
    }
}
