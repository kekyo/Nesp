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

namespace Nesp.Internals
{
#if true
    public static class OptionalExtension
    {
        #region ToDisplayString
        public static string ToDisplayString<T>(this T optional)
            where T : class
        {
            return (optional != null) ? $"Some({optional})" : "None";
        }

        public static string ToDisplayString<T>(this T? optional)
            where T : struct
        {
            return optional.HasValue ? $"Some({optional.Value})" : "None";
        }
        #endregion

        #region Bind
        public static U Bind<T, U>(this T optional, Func<T, U> binder)
            where T : class
            where U : class
        {
            return optional != null ? binder(optional) : null;
        }

        public static U Bind<T, U>(this T? optional, Func<T, U> binder)
            where T : struct
            where U : class
        {
            return optional.HasValue ? binder(optional.Value) : null;
        }

        public static U? Bind<T, U>(this T optional, Func<T, U?> binder)
            where T : class
            where U : struct
        {
            return optional != null ? binder(optional) : default(U?);
        }

        public static U? Bind<T, U>(this T? optional, Func<T, U?> binder)
            where T : struct
            where U : struct
        {
            return optional.HasValue ? binder(optional.Value) : default(U?);
        }
        #endregion

        #region Match
        public static U Match<T, U>(this T optional, Func<T, U> some, Func<U> none)
            where T : class
        {
            return optional != null ? some(optional) : none();
        }

        public static U Match<T, U>(this T? optional, Func<T, U> some, Func<U> none)
            where T : struct
        {
            return optional.HasValue ? some(optional.Value) : none();
        }

        public static U Match<T, U>(this T optional, Func<T, U> some, U none)
            where T : class
        {
            return optional != null ? some(optional) : none;
        }

        public static U Match<T, U>(this T? optional, Func<T, U> some, U none)
            where T : struct
        {
            return optional.HasValue ? some(optional.Value) : none;
        }

        public static void Match<T>(this T optional, Action<T> some, Action none)
            where T : class
        {
            if (optional != null)
            {
                some(optional);
            }
            else
            {
                none();
            }
        }

        public static void Match<T>(this T? optional, Action<T> some, Action none)
            where T : struct
        {
            if (optional.HasValue)
            {
                some(optional.Value);
            }
            else
            {
                none();
            }
        }

        public static void Match<T>(this T optional, Action<T> some)
            where T : class
        {
            if (optional != null)
            {
                some(optional);
            }
        }

        public static void Match<T>(this T? optional, Action<T> some)
            where T : struct
        {
            if (optional.HasValue)
            {
                some(optional.Value);
            }
        }
        #endregion

        #region AsOptional
        /// <summary>
        /// int.Parse(str, out var value).AsOptional(value)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tryResult"></param>
        /// <param name="value"></param>
        /// <returns>Optional (Nullable) value</returns>
        public static T? AsOptional<T>(this bool tryResult, T value)
            where T : struct
        {
            return tryResult ? value : default(T?);
        }
        #endregion
    }
#else
    public sealed class Optional<T>
    {
        public static readonly Optional<T> None = null;

        public static Optional<T> Some(T value)
        {
            return new Optional<T>(value);
        }

        internal readonly T value;

        public Optional(T value)
        {
            this.value = value;
        }

        public override string ToString()
        {
            return $"Some({value})";
        }

        public static implicit operator Optional<T>(T value)
        {
            return new Optional<T>(value);
        }

        public static implicit operator Optional<T>(Optional none)
        {
            return null;
        }
    }

    public struct Optional
    {
        public static readonly Optional None = new Optional();

        public static Optional<T> Some<T>(T value)
        {
            return Optional<T>.Some(value);
        }

        public string ToDisplayString()
        {
            return "None";
        }

        public override string ToString()
        {
            return "None";
        }
    }

    public static class OptionalExtension
    {
        public static string ToDisplayString<T>(this Optional<T> optional)
        {
            return optional?.ToString() ?? "None";
        }

        public static Optional<U> Bind<T, U>(this Optional<T> optional, Func<T, Optional<U>> binder)
        {
            return optional != null ? binder(optional.value) : null;
        }

        public static U Match<T, U>(this Optional<T> optional, Func<T, U> some, Func<U> none)
        {
            return optional != null ? some(optional.value) : none();
        }
    }
#endif
}
