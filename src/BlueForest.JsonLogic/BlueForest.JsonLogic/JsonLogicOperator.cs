using System.Collections.Generic;
using System.Linq.Expressions;

namespace BlueForest.JsonLogic
{
    public class JsonLogicOperator
    {
        public static Expression And(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            Expression exp = Expression.AndAlso(values[0], values[1]);
            for (int i = 2; i != values.Count; i++)
            {
                exp = Expression.AndAlso(exp, values[i]);
            }
            return exp;
        }
        public static Expression Or(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            Expression exp = Expression.Or(values[0], values[1]);
            for (int i = 2; i != values.Count; i++)
            {
                exp = Expression.Or(exp, values[i]);
            }
            return exp;
        }
        public static Expression Equal(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.Equal(values[0], values[1]);
        }
        public static Expression NotEqual(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.NotEqual(values[0], values[1]);
        }
        public static Expression LessThan(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.LessThan(values[0], values[1]);
        }
        public static Expression LessThanOrEqual(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.LessThanOrEqual(values[0], values[1]);
        }
        public static Expression GreaterThan(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.GreaterThan(values[0], values[1]);
        }
        public static Expression GreaterThanOrEqual(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.GreaterThanOrEqual(values[0], values[1]);
        }
        public static Expression Add(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.AddChecked(values[0], values[1]);
        }
        public static Expression AddAssign(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.AddAssignChecked(values[0], values[1]);
        }
        public static Expression Subtract(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.SubtractChecked(values[0], values[1]);
        }
        public static Expression SubtractAssign(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.SubtractAssignChecked(values[0], values[1]);
        }
        public static Expression Multiply(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.MultiplyChecked(values[0], values[1]);
        }
        public static Expression MultiplyAssign(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.MultiplyAssignChecked(values[0], values[1]);
        }
        public static Expression Divide(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.Divide(values[0], values[1]);
        }
        public static Expression DivideAssign(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.DivideAssign(values[0], values[1]);
        }
        public static Expression Modulo(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.Modulo(values[0], values[1]);
        }
        public static Expression ModuloAssign(IList<Expression> values)
        {
            if (values.Count < 2) throw new JsonLogicException();
            return Expression.ModuloAssign(values[0], values[1]);
        }
    }
}
