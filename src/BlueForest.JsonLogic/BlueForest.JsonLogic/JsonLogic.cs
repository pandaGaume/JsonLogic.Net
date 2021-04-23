using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace BlueForest.JsonLogic
{
    public class JsonLogicException : Exception
    {
        public JsonLogicException(string mess = null) : base(mess) { }
        public JsonLogicException(string mess, params object[] args) : base(mess != null ? string.Format(mess, args) : null) { }
    }

    public class JsonLogic
    {
        public static Expression<Func<DataT, object>> Parse<DataT>(string jsonStr)
        {
            return Parse<DataT>(Encoding.UTF8.GetBytes(jsonStr));
        }
        public static Expression<Func<DataT, object>> Parse<DataT>(ReadOnlySpan<byte> jsonData)
        {
            return Parse<DataT, Object>(jsonData);
        }
        public static Expression<Func<DataT, ReturnT>> Parse<DataT,ReturnT>(string jsonStr)
        {
            return Parse<DataT,ReturnT> (Encoding.UTF8.GetBytes(jsonStr));
        }
        public static Expression<Func<DataT, ReturnT>> Parse<DataT,ReturnT>(ReadOnlySpan<byte> jsonData)
        {
            var reader = new Utf8JsonReader(jsonData);
            if (!reader.Read()) throw new JsonException();
            var type = Expression.Parameter(typeof(DataT), "t");
            return Expression.Lambda<Func<DataT, ReturnT>>(Expression.Convert(Parse(ref reader, type), typeof(ReturnT)), type);
        }
        internal static Expression Parse(ref Utf8JsonReader reader, ParameterExpression parameter)
        {
            Expression exp = default;
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                        var opName = reader.GetString()?.Trim();
                        if (!reader.Read()) throw new JsonException();
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.StartArray:
                                {
                                    // this migh be an expression of type { operation : [params...] }
                                    // parse each array item as expression 
                                    var values = ParseArray(ref reader, parameter);
                                    switch (opName)
                                    {
                                        case "and":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.AndAlso(values[0], values[1]);
                                                for (int i = 2; i != values.Count; i++)
                                                {
                                                    exp = Expression.AndAlso(exp, values[i]);
                                                }
                                                break;
                                            }
                                        case "or":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.Or(values[0], values[1]);
                                                for (int i = 2; i != values.Count; i++)
                                                {
                                                    exp = Expression.Or(exp, values[i]);
                                                }
                                                break;
                                            }
                                        case "==":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.Equal(values[0], values[1]);
                                                break;
                                            }
                                        case "!=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.NotEqual(values[0], values[1]);
                                                break;
                                            }
                                        case "<":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.LessThan(values[0], values[1]);
                                                break;
                                            }
                                        case "<=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.LessThanOrEqual(values[0], values[1]);
                                                break;
                                            }
                                        case ">":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.GreaterThan(values[0], values[1]);
                                                break;
                                            }
                                        case ">=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.GreaterThanOrEqual(values[0], values[1]);
                                                break;
                                            }
                                        case "+":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.AddChecked(values[0], values[1]);
                                                break;
                                            }
                                        case "+=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.AddAssignChecked(values[0], values[1]);
                                                break;
                                            }
                                        case "-":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.SubtractChecked(values[0], values[1]);
                                                break;
                                            }
                                        case "-=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.SubtractAssignChecked(values[0], values[1]);
                                                break;
                                            }
                                        case "*":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.MultiplyChecked(values[0], values[1]);
                                                break;
                                            }
                                        case "*=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.MultiplyAssignChecked(values[0], values[1]);
                                                break;
                                            }
                                        case "/":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.Divide(values[0], values[1]);
                                                break;
                                            }
                                        case "/=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.DivideAssign(values[0], values[1]);
                                                break;
                                            }
                                        case "%":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.Modulo(values[0], values[1]);
                                                break;
                                            }
                                        case "%=":
                                            {
                                                if (values.Count < 2) throw new JsonLogicException();
                                                exp = Expression.ModuloAssign(values[0], values[1]);
                                                break;
                                            }
                                        default: throw new JsonLogicException("Operator '{}' not supported", opName);
                                    }
                                    break;
                                }
                            case JsonTokenType.String:
                                {
                                    // this is where we grap a property from the data which MUST be an Object
                                    switch (opName)
                                    {
                                        case "var":
                                            {
                                                exp = GetPropertyGetter(reader.GetString(), parameter);
                                                break;
                                            }
                                        default: throw new JsonLogicException();
                                    }
                                    break;
                                }
                            case JsonTokenType.Number:
                                {
                                    // this is where we grap a value into the data which MUST be an array.
                                    switch (opName)
                                    {
                                        case "var":
                                            {
                                                exp = GetArrayAccess(reader.GetInt32(), parameter);
                                                break;
                                            }
                                        default: throw new JsonLogicException();
                                    }
                                    break;
                                }
                            default: throw new JsonException();
                        }
                        if (!reader.Read() || reader.TokenType != JsonTokenType.EndObject) throw new JsonException();
                        break;
                    }
                case JsonTokenType.String:
                    {
                        var value = reader.GetString();
                        exp = Expression.Constant(value);
                        break;
                    }
                case JsonTokenType.Number:
                    {
                        var value = reader.GetInt32();
                        exp = Expression.Constant(value);
                        break;
                    }
                case JsonTokenType.StartArray:
                    {
                        IList<Expression> data = ParseArray(ref reader, parameter);
                        exp = Expression.Constant(data);
                        break;
                    }
                default: throw new JsonException();
            }
            return exp;
        }

        internal static IList<Expression> ParseArray(ref Utf8JsonReader reader, ParameterExpression parameter)
        {
            if (!reader.Read()) throw new JsonException();
            var l = new List<Expression>(2);
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                l.Add(Parse(ref reader, parameter));
                if (!reader.Read()) throw new JsonException();
            }
            return l;
        }

        internal static Expression GetPropertyGetter(string propertyExp, ParameterExpression parameter)
        {
            string[] parts = propertyExp.Split('.');
            if (parts.Length == 0) throw new JsonLogicException();
            MemberExpression exp = default;
            try
            {
                var name = parts[0];
                exp = Expression.PropertyOrField(parameter, name);
                for (int i = 1; i != parts.Length; i++)
                {
                    exp = Expression.PropertyOrField(exp, parts[i]) ;
                }
            }
            catch (ArgumentException )
            {
                throw new JsonLogicException("Property '{0}' not found.", propertyExp);
            }
            return exp;
        }
        internal static Expression GetArrayAccess(int index, ParameterExpression parameter)
        {
            return default;
        }
    }
}
