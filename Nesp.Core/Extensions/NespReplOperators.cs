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
    public sealed class NespReplExit
    {
        internal NespReplExit(int exitCode)
        {
            this.ExitCode = exitCode;
        }

        public int ExitCode { get; }

        public override string ToString()
        {
            return $"exit {this.ExitCode}";
        }
    }

    public sealed class NespReplCls
    {
        public static NespReplCls Instance = new NespReplCls();

        private NespReplCls()
        {
        }
    }

    public sealed class NespReplHelp
    {
        public static NespReplHelp Instance = new NespReplHelp();

        private NespReplHelp()
        {
        }
    }

    internal static class NespReplOperators
    {
        [MemberBind("exit")]
        public static object Exit()
        {
            return new NespReplExit(0);
        }

        [MemberBind("exit")]
        public static object Exit(int exitCode)
        {
            return new NespReplExit(exitCode);
        }

        [MemberBind("cls")]
        public static object Cls()
        {
            return NespReplCls.Instance;
        }

        [MemberBind("help")]
        public static object Help()
        {
            return NespReplHelp.Instance;
        }
    }
}
