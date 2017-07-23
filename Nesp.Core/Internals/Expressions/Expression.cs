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
using System.Reflection;

using RawExpression = System.Linq.Expressions.Expression;

namespace Nesp.Internals.Expressions
{
    internal static class ExpressionExtension
    {
        public static RawExpression Create(this Expression expression)
        {
            return expression?.InternalCreate();
        }
    }

    internal abstract class Expression
    {
        private RawExpression cache;

        protected Expression()
        {
        }

        public abstract Type CandidateType { get; }

        protected abstract RawExpression OnCreate();

        internal RawExpression InternalCreate()
        {
            return cache ?? (cache = this.OnCreate());
        }

        public static ConstantExpression Constant(object value)
        {
            return new ConstantExpression(value);
        }

        public static ConvertExpression Convert(Expression expression, Type targetType)
        {
            return new ConvertExpression(expression, targetType);
        }

        public static FieldExpression Field(Expression instance, FieldInfo fi)
        {
            return new FieldExpression(instance, fi);
        }

        public static PropertyExpression Property(Expression instance, PropertyInfo pi)
        {
            return new PropertyExpression(instance, pi);
        }

        public static MethodCallExpression Call(Expression instance, MethodInfo mi, IEnumerable<Expression> parameters)
        {
            return new MethodCallExpression(instance, mi, parameters);
        }

        public static ParameterExpression Parameter(Type hintType, string name)
        {
            return new ParameterExpression(hintType, name);
        }

        public static NewArrayExpression NewArrayInit(Type elementType, IEnumerable<Expression> initialValues)
        {
            return new NewArrayExpression(elementType, initialValues);
        }

        public static LambdaExpression Lambda(Expression expression, string name, IEnumerable<ParameterExpression> parameters)
        {
            return new LambdaExpression(expression, name, parameters);
        }

        public static LambdaExpression<TDelegate> Lambda<TDelegate>(Expression expression, string name, IEnumerable<ParameterExpression> parameters)
            where TDelegate : class
        {
            return new LambdaExpression<TDelegate>(expression, name, parameters);
        }
    }

    internal sealed class ConstantExpression : Expression
    {
        public ConstantExpression(object value)
        {
            this.Value = value;
        }

        public override Type CandidateType => this.Value?.GetType();

        public object Value { get; }

        protected override RawExpression OnCreate()
        {
            return RawExpression.Constant(this.Value);
        }
    }

    internal sealed class ConvertExpression : Expression
    {
        public ConvertExpression(Expression operand, Type targetType)
        {
            this.CandidateType = targetType;
            this.Operand = operand;
        }

        public override Type CandidateType { get; }

        public Expression Operand { get; }

        protected override RawExpression OnCreate()
        {
            return RawExpression.Convert(this.Operand.Create(), this.CandidateType);
        }
    }

    internal sealed class FieldExpression : Expression
    {
        public FieldExpression(Expression instance, FieldInfo fi)
        {
            this.Instance = instance;
            this.Field = fi;
        }

        public override Type CandidateType => this.Field.FieldType;

        public Expression Instance { get; }
        public new FieldInfo Field { get; }

        protected override RawExpression OnCreate()
        {
            return RawExpression.Field(this.Instance.Create(), this.Field);
        }
    }

    internal sealed class PropertyExpression : Expression
    {
        public PropertyExpression(Expression instance, PropertyInfo pi)
        {
            this.Instance = instance;
            this.Property = pi;
        }

        public override Type CandidateType => this.Property.PropertyType;

        public Expression Instance { get; }
        public new PropertyInfo Property { get; }

        protected override RawExpression OnCreate()
        {
            return RawExpression.Property(this.Instance.Create(), this.Property);
        }
    }

    internal sealed class MethodCallExpression : Expression
    {
        public MethodCallExpression(Expression instance, MethodInfo mi, IEnumerable<Expression> arguments)
        {
            this.Instance = instance;
            this.Method = mi;
            this.Arguments = arguments;
        }

        public override Type CandidateType => this.Method.ReturnType;

        public Expression Instance { get; }
        public MethodInfo Method { get; }
        public IEnumerable<Expression> Arguments { get; }

        protected override RawExpression OnCreate()
        {
            return RawExpression.Call(
                this.Instance.Create(),
                this.Method,
                this.Arguments.Select(argExpr => argExpr.Create()));
        }
    }

    internal sealed class ParameterExpression : Expression
    {
        private Type candidateType;

        public ParameterExpression(Type hintType, string name)
        {
            candidateType = hintType;
            this.Name = name;
        }

        public override Type CandidateType => candidateType;

        public string Name { get; }

        public void UpdateCandidateType(Type type)
        {
            candidateType = type;
        }

        protected override RawExpression OnCreate()
        {
            return RawExpression.Parameter(this.CandidateType, this.Name);
        }
    }

    internal sealed class NewArrayExpression : Expression
    {
        public NewArrayExpression(Type hintElementType, IEnumerable<Expression> initialValues)
        {
            this.HintElementType = hintElementType;
            this.InitialValues = initialValues;
        }

        public override Type CandidateType => this.HintElementType.MakeArrayType();

        public Type HintElementType { get; }
        public IEnumerable<Expression> InitialValues { get; }

        protected override RawExpression OnCreate()
        {
            return RawExpression.NewArrayInit(
                this.HintElementType,
                this.InitialValues.Select(value => value.Create()));
        }
    }

    internal class LambdaExpression : Expression
    {
        public LambdaExpression(Expression expression, string name, IEnumerable<ParameterExpression> parameters)
        {
            this.Expression = expression;
            this.Name = name;
            this.Parameters = parameters;
        }

        public override Type CandidateType => this.Expression.CandidateType;

        public Expression Expression { get; }
        public string Name { get; }
        public IEnumerable<ParameterExpression> Parameters { get; }

        protected override RawExpression OnCreate()
        {
            // TODO: Support tailcall recursion
            return RawExpression.Lambda(
                this.Expression.Create(),
                this.Name,
                this.Parameters
                    .Select(argExpr => (System.Linq.Expressions.ParameterExpression)argExpr.Create())
                    .ToArray());
        }
    }

    internal sealed class LambdaExpression<TDelegate> : LambdaExpression
        where TDelegate : class
    {
        private TDelegate cache;

        public LambdaExpression(Expression expression, string name, IEnumerable<ParameterExpression> parameters)
            : base(expression, name, parameters)
        {
        }

        protected override RawExpression OnCreate()
        {
            // TODO: Support tailcall recursion
            return RawExpression.Lambda<TDelegate>(
                this.Expression.Create(),
                this.Name,
                this.Parameters
                    .Select(argExpr => (System.Linq.Expressions.ParameterExpression)argExpr.Create())
                    .ToArray());
        }

        public TDelegate Compile()
        {
            if (cache == null)
            {
                var lambdaExpr = (System.Linq.Expressions.Expression<TDelegate>)this.Create();
                cache = lambdaExpr.Compile();
            }
            return cache;
        }
    }
}
