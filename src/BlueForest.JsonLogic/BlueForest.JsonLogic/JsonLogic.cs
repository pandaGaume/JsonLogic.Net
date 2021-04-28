using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace BlueForest.JsonLogic
{
    using OpFactory = Func<IList<Expression>, Stack<Expression>, Expression>;
    using OpRegistry = Dictionary<string, Func<IList<Expression>, Stack<Expression>, Expression>>;

    public class JsonLogicException : Exception
    {
        public JsonLogicException(string mess = null) : base(mess) { }
        public JsonLogicException(string mess, params object[] args) : base(mess != null ? string.Format(mess, args) : null) { }
    }

    public class JsonLogicOptions
    {
        public IDictionary<string,string> Aliases { get; set; }
    }

    public class JsonLogic
    {
        internal static OpRegistry KnownOpFactories = new OpRegistry()
        {
            { "and", JsonLogicOperator.And                  },
            { "or" , JsonLogicOperator.Or                   },
            { "==" , JsonLogicOperator.Equal                },
            { "===", JsonLogicOperator.Equal                },
            { "!=" , JsonLogicOperator.NotEqual             },
            { "!==", JsonLogicOperator.NotEqual             },
            { "<"  , JsonLogicOperator.LessThan             },
            { ">"  , JsonLogicOperator.GreaterThan          },
            { "<="  , JsonLogicOperator.LessThanOrEqual     },
            { ">="  , JsonLogicOperator.GreaterThanOrEqual  },
            { "+"  , JsonLogicOperator.Add                  },
            { "-"  , JsonLogicOperator.Subtract             },
            { "*"  , JsonLogicOperator.Multiply             },
            { "/"  , JsonLogicOperator.Divide               },
            { "%"  , JsonLogicOperator.Modulo               },
            { "var", JsonLogicOperator.Var                  },
            { "min", JsonLogicOperator.Min                  },
            { "max", JsonLogicOperator.Max                  }
        };

        internal static OpRegistry KnownQueryFactories = new OpRegistry()
        {
            { "all" , JsonLogicOperator.All                 },
            { "some", JsonLogicOperator.Some                },
            { "any" , JsonLogicOperator.Some                }
        };

        public static LambdaExpression Parse(string jsonStr, Type dataType, JsonLogicOptions options = null)=> Parse<object>(Encoding.UTF8.GetBytes(jsonStr), dataType, options);

        public static LambdaExpression Parse<ReturnT>(string jsonStr, Type dataType,JsonLogicOptions options = null)=> Parse<ReturnT>(Encoding.UTF8.GetBytes(jsonStr), dataType, options);

        public static LambdaExpression Parse(ReadOnlySpan<byte> jsonData, Type dataType, JsonLogicOptions options = null)=>  Parse<object>(jsonData, dataType, options);
 
        public static LambdaExpression Parse<ReturnT>(ReadOnlySpan<byte> jsonData, Type dataType, JsonLogicOptions options = null)
        {
            var reader = new Utf8JsonReader(jsonData);
            if (!reader.Read()) throw new JsonException();
            var stack = new Stack<Expression>(1);
            var dataExpr = Expression.Parameter(dataType ?? typeof(object), "data");
            stack.Push(dataExpr);
            var bodyExpr = Parse(ref reader, stack, options);
            return Expression.Lambda(Expression.Convert(bodyExpr, typeof(ReturnT)), dataExpr);
        }

        internal static Expression Parse(ref Utf8JsonReader reader, Stack<Expression> stack, JsonLogicOptions options = null)
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
                                    OpFactory factory = default;
                                    // retreive standard operations
                                    if (KnownOpFactories.TryGetValue(opName, out factory))
                                    {
                                        var values = ParseOpParameters(ref reader, stack);
                                        exp = factory?.Invoke(values, stack);
                                        break;
                                    }
                                    // then operations on Array. We must parse it in different way, because
                                    // underlying predicate need ExpressionParameter to be passed instead as 
                                    // data.
                                    if (KnownQueryFactories.TryGetValue(opName, out factory))
                                    {
                                        var values = ParseQueryParameters(ref reader, stack);
                                        exp = factory?.Invoke(values, stack);
                                        break;
                                    }

                                    throw new JsonLogicException($"Unknown operation {opName}");
                                }
                            case JsonTokenType.String:
                                {
                                    // using Syntactic sugar such "var"="xxx" instead as "var" = ["xxx"]
                                    switch (opName)
                                    {
                                        case "var":
                                            {
                                                exp = GetPropertyGetter(reader.GetString(), stack);
                                                break;
                                            }
                                        default: throw new JsonLogicException();
                                    }
                                    break;
                                }
                            case JsonTokenType.Number:
                                {
                                    // using Syntactic sugar such "var"=n instead as "var" = [n]
                                    switch (opName)
                                    {
                                        case "var":
                                            {
                                                exp = Expression.ArrayAccess(stack.Peek(), Expression.Constant(reader.GetInt32()));
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
                        exp = Expression.Constant((object)value);
                        break;
                    }
                case JsonTokenType.Number:
                    {
                        var value = reader.GetDouble();
                        exp = Expression.Constant((object)value);
                        break;
                    }
                case JsonTokenType.StartArray:
                    {
                        exp = ParseArray(ref reader);
                        break;
                    }
                default: throw new JsonException();
            }
            return exp;
        }

        internal static IList<Expression> ParseOpParameters(ref Utf8JsonReader reader, Stack<Expression> parameters)
        {
            if (!reader.Read()) throw new JsonException();
            var l = new List<Expression>(2);
            while (reader.TokenType != JsonTokenType.EndArray)
            {
                l.Add(Parse(ref reader, parameters));
                if (!reader.Read()) throw new JsonException();
            }
            return l;
        }

        internal static IList<Expression> ParseQueryParameters(ref Utf8JsonReader reader, Stack<Expression> stack)
        {
            if (!reader.Read()) throw new JsonException();
            var l = new List<Expression>(2);
            if(reader.TokenType != JsonTokenType.EndArray)
            {
                var arrayExpr = Parse(ref reader, stack);
                if (!arrayExpr.Type.IsArray) throw new JsonLogicException();
                l.Add(arrayExpr);
                var etype = arrayExpr.Type.GetElementType();
                if (!reader.Read()) throw new JsonException();
                var predicateParameter = Expression.Parameter(etype, "p");
                stack.Push(predicateParameter);
                var predicateExpr = Parse(ref reader, stack);
                stack.Pop();
                l.Add(predicateExpr);
                if (!reader.Read()) throw new JsonException();
                if (reader.TokenType != JsonTokenType.EndArray) throw new JsonLogicException();
            }
            return l;
        }

        internal static Expression ParseArray(ref Utf8JsonReader reader)
        {
            if (!reader.Read()) throw new JsonException();
            if(reader.TokenType != JsonTokenType.EndArray)
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.String: return ParseStringArray(ref reader);
                    case JsonTokenType.Number: return ParseNumberArray(ref reader);
                    default: throw new JsonLogicException("Array items must be of type string or number.");
                }
            }
            return Expression.Constant(Array.Empty<object>());
        }

        internal static Expression ParseStringArray(ref Utf8JsonReader reader)
        {
            List<string> list = new List<string>(8);
            do
            {
                if (reader.TokenType != JsonTokenType.String) throw new JsonLogicException("Array items must be of type string or number.");
                list.Add(reader.GetString());
                if (!reader.Read()) throw new JsonException();
            } while (reader.TokenType != JsonTokenType.EndArray);
            return Expression.Constant(list.ToArray());
        }

        internal static Expression ParseNumberArray(ref Utf8JsonReader reader)
        {
            List<double> list = new List<double>(8);
            do
            {
                if (reader.TokenType != JsonTokenType.Number) throw new JsonLogicException("Array items must be of type string or number.");
                list.Add(reader.GetDouble());
                if (!reader.Read()) throw new JsonException();
            } while (reader.TokenType != JsonTokenType.EndArray);
            return Expression.Constant(list.ToArray());
        }

        internal static Expression GetPropertyGetter(string propertyName, Stack<Expression> stack)
        {
            // special case where we must return the whole object
            if (string.IsNullOrEmpty(propertyName))
            {
                return stack.Peek();
            }

            string[] parts = propertyName.Split('.');
            MemberExpression exp = default;
            foreach (var e in stack)
            {
                try
                {
                    var name = parts[0];
                    exp = Expression.PropertyOrField(e, name);
                    for (int i = 1; i != parts.Length; i++)
                    {
                        exp = Expression.PropertyOrField(exp, parts[i]);
                    }
                }
                catch (ArgumentException)
                {
                    exp = default;
                }
            }
            if( exp == default)
            {
                throw new JsonLogicException("Property '{0}' not found.", propertyName);
            }
            if (exp.Type.IsJsonNumberType() || exp.Type == typeof(string))
            {
                return exp;
            }
            if(exp.IsNumericType())
            {
                return exp.EnsureJsonNumber();
            }
            return exp.Type != typeof(object) ? Expression.Convert(exp, typeof(Object)) : exp;
        }
    }
}
