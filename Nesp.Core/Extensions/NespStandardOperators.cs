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

namespace Nesp.Extensions
{
    internal static class NespStandardOperators
    {
        // TODO: Replace native expressions.

        #region Addition
        [NespIdentity("+")]
        public static byte Addition(byte a, byte b)
        {
            return (byte)(a + b);
        }

        [NespIdentity("+")]
        public static short Addition(short a, short b)
        {
            return (short)(a + b);
        }

        [NespIdentity("+")]
        public static int Addition(int a, int b)
        {
            return a + b;
        }

        [NespIdentity("+")]
        public static long Addition(long a, long b)
        {
            return a + b;
        }

        [NespIdentity("+")]
        public static float Addition(float a, float b)
        {
            return a + b;
        }

        [NespIdentity("+")]
        public static double Addition(double a, double b)
        {
            return a + b;
        }

        [NespIdentity("+")]
        public static string Addition(string a, string b)
        {
            return a + b;
        }

        [NespIdentity("+")]
        public static string Addition(string a, char b)
        {
            return a + b;
        }

        [NespIdentity("+")]
        public static string Addition(char a, char b)
        {
            return a.ToString() + b;
        }
        #endregion

        #region Subtraction
        [NespIdentity("-")]
        public static byte Subtraction(byte a, byte b)
        {
            return (byte)(a - b);
        }

        [NespIdentity("-")]
        public static short Subtraction(short a, short b)
        {
            return (short)(a - b);
        }

        [NespIdentity("-")]
        public static int Subtraction(int a, int b)
        {
            return a - b;
        }

        [NespIdentity("-")]
        public static long Subtraction(long a, long b)
        {
            return a - b;
        }

        [NespIdentity("-")]
        public static float Subtraction(float a, float b)
        {
            return a - b;
        }

        [NespIdentity("-")]
        public static double Subtraction(double a, double b)
        {
            return a - b;
        }
        #endregion

        #region Multiply
        [NespIdentity("*")]
        public static int Multiply(byte a, byte b)
        {
            return a * b;
        }

        [NespIdentity("*")]
        public static int Multiply(short a, short b)
        {
            return a * b;
        }

        [NespIdentity("*")]
        public static int Multiply(int a, int b)
        {
            return a * b;
        }

        [NespIdentity("*")]
        public static long Multiply(long a, long b)
        {
            return a * b;
        }

        [NespIdentity("*")]
        public static float Multiply(float a, float b)
        {
            return a * b;
        }

        [NespIdentity("*")]
        public static double Multiply(double a, double b)
        {
            return a * b;
        }
        #endregion

        #region Division
        [NespIdentity("/")]
        public static byte Division(byte a, byte b)
        {
            return (byte)(a / b);
        }

        [NespIdentity("/")]
        public static short Division(short a, short b)
        {
            return (short)(a / b);
        }

        [NespIdentity("/")]
        public static int Division(int a, int b)
        {
            return a / b;
        }

        [NespIdentity("/")]
        public static long Division(long a, long b)
        {
            return a / b;
        }

        [NespIdentity("/")]
        public static float Division(float a, float b)
        {
            return a / b;
        }

        [NespIdentity("/")]
        public static double Division(double a, double b)
        {
            return a / b;
        }
        #endregion

        #region Modulus
        [NespIdentity("%")]
        public static byte Modulus(byte a, byte b)
        {
            return (byte)(a / b);
        }

        [NespIdentity("%")]
        public static short Modulus(short a, short b)
        {
            return (short)(a / b);
        }

        [NespIdentity("%")]
        public static int Modulus(int a, int b)
        {
            return a / b;
        }

        [NespIdentity("%")]
        public static long Modulus(long a, long b)
        {
            return a / b;
        }

        [NespIdentity("%")]
        public static float Modulus(float a, float b)
        {
            return a / b;
        }

        [NespIdentity("%")]
        public static double Modulus(double a, double b)
        {
            return a / b;
        }
        #endregion

        #region Equality
        [NespIdentity("==")]
        public static bool Equality(object a, object b)
        {
            if (a != null)
            {
                return a.Equals(b);
            }
            else if (b != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        [NespIdentity("!=")]
        public static bool Inequality(object a, object b)
        {
            if (a != null)
            {
                return a.Equals(b);
            }
            else if (b != null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region New
        [NespIdentity("new")]
        public static object New(Type type)
        {
            return Activator.CreateInstance(type);
        }

        // TODO: Support variable arguments.
        [NespIdentity("new")]
        public static object New(Type type, params object[] args)
        {
            return Activator.CreateInstance(type, args);
        }
        #endregion

        //#region Bind
        //[MemberBind("let")]
        //public static object Bind(string name, object[] argList, object[])
        //{
        //    return Activator.CreateInstance(type);
        //}
        //#endregion
    }
}
