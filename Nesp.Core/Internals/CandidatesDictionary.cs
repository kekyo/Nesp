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
using System.Collections.Generic;
using System.Linq;

namespace Nesp.Internals
{
    internal sealed class CandidatesDictionary<T>
    {
        private static readonly T[] empty = new T[0];

        private readonly IReadOnlyDictionary<string, T[]> original;
        private Dictionary<string, T[]> inner;

        public CandidatesDictionary()
        {
        }

        private CandidatesDictionary(IReadOnlyDictionary<string, T[]> original)
        {
            this.original = original;
            this.inner = null;
        }

        public CandidatesDictionary<T> Clone()
        {
            return new CandidatesDictionary<T>(inner ?? original);
        }

        private void Unique()
        {
            if (inner != null)
            {
                return;
            }

            var dict = original as IDictionary<string, T[]>;
            if (dict != null)
            {
                inner = new Dictionary<string, T[]>(dict);
            }
            else if (original != null)
            {
                inner = original.ToDictionary(entry => entry.Key, entry => entry.Value);
            }
            else
            {
                inner = new Dictionary<string, T[]>();
            }
        }

        public void AddCandidate(string key, T candidate)
        {
            this.Unique();

            if (inner.TryGetValue(key, out var last) == false)
            {
                var newCandidates = new[] { candidate };
                inner.Add(key, newCandidates);
            }
            else
            {
                var newCandidates = new T[last.Length + 1];
                Array.Copy(last, newCandidates, last.Length);
                newCandidates[last.Length] = candidate;
                inner[key] = newCandidates;
            }
        }

        public void AddCandidates(string key, T[] candidates)
        {
            this.Unique();

            if (inner.TryGetValue(key, out var last) == false)
            {
                inner.Add(key, candidates);
            }
            else
            {
                var newCandidates = new T[last.Length + candidates.Length];
                Array.Copy(last, newCandidates, last.Length);
                Array.Copy(candidates, 0, newCandidates, last.Length, candidates.Length);
                inner[key] = newCandidates;
            }
        }

        public T[] this[string key]
        {
            get
            {
                if (inner != null)
                {
                    return inner.TryGetValue(key, out var candidates)
                        ? candidates
                        : empty;
                }
                else if (original != null)
                {
                    return original.TryGetValue(key, out var candidates)
                        ? candidates
                        : empty;
                }
                else
                {
                    return empty;
                }
            }
        }
    }
}