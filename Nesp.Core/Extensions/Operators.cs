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

namespace Nesp.Extensions
{
    internal static class Operators
    {
        // TODO: Replace native expressions.

        [MemberBind("+")]
        public static byte Add(byte a, byte b)
        {
            return (byte)(a + b);
        }

        [MemberBind("+")]
        public static short Add(short a, short b)
        {
            return (short)(a + b);
        }

        [MemberBind("+")]
        public static int Add(int a, int b)
        {
            return a + b;
        }

        [MemberBind("+")]
        public static long Add(long a, long b)
        {
            return a + b;
        }

        [MemberBind("+")]
        public static string Add(string a, string b)
        {
            return a + b;
        }

        [MemberBind("+")]
        public static string Add(string a, char b)
        {
            return a + b;
        }

        [MemberBind("+")]
        public static string Add(char a, char b)
        {
            return a.ToString() + b;
        }
    }
}
