using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Text.Json;

namespace BlueForest.JsonLogic
{
    using OpFactory = Func<IList<Expression>, Stack<Expression>, JsonLogicOptions, Expression>;
    using OpRegistry = Dictionary<string, Func<IList<Expression>, Stack<Expression>, JsonLogicOptions, Expression>>;

    public class JsonLogicException : Exception
    {
        public JsonLogicException(string mess = null) : base(mess) { }
        public JsonLogicException(string mess, params object[] args) : base(mess != null ? string.Format(mess, args) : null) { }
    }

    public class JsonLogicOptions
    {
        public const bool RelaxCamelDefault = true;

        IDictionary<string, string> _aliases = null;
        bool _rc = RelaxCamelDefault;

        /// <summary>
        /// Aliases for operations and queries name.
        /// </summary>
        public IDictionary<string,string> Aliases { get=>_aliases; set=>_aliases = value; }

        /// <summary>
        /// Get/Set the Case sensitivity of property names search.
        /// If true, properties name's are tested using the both version Uppercase and lower case of it first letter .
        /// For example, "nested" property will be search with "nested" and "Nested"
        /// This is usefull to keep the file json "compatible" with js and .Net naming conventions.
        /// Default is true.
        /// </summary>
        public bool RelaxCamel { get => _rc; set=>_rc = value; }
    }

    public static class TerminalVocabulary
    {
        public const string And                 = "and" ;
        public const string Or                  = "or"  ;
        public const string Equal               = "=="  ;
        public const string AbsoluteEqual       = "===" ;
        public const string NotEqual            = "!="  ;
        public const string AbsoluteNotEqual    = "!==" ;
        public const string LessThan            = "<"   ;
        public const string GreaterThan         = ">"   ;
        public const string LessThanOrEqual     = "<="  ;
        public const string GreaterThanOrEqual  = ">="  ;
        public const string Add                 = "+"   ;
        public const string Subtract            = "-"   ;
        public const string Multiply            = "*"   ;
        public const string Divide              = "/"   ;
        public const string Modulo              = "%"   ;
        public const string Min                 = "min" ;
        public const string Max                 = "max" ;

        public const string Var                 = "var" ;
        public const string All                 = "all" ;
        public const string Some                = "some";
        public const string Any                 = "any" ;
    }


    public class JsonLogic
    {
        internal static OpRegistry KnownOpFactories = new OpRegistry()
        {
            { TerminalVocabulary.And                , JsonLogicOperation.And                  },
            { TerminalVocabulary.Or                 , JsonLogicOperation.Or                   },
            { TerminalVocabulary.Equal              , JsonLogicOperation.Equal                },
            { TerminalVocabulary.AbsoluteEqual      , JsonLogicOperation.Equal                },
            { TerminalVocabulary.NotEqual           , JsonLogicOperation.NotEqual             },
            { TerminalVocabulary.AbsoluteNotEqual   , JsonLogicOperation.NotEqual             },
            { TerminalVocabulary.LessThan           , JsonLogicOperation.LessThan             },
            { TerminalVocabulary.GreaterThan        , JsonLogicOperation.GreaterThan          },
            { TerminalVocabulary.LessThanOrEqual    , JsonLogicOperation.LessThanOrEqual      },
            { TerminalVocabulary.GreaterThanOrEqual , JsonLogicOperation.GreaterThanOrEqual   },
            { TerminalVocabulary.Add                , JsonLogicOperation.Add                  },
            { TerminalVocabulary.Subtract           , JsonLogicOperation.Subtract             },
            { TerminalVocabulary.Multiply           , JsonLogicOperation.Multiply             },
            { TerminalVocabulary.Divide             , JsonLogicOperation.Divide               },
            { TerminalVocabulary.Modulo             , JsonLogicOperation.Modulo               },
            { TerminalVocabulary.Var                , JsonLogicOperation.Var                  },
            { TerminalVocabulary.Min                , JsonLogicOperation.Min                  },
            { TerminalVocabulary.Max                , JsonLogicOperation.Max                  }
        };

