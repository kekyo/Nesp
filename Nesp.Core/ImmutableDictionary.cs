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
using System.Linq;

namespace Nesp
{
    internal struct ImmutableDictionary<TKey, TValue>
    {
        private readonly IReadOnlyDictionary<TKey, TValue> original;
        private Dictionary<TKey, TValue> inner;

        public ImmutableDictionary(IReadOnlyDictionary<TKey, TValue> original)
        {
            this.original = original;
            this.inner = null;
        }

        private void Unique()
        {
            if (inner != null)
            {
                return;
            }

            var dict = original as IDictionary<TKey, TValue>;
            if (dict != null)
            {
                inner = new Dictionary<TKey, TValue>(dict);
            }
            else if (original != null)
            {
                inner = original.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            else
            {
                inner = new Dictionary<TKey, TValue>();
            }
        }

        public void AddValue(TKey key, TValue value)
        {
            this.Unique();
            inner.Add(key, value);
        }

        public void SetValue(TKey key, TValue value)
        {
            this.Unique();
            inner[key] = value;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (inner != null)
            {
                return inner.TryGetValue(key, out value);
            }
            else if (original != null)
            {
                return original.TryGetValue(key, out value);
            }
            else
            {
                value = default(TValue);
                return false;
            }
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
