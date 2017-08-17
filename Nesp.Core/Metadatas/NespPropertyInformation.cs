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

using System.Reflection;

namespace Nesp.Metadatas
{
    public sealed class NespPropertyInformation
    {
        private readonly PropertyInfo property;

        internal NespPropertyInformation(PropertyInfo property, NespMetadataContext context)
        {
            this.property = property;
            this.DeclaringType = context.FromType(property.DeclaringType.GetTypeInfo());
            this.PropertyType = context.FromType(property.PropertyType.GetTypeInfo());
            this.Getter = (property.GetMethod != null) ? context.FromMethodInfo(property.GetMethod) : null;
            this.Setter = (property.SetMethod != null) ? context.FromMethodInfo(property.SetMethod) : null;
        }

        public string Name => property.Name;
        public NespTypeInformation DeclaringType { get; }
        public NespTypeInformation PropertyType { get; }
        public NespFunctionInformation Getter { get; }
        public NespFunctionInformation Setter { get; }

        public override string ToString()
        {
            return $"{this.DeclaringType}.{this.Name}";
        }
    }
}
