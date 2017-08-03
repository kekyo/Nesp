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

namespace Nesp.Expressions
{
    public sealed class NespTokenInformation
    {
        public readonly int StartLine;
        public readonly int StartColumn;
        public readonly int EndLine;
        public readonly int EndColumn;

        public NespTokenInformation(int startLine, int startColumn, int endLine, int endColumn)
        {
            this.StartLine = startLine;
            this.StartColumn = startColumn;
            this.EndLine = endLine;
            this.EndColumn = endColumn;
        }

        public override string ToString()
        {
            return $"({this.StartLine},{this.StartColumn}) - ({this.EndLine},{this.EndColumn})";
        }
    }

    public abstract class NespTokenExpression : NespExpression
    {
        internal NespTokenExpression(NespTokenInformation token)
        {
            this.Token = token;
        }

        public NespTokenInformation Token { get; }

        public object Value => this.GetValue();

        internal abstract object GetValue();
    }

    public abstract class NespTokenExpression<T> : NespTokenExpression
    {
        internal NespTokenExpression(NespTokenInformation token)
            : base(token)
        {
        }

        internal override object GetValue()
        {
            return this.Value;
        }

        public new abstract T Value { get; }
    }

    public abstract class NespTypedTokenExpression : NespExpression
    {
        internal NespTypedTokenExpression(NespTokenInformation token)
        {
            this.Token = token;
        }

        public NespTokenInformation Token { get; }
    }
}