        internal static OpRegistry KnownQueryFactories = new OpRegistry()
        {
            { TerminalVocabulary.All , JsonLogicOperation.All  },
            { TerminalVocabulary.Some, JsonLogicOperation.Some },
            { TerminalVocabulary.Any , JsonLogicOperation.Some }
        };

        public static Delegate Compile(string jsonStr, Type dataType, JsonLogicOptions options = null) => Parse<object>(Encoding.UTF8.GetBytes(jsonStr), dataType, options).Compile();
        public static Delegate Compile<T>(string jsonStr, JsonLogicOptions options = null) => Parse<object>(Encoding.UTF8.GetBytes(jsonStr), typeof(T), options).Compile();
        public static Delegate Compile<ReturnT>(string jsonStr, Type dataType, JsonLogicOptions options = null) => Parse<ReturnT>(Encoding.UTF8.GetBytes(jsonStr), dataType, options).Compile();
        public static Delegate Compile<T,ReturnT>(string jsonStr, JsonLogicOptions options = null) => Parse<ReturnT>(Encoding.UTF8.GetBytes(jsonStr), typeof(T), options).Compile();
        public static Delegate Compile(ReadOnlySpan<byte> jsonData, Type dataType, JsonLogicOptions options = null) => Parse<object>(jsonData, dataType, options).Compile();
        public static Delegate Compile<T>(ReadOnlySpan<byte> jsonData, JsonLogicOptions options = null) => Parse<object>(jsonData, typeof(T), options).Compile();
        public static Delegate Compile<ReturnT>(ReadOnlySpan<byte> jsonData, Type dataType, JsonLogicOptions options = null) => Parse<ReturnT>(jsonData, dataType, options).Compile();
        public static Delegate Compile<T,ReturnT>(ReadOnlySpan<byte> jsonData, JsonLogicOptions options = null) => Parse<ReturnT>(jsonData, typeof(T), options).Compile();

        public static LambdaExpression Parse(string jsonStr, Type dataType, JsonLogicOptions options = null)=> Parse<object>(Encoding.UTF8.GetBytes(jsonStr), dataType, options);

        public static LambdaExpression Parse<ReturnT>(string jsonStr, Type dataType,JsonLogicOptions options = null)=> Parse<ReturnT>(Encoding.UTF8.GetBytes(jsonStr), dataType, options);

        public static LambdaExpression Parse(ReadOnlySpan<byte> jsonData, Type dataType, JsonLogicOptions options = null)=>  Parse<object>(jsonData, dataType, options);

