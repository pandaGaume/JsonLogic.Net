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


    public partial class JsonLogic
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

        public static void Assemble(Utf8JsonWriter writer, Delegate value, JsonSerializerOptions options) => writer.WriteStringValue(string.Empty);
    }
}
