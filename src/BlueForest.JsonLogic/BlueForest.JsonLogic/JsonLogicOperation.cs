using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace BlueForest.JsonLogic
{
    public class JsonLogicOperation
    {
        public static Expression And(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            Expression exp = Expression.AndAlso(values[0], values[1]);
            for (int i = 2; i != values.Count; i++)
            {
                exp = Expression.AndAlso(exp, values[i]);
            }
            return exp;
        }
        public static Expression Or(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            Expression exp = Expression.Or(values[0], values[1]);
            for (int i = 2; i != values.Count; i++)
            {
                exp = Expression.Or(exp, values[i]);
            }
            return exp;
        }
        public static Expression Equal(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            if (values[0].Type.IsNumericType())
            {
                return Expression.Equal(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
            }
            return Expression.Equal(values[0], values[1]);
        }
        public static Expression NotEqual(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            if (values[0].Type.IsNumericType())
            {
                return Expression.Equal(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
            }
            return Expression.NotEqual(values[0], values[1]);
        }
        public static Expression LessThan(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            if (values.Count == 2)
            {
                return Expression.LessThan(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
            }

            // special case to test between
            var v0 = values[0].EnsureJsonNumber();
            var v1 = values[1].EnsureJsonNumber();
            var v2 = values[2].EnsureJsonNumber();
            var a = Expression.LessThan(v0, v1);
            var b = Expression.LessThan(v1, v2);
            return Expression.And(a, b);
        }
        public static Expression LessThanOrEqual(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            if (values.Count == 2)
            {
                return Expression.LessThanOrEqual(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
            }

            // special case to test between
            var v0 = values[0].EnsureJsonNumber();
            var v1 = values[1].EnsureJsonNumber();
            var v2 = values[2].EnsureJsonNumber();
            var a = Expression.LessThanOrEqual(v0, v1);
            var b = Expression.LessThanOrEqual(v1, v2);
            return Expression.And(a, b);
        }
        public static Expression GreaterThan(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.GreaterThan(values[0], values[1]);
        }
        public static Expression GreaterThanOrEqual(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.GreaterThanOrEqual(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
        }
        public static Expression Add(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.AddChecked(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
        }
        public static Expression Subtract(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.SubtractChecked(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
        }
        public static Expression Multiply(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.MultiplyChecked(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
        }
        public static Expression Divide(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.Divide(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
        }
        public static Expression Modulo(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.Modulo(values[0].EnsureJsonNumber(), values[1].EnsureJsonNumber());
        }
        public static Expression Min(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            return MinMax(values, options, false);
        }
        public static Expression Max(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            return MinMax(values, options, true);
        }
        public static Expression Var(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 1) throw new JsonLogicException();
            var a = values[0];
            if (a is ConstantExpression ce)
            {
                if (ce.Value is string str)
                {
                    try
                    {
                        return JsonLogic.GetProperty(str, stack, options);
                    }
                    catch (JsonLogicException)
                    {
                        if (values.Count > 1)
                        {
                            return values[1];
                        }
                        throw;
                    }
                }

                if (ce.Type.IsNumericType())
                {
                    var array = stack?.FirstOrDefault(e => e.Type.IsArray);
                    if (array != default)
                    {
                        return Expression.ArrayAccess(array, ce.EnsureIndexNumber());
                    }
                }
            }
            throw new JsonLogicException();
        }
        public static Expression All(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            if (!values[0].Type.IsArray) throw new JsonLogicException();

            Expression arrayExpr = values[0];
            Type etype = arrayExpr.Type.GetElementType();

            var indexVariable = Expression.Variable(typeof(int), "index");
            var lengthExp = Expression.Property(arrayExpr, typeof(Array).GetPublicInstanceDeclaredOnlyReadProperty(nameof(Array.Length))!);
            var result = Expression.Variable(typeof(bool), "result");
            var label = Expression.Label();
            var predicate = values[1] as BinaryExpression;
            // Parameter for the catch block
            var exception = Expression.Parameter(typeof(Exception));

            var expr = Expression.Block(
                new[] { indexVariable, predicate.Left as ParameterExpression, result },
                Expression.TryCatch(
                    Expression.Block(
                        Expression.Assign(result, Expression.Constant(true)),
                        Expression.Assign(indexVariable, Expression.Constant(0)),
#if DEBUG
                        ExpressionHelpers.DebugExpression(lengthExp),
#endif
                        Expression.Loop(
                            Expression.IfThenElse(
                                Expression.LessThan(indexVariable, lengthExp),
                                Expression.Block(
                                    Expression.Assign(predicate.Left, Expression.ArrayIndex(arrayExpr, indexVariable)),
#if DEBUG
                                    ExpressionHelpers.DebugExpression(predicate),
#endif
                                    Expression.IfThen(
                                        Expression.Not(predicate),
                                        Expression.Block(
                                            Expression.Assign(result, Expression.Constant(false)),
                                            Expression.Break(label)
                                        )
                                    ),
                                    Expression.PostIncrementAssign(indexVariable)
                                ),
                                Expression.Break(label)),
                         label)
                    ),
                    Expression.Catch(
                        exception
#if DEBUG
                        , ExpressionHelpers.DebugExpression(exception)
#endif
                   )
                ),
                result);

            return expr;
        }
        public static Expression Some(IList<Expression> values, Stack<Expression> stack = null, JsonLogicOptions options = null)
        {
            if (values.Count < 2) throw new JsonLogicException();
            if (!values[0].Type.IsArray) throw new JsonLogicException();

            Expression arrayExpr = values[0];
            Type etype = arrayExpr.Type.GetElementType();

            var indexVariable = Expression.Variable(typeof(int), "index");
            var lengthExp = Expression.Property(arrayExpr, typeof(Array).GetPublicInstanceDeclaredOnlyReadProperty(nameof(Array.Length))!);
            var result = Expression.Variable(typeof(bool), "result");
            var label = Expression.Label();
            var predicate = values[1] as BinaryExpression;
            // Parameter for the catch block
            var exception = Expression.Parameter(typeof(Exception));

            var expr = Expression.Block(
                new[] { indexVariable, predicate.Left as ParameterExpression, result },
                Expression.TryCatch(
                    Expression.Block(
                        Expression.Assign(result, Expression.Constant(false)),
                        Expression.Assign(indexVariable, Expression.Constant(0)),
#if DEBUG
                        ExpressionHelpers.DebugExpression(lengthExp),
#endif
                        Expression.Loop(
                            Expression.IfThenElse(
                                Expression.LessThan(indexVariable, lengthExp),
                                Expression.Block(
                                    Expression.Assign(predicate.Left, Expression.ArrayIndex(arrayExpr, indexVariable)),
#if DEBUG
                                    ExpressionHelpers.DebugExpression(predicate),
#endif
                                    Expression.IfThen(
                                        predicate,
                                        Expression.Block(
                                            Expression.Assign(result, Expression.Constant(true)),
                                            Expression.Break(label)
                                        )
                                    ),
                                    Expression.PostIncrementAssign(indexVariable)
                                ),
                                Expression.Break(label)),
                         label)
                    ),
                    Expression.Catch(
                        exception
#if DEBUG
                        ,ExpressionHelpers.DebugExpression(exception)
#endif
                   )
                ),
                result) ;

            return expr;
        }
        private static Expression MinMax(IList<Expression> values, JsonLogicOptions options, bool op )
        {
            Expression array = default;
            Type etype = default;

            switch (values.Count)
            {
                case 0: throw new JsonLogicException();
                case 1:
                    {
                        if (values[0].Type.IsArray)
                        {
                            array = values[0];
                            etype = array.Type.GetElementType();
                            break;
                        }
                        return values[0];
                    }
                case 2:
                    {
                        // fast track..
                        var a = values[0].EnsureJsonNumber();
                        var b = values[1].EnsureJsonNumber();
                        var p = Expression.Variable(a.Type);
                        return Expression.Block(a.Type,
                            new[] { p },
                            Expression.IfThenElse(Expression.LessThan(a, b),
                                                  Expression.Assign(p, op ? b : a),
                                                  Expression.Assign(p, op ? a : b)),
                            p);
                    }
                default:
                    {
                        if (!values.All(v => v is ConstantExpression ce && ce.Type.IsNumericType())) throw new JsonLogicException("Array must be of Number");
                        array = Expression.Constant(values.Cast<ConstantExpression>().Select<ConstantExpression,double>(v => (double)Convert.ChangeType(v.Value, typeof(double))).ToArray());
                        etype = typeof(double);
                        break;
                    }
            }
            var indexVariable = Expression.Variable(typeof(int), "index");
            var arrayValue = Expression.Variable(etype, "value");
            var result = Expression.Variable(etype, "result");
            var lengthExp = Expression.Property(array, typeof(Array).GetPublicInstanceDeclaredOnlyReadProperty(nameof(Array.Length))!);

            return Expression.Block(
                new[] { indexVariable, arrayValue, result },
                Expression.Assign(result, Expression.ArrayIndex(array, Expression.Constant(0))),
                ExpressionHelpers.For(
                    Expression.Assign(indexVariable, Expression.Constant(0)),
                    Expression.LessThan(indexVariable, lengthExp),
                    Expression.PostIncrementAssign(indexVariable),
                    Expression.Block(
                       Expression.Assign(arrayValue, Expression.ArrayIndex(array, indexVariable)),
                       Expression.IfThenElse(Expression.LessThan(result, arrayValue),
                                              Expression.Assign(result, op ? arrayValue : result),
                                              Expression.Assign(result, op ? result : arrayValue))
                    )
                ),
                result);
        }
    }
}
