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
using System.Threading.Tasks;

namespace Nesp
{
    internal sealed class MemberBinder : INespMemberBinder
    {
        public MethodInfo SelectMethod(MethodInfo[] candidates, Type[] types)
        {
            return Type.DefaultBinder.SelectMethod(
                BindingFlags.Public | BindingFlags.Static, candidates, types, null) as MethodInfo;
        }
    }

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
            if (NespDefaultExtension.ReservedTypeNames.TryGetValue(type, out var typeName) == false)
            {
                typeName = type.FullName;
            }

            return $"{value} : {typeName}";
        }

        private static async Task<int> MainAsync(string[] args)
        {
            Console.WriteLine("This is Nesp interpreter.");
            Console.WriteLine("Copyright (c) 2017 Kouji Matsui (@kekyo2)");

            Console.Write("Loading ...");

            var memberBinder = new MemberBinder();
            var engine = new NespEngine(NespExpressionType.Repl, memberBinder);

            await engine.AddExtensionAsync(NespDefaultExtension.Instance);
            await engine.AddExtensionAsync(NespReplExtension.Instance);

            Console.WriteLine(" Done.");

            while (true)
            {
                Console.Write("> ");
                var replLine = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(replLine))
                {
                    Console.WriteLine("Try type: '* 123 456'");
                    Console.WriteLine("Try type: 'datetime.Now'");
                    Console.WriteLine("Try type: 'int.Parse \"12345\"'");
                    Console.WriteLine("Try type: 'System.Guid.NewGuid'");
                    continue;
                }

                var func = await engine.CompileExpressionAsync(replLine);
                var result = func();
                var exit = result as NespReplExit;
                if (exit != null)
                {
                    return exit.ExitCode;
                }

                Console.WriteLine(Format(result));
            }
        }

        public static int Main(string[] args)
        {
            return MainAsync(args).Result;
        }
    }
}
