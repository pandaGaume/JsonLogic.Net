using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace BlueForest.JsonLogic
{
    public static class TypeExtensions
    {
        const BindingFlags PublicInstanceDeclaredOnly = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        internal static Type JsonNumberType = typeof(double);
        internal static TypeCode JsonNumberTypeCode = Type.GetTypeCode(JsonNumberType);
        internal static bool IsNumericType(this object o)
        {
            return IsNumericType(o.GetType());
        }
        internal static bool IsNumericType(this Expression expr)
        {
            return IsNumericType(expr.Type);
        }
        internal static bool IsNumericType(this Type t)
        {
            return Type.GetTypeCode(t) switch
            {
                TypeCode.Double or 
                TypeCode.Int32 or 
                TypeCode.Single or 
                TypeCode.Byte or 
                TypeCode.SByte or 
                TypeCode.UInt16 or 
                TypeCode.UInt32 or 
                TypeCode.UInt64 or 
                TypeCode.Int16 or 
                TypeCode.Int64 or 
                TypeCode.Decimal => true,
                _ => false,
            };
        }

        internal static bool IsJsonNumberType(this Type t) => Type.GetTypeCode(t) == JsonNumberTypeCode;
        internal static bool IsIntegerType(this Type t) => Type.GetTypeCode(t) == TypeCode.Int32;

        internal static PropertyInfo? GetPublicInstanceDeclaredOnlyReadProperty(this Type type, string name)
            => type.GetProperties(PublicInstanceDeclaredOnly)
                .FirstOrDefault(property => property.Name == name && property.GetGetMethod() is not null);
    }
}
