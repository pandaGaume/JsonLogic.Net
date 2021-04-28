using System;
using System.Linq.Expressions;
using System.Reflection;

namespace BlueForest.JsonLogic
{
    public static class ExpressionHelpers
    {
#if DEBUG
        static readonly MethodInfo logMethod = typeof(ExpressionHelpers).GetMethod("Debug");

        public static void Debug(Object data) => Console.WriteLine(data);

        public static Expression DebugExpression(Expression expr)
        {
            return Expression.Call(logMethod, Expression.Convert(expr,typeof(object)));
        }
#endif
        public static Expression For(Expression initialization, Expression condition, Expression iterator, Expression body)
            => Expression.Block(
                initialization,
                While(
                    condition,
                    Expression.Block(
                        body,
                        iterator
                    )
                )
            );

        public static LoopExpression While(Expression condition, Expression body)
        {
            var label = Expression.Label();
            return Expression.Loop(
                Expression.IfThenElse(
                    condition,
                    body,
                    Expression.Break(label)
                ),
                label
            );
        }

    }
}
