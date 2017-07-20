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

using System.Collections.Generic;

namespace Nesp
{
    internal struct ImmutableDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> inner;
        private bool isOrigin;

        public ImmutableDictionary(Dictionary<TKey, TValue> original)
        {
            inner = original;
            isOrigin = true;
        }

        public void Add(TKey key, TValue value)
        {
            if (isOrigin == true)
            {
                inner = new Dictionary<TKey, TValue>(inner);
                isOrigin = false;
            }

            inner.Add(key, value);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            return inner.TryGetValue(key, out value);
        }
    }

    internal static class DictionaryExtensions
    {
        public static TValue GetValue<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key)
            where TValue : class
        {
            dict.TryGetValue(key, out var value);
            return value;
        }
    }
}
