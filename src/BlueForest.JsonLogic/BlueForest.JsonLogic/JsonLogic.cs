using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace BlueForest.JsonLogic
{
    using OpFactory = Func<IList<Expression>, Expression>;
    using OpRegistry = Dictionary<string, Func<IList<Expression>, Expression>>;

    public class JsonLogicException : Exception
    {
        public JsonLogicException(string mess = null) : base(mess) { }
        public JsonLogicException(string mess, params object[] args) : base(mess != null ? string.Format(mess, args) : null) { }
    }

    public class JsonLogicOptions
    {
        public OpRegistry CustomFactories { get; set; }
        public IDictionary<string,string> Aliases { get; set; }
    }

    public class JsonLogic
    {
        internal static OpRegistry KnownOpFactories = new OpRegistry()
        {
            { "and", JsonLogicOperator.And                  },
            { "or" , JsonLogicOperator.Or                   },
            { "==" , JsonLogicOperator.Equal                },
            { "!=" , JsonLogicOperator.NotEqual             },
            { "<"  , JsonLogicOperator.LessThan             },
            { "<=" , JsonLogicOperator.LessThanOrEqual      },
            { ">"  , JsonLogicOperator.GreaterThan          },
            { ">=" , JsonLogicOperator.GreaterThanOrEqual   },
            { "+"  , JsonLogicOperator.Add                  },
            { "+=" , JsonLogicOperator.AddAssign            },
            { "-"  , JsonLogicOperator.Subtract             },
            { "-=" , JsonLogicOperator.SubtractAssign       },
            { "*"  , JsonLogicOperator.Multiply             },
            { "*=" , JsonLogicOperator.MultiplyAssign       },
            { "/"  , JsonLogicOperator.Divide               },
            { "/=" , JsonLogicOperator.DivideAssign         },
            { "%"  , JsonLogicOperator.Modulo               },
            { "%=" , JsonLogicOperator.ModuloAssign         }
        };

        public static Expression<Func<DataT, object>> Parse<DataT>(string jsonStr, JsonLogicOptions options = null)
        {
            return Parse<DataT>(Encoding.UTF8.GetBytes(jsonStr), options);
        }
        public static Expression<Func<DataT, object>> Parse<DataT>(ReadOnlySpan<byte> jsonData, JsonLogicOptions options = null)
        {
            return Parse<DataT, Object>(jsonData, options);
        }
        public static Expression<Func<DataT, ReturnT>> Parse<DataT,ReturnT>(string jsonStr, JsonLogicOptions options = null)
        {
            return Parse<DataT,ReturnT> (Encoding.UTF8.GetBytes(jsonStr), options);
        }
        public static Expression<Func<DataT, ReturnT>> Parse<DataT,ReturnT>(ReadOnlySpan<byte> jsonData, JsonLogicOptions options = null)
        {
            var reader = new Utf8JsonReader(jsonData);
            if (!reader.Read()) throw new JsonException();
            var type = Expression.Parameter(typeof(DataT), "t");
            return Expression.Lambda<Func<DataT, ReturnT>>(Expression.Convert(Parse(ref reader, type, options), typeof(ReturnT)), type);
        }
        internal static Expression Parse(ref Utf8JsonReader reader, ParameterExpression parameter, JsonLogicOptions options = null)
        {
            Expression exp = default;
            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    {
                        if (!reader.Read() || reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
                        var opName = reader.GetString()?.Trim()?.ToLower();
                        if(options?.Aliases != null && options.Aliases.TryGetValue(opName, out string alias))
                        {
                            opName = alias;
                        }
                        if (!reader.Read()) throw new JsonException();
                        switch (reader.TokenType)
                        {
                            case JsonTokenType.StartArray:
                                {
                                    // this migh be an expression of type { operation : [params...] }
                                    // parse each array item as expression 
                                    var values = ParseArray(ref reader, parameter);

                                    // retreive standard operation
                                    if ( KnownOpFactories.TryGetValue(opName, out OpFactory factory))
                                    {
                                        exp = factory?.Invoke(values);
                                    }
                                    // then try to retreive custom operation if not found
                                    if (exp == default && options?.CustomFactories != null)
                                    {
                                        if (options.CustomFactories.TryGetValue(opName, out factory))
                                        {
                                            exp = factory?.Invoke(values);
                                        }
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
