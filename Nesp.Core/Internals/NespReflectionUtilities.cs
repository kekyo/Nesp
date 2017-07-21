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
using System.Collections;
using System.Linq;
using System.Reflection;

namespace Nesp.Internals
{
    internal static class NespReflectionUtilities
    {
        public static Type AsType(this MemberInfo member)
        {
            // PCL stupid:
            //   System.Type not inherit from System.Reflection.MemberInfo on PCL.
            return ((object) member) as Type;
        }

        public static string GetReadableTypeName(Type type, Func<Type, string> getTypeName)
        {
            // getTypeName is recursive call target (combinator)

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                return $"{getTypeName(elementType)}[{new string(Enumerable.Range(0, type.GetArrayRank() - 1).Select(index => ',').ToArray())}]";
            }

            // TODO: Generic type
            // TODO: Inner type
            // TODO: Delegate type (Format with arrow operator)
            return $"{type.Namespace}.{type.Name}";
        }

        private static string InternalFormatReadableString(object value)
        {
            if (value == null)
            {
                return "(null)";
            }
            if (value is string)
            {
                return "\"" + value + "\"";
            }

            var collection = value as ICollection;
            if (collection != null)
            {
                return string.Join(
                    " ",
                    collection
                    .Cast<object>()
                    .Select(InternalFormatReadableString));
            }
            else
            {
                return value.ToString();
            }
        }

        public static string FormatReadableString(object value, Func<Type, string> getTypeName)
        {
            if (value == null)
            {
                return "(null)";
            }

            var type = value.GetType();
            var typeName = getTypeName(type);

            return $"{InternalFormatReadableString(value)} : {typeName}";
        }
    }
}
