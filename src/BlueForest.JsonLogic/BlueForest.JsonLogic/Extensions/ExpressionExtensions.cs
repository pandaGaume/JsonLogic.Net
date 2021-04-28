using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BlueForest.JsonLogic
{
    public static class ExpressionExtensions
    {
        internal static Expression EnsureJsonNumber(this Expression e) => e.Type.IsJsonNumberType() ? e : e.Type.IsNumericType() ? Expression.Convert(e, TypeExtensions.JsonNumberType) : throw new JsonLogicException();
        internal static Expression EnsureIndexNumber(this Expression e) => e.Type.IsNumericType() ? e.Type.IsIntegerType()? e : Expression.Convert(e, typeof(int)) : throw new JsonLogicException();
        internal static IEnumerable<Expression> EnsureJsonNumber(this IEnumerable<Expression> expressions) => expressions.Select(e => e.EnsureJsonNumber());

    }
}