        public static LambdaExpression Parse<ReturnT>(ReadOnlySpan<byte> jsonData, Type dataType, JsonLogicOptions options = null)
        {
            var reader = new Utf8JsonReader(jsonData);
            if (!reader.Read()) throw new JsonException();
            return Parse<ReturnT>(ref reader, dataType, options);
        }
        public static LambdaExpression Parse(ref Utf8JsonReader reader, Type dataType, JsonLogicOptions options = null) => Parse<object>(ref reader, dataType, options);
        public static LambdaExpression Parse<ReturnT>(ref Utf8JsonReader reader, Type dataType, JsonLogicOptions options = null)
        {
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
                                    // 1 - try to get a standard operations
                                    if (KnownOpFactories.TryGetValue(opName, out OpFactory factory))
                                    {
                                        var values = ParseOpParameters(ref reader, stack, options);
                                        exp = factory?.Invoke(values, stack, options);
                                        break;
                                    }
                                    // 2 - Else it's operations on Array. We must parse it in different way, because
                                    // underlying predicate need ExpressionParameter to be passed instead as 
                                    // data.
                                    if (KnownQueryFactories.TryGetValue(opName, out factory))
                                    {
                                        var values = ParseQueryParameters(ref reader, stack, options);
                                        exp = factory?.Invoke(values, stack, options);
                                        break;
                                    }

                                    throw new JsonLogicException($"Unknown operation {opName}");
                                }
                            case JsonTokenType.String:
                                {
                                    // using Syntactic sugar such "var"="xxx" instead as "var" = ["xxx"]
                                    switch (opName)
                                    {
                                        case TerminalVocabulary.Var:
                                            {
                                                exp = GetProperty(reader.GetString(), stack, options);
                                                break;
                                            }
                                        default: throw new JsonLogicException($"Syntax error.");
                                    }
                                    break;
                                }
                            case JsonTokenType.Number:
                                {
                                    // using Syntactic sugar such "var"=n instead as "var" = [n]
                                    switch (opName)
                                    {
                                        case TerminalVocabulary.Var:
                                            {
                                                exp = Expression.ArrayAccess(stack.Peek(), Expression.Constant(reader.GetInt32()));
                                                break;
                                            }
                                        default: throw new JsonLogicException($"Syntax error.");
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

        internal static IList<Expression> ParseOpParameters(ref Utf8JsonReader reader, Stack<Expression> parameters, JsonLogicOptions options)
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

        internal static IList<Expression> ParseQueryParameters(ref Utf8JsonReader reader, Stack<Expression> stack, JsonLogicOptions options)
        {
            if (!reader.Read()) throw new JsonException();
            var l = new List<Expression>(2);
            if(reader.TokenType != JsonTokenType.EndArray)
            {
                var arrayExpr = Parse(ref reader, stack);
                if (!arrayExpr.Type.IsArray) throw new JsonLogicException($"Querie's first parameter MUST be an array.");
                l.Add(arrayExpr);
                var etype = arrayExpr.Type.GetElementType();
                if (!reader.Read()) throw new JsonException();
                var predicateParameter = Expression.Parameter(etype, "p");
                stack.Push(predicateParameter);
                var predicateExpr = Parse(ref reader, stack);
                stack.Pop();
                l.Add(predicateExpr);
                if (!reader.Read()) throw new JsonException();
                if (reader.TokenType != JsonTokenType.EndArray) reader.Skip();
            }
            return l;
        }

        internal static Expression ParseArray(ref Utf8JsonReader reader)
        {
            if (!reader.Read()) throw new JsonException();
            if(reader.TokenType != JsonTokenType.EndArray)
            {
                return reader.TokenType switch
                {
                    JsonTokenType.String => ParseStringArray(ref reader),
                    JsonTokenType.Number => ParseNumberArray(ref reader),
                    _ => throw new JsonLogicException("Array items must be of type string or number."),
                };
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

        internal static Expression GetProperty(string propertyName, Stack<Expression> stack, JsonLogicOptions options)
        {
            // special case where we must return the whole object
            if (string.IsNullOrEmpty(propertyName))
            {
                return stack.Peek();
            }

            string[] parts = propertyName.Split('.');
            Expression exp = default;
            foreach (var e in stack)
            {
                try
                {
                    var name = parts[0];
                    exp = GetPropertyOrField(e, name, options);
                    for (int i = 1; i != parts.Length; i++)
                    {
                        exp = GetPropertyOrField(exp, parts[i], options);
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
            if (exp.Type.IsArray)
            {
                return exp;
            }
            if (exp.Type != typeof(object))
            {
                exp = Expression.Convert(exp, typeof(object));
            }
            return exp;
        }

        internal static MemberExpression GetPropertyOrField(Expression e, string name, JsonLogicOptions options)
        {
            try
            {
                return Expression.PropertyOrField(e, name);
            }
            catch
            {
                if (options?.RelaxCamel ?? JsonLogicOptions.RelaxCamelDefault)
                {
                    name = (Char.IsUpper(name[0]) ? Char.ToLowerInvariant(name[0]) : Char.ToUpperInvariant(name[0])) + name[1..];
                    return Expression.PropertyOrField(e, name);
                }
                throw;
            }
        }
    }
}
