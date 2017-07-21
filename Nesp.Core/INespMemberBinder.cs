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
using System.Reflection;

namespace Nesp
{
    /// <summary>
    /// This interface injects runtime member binder implementation.
    /// </summary>
    /// <example>
    /// <code>
    /// // This is typical implementation for runtime binder.
    /// private sealed class MemberBinder : INespMemberBinder
    /// {
    ///     public MethodInfo SelectMethod(MethodInfo[] candidates, Type[] types)
    ///     {
    ///         return Type.DefaultBinder.SelectMethod(
    ///             BindingFlags.Public | BindingFlags.Static, candidates, types, null) as MethodInfo;
    ///     }
    /// }
    /// </code>
    /// </example>
    public interface INespMemberBinder
    {
        /// <summary>
        /// This is selector for method of candidate.
        /// </summary>
        /// <param name="candidates">Target methods</param>
        /// <param name="argTypes">Matching argument types</param>
        /// <returns>Selected method</returns>
        // TODO: DefaultBinder.SelectMethod can't resolve variable arguments (params).
        MethodInfo SelectMethod(MethodInfo[] candidates, Type[] argTypes);
    }
}
