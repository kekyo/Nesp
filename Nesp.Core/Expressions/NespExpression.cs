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
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nesp.Expressions
{
    internal static class NespExpressionExtension
    {
        public static Expression Create(this NespExpression expression)
        {
            return expression?.InternalCreate();
        }
    }

    public abstract class NespExpression
    {
        private Expression cache;

        internal NespExpression()
        {
        }

        public abstract Type CandidateType { get; }

        internal abstract Expression OnCreate();

        internal Expression InternalCreate()
        {
            return cache ?? (cache = this.OnCreate());
        }

        public static NespConstantExpression Constant(object value)
        {
            return new NespConstantExpression(value);
        }

        public static NespConvertExpression Convert(NespExpression expression, Type targetType)
        {
            return new NespConvertExpression(expression, targetType);
        }

        public static NespFieldExpression Field(NespExpression instance, FieldInfo fi)
        {
            return new NespFieldExpression(instance, fi);
        }

        public static NespPropertyExpression Property(
            NespExpression instance,
            PropertyInfo pi)
        {
            return new NespPropertyExpression(instance, pi);
        }

        public static NespApplyFunctionExpression Apply(
            NespExpression instance,
            MethodInfo mi,
            IEnumerable<NespExpression> arguments)
        {
            return new NespApplyFunctionExpression(instance, mi, arguments);
        }

        public static NespParameterExpression Parameter(string name)
        {
            return new NespParameterExpression(null, name);
        }

        public static NespParameterExpression Parameter(Type annotateType, string name)
        {
            return new NespParameterExpression(annotateType, name);
        }

        public static NewArrayExpression NewArrayInit(
            Type elementType,
            IEnumerable<NespExpression> initialValues)
        {
            return new NewArrayExpression(elementType, initialValues);
        }

        public static NespLambdaExpression Lambda(
            NespExpression expression,
            string name,
            IEnumerable<NespParameterExpression> parameters)
        {
            return new NespLambdaExpression(expression, name, parameters);
        }

        public static NespLambdaExpression<TDelegate> Lambda<TDelegate>(
            NespExpression expression,
            string name,
            IEnumerable<NespParameterExpression> parameters)
            where TDelegate : class
        {
            return new NespLambdaExpression<TDelegate>(expression, name, parameters);
        }
    }

    public sealed class NespConvertExpression : NespExpression
    {
        internal NespConvertExpression(NespExpression operand, Type targetType)
        {
            this.CandidateType = targetType;
            this.Operand = operand;
        }

        public override Type CandidateType { get; }

        public NespExpression Operand { get; }

        internal override Expression OnCreate()
        {
            return Expression.Convert(this.Operand.Create(), this.CandidateType);
        }
    }

    public sealed class NespFieldExpression : NespExpression
    {
        internal NespFieldExpression(NespExpression instance, FieldInfo fi)
        {
            this.Instance = instance;
            this.Field = fi;
        }

        public override Type CandidateType => this.Field.FieldType;

        public NespExpression Instance { get; }
        public new FieldInfo Field { get; }

        internal override Expression OnCreate()
        {
            return Expression.Field(this.Instance.Create(), this.Field);
        }
    }

    public sealed class NespPropertyExpression : NespExpression
    {
        internal NespPropertyExpression(NespExpression instance, PropertyInfo pi)
        {
            this.Instance = instance;
            this.Property = pi;
        }

        public override Type CandidateType => this.Property.PropertyType;

        public NespExpression Instance { get; }
        public new PropertyInfo Property { get; }

        internal override Expression OnCreate()
        {
            return Expression.Property(this.Instance.Create(), this.Property);
        }
    }

    public sealed class NewArrayExpression : NespExpression
    {
        internal NewArrayExpression(Type hintElementType, IEnumerable<NespExpression> initialValues)
        {
            this.HintElementType = hintElementType;
            this.InitialValues = initialValues;
        }

        public override Type CandidateType => this.HintElementType.MakeArrayType();

        public Type HintElementType { get; }
        public IEnumerable<NespExpression> InitialValues { get; }

        internal override Expression OnCreate()
        {
            return Expression.NewArrayInit(
                this.HintElementType,
                this.InitialValues.Select(value => value.Create()));
        }
    }
}
