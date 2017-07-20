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
using System.Threading.Tasks;

namespace Nesp
{
    public static class Program
    {
        private static string Format(object value)
        {
            if (value == null)
            {
                return "(null)";
            }
            if (value is string)
            {
                return "\"" + value + "\"";
            }
            var type = value.GetType();
            if (type.IsPrimitive)
            {
                return value.ToString();
            }

            return $"{value} : {value.GetType().Name}";
        }

        private static async Task<int> MainAsync(string[] args)
        {
            Console.WriteLine("This is Nesp interpreter.");
            Console.WriteLine("Copyright (c) 2017 Kouji Matsui (@kekyo2)");

            var engine = new NespEngine(NespExpressionType.Repl);

            while (true)
            {
                Console.Write("> ");
                var replLine = Console.ReadLine();
                var func = await engine.CompileExpressionAsync(replLine);
                var result = func();
                Console.WriteLine(Format(result));
            }

            return 0;
        }

        public static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }
    }
}
