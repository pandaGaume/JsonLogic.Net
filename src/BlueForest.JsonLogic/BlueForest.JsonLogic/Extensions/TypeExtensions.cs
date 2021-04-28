using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Double:
                case TypeCode.Int32:
                case TypeCode.Single:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                    return true;
                default:
                    return false;
            }
        }

        internal static bool IsJsonNumberType(this Type t) => Type.GetTypeCode(t) == JsonNumberTypeCode;
        internal static bool IsIntegerType(this Type t) => Type.GetTypeCode(t) == TypeCode.Int32;

        internal static PropertyInfo? GetPublicInstanceDeclaredOnlyReadProperty(this Type type, string name)
            => type.GetProperties(PublicInstanceDeclaredOnly)
                .FirstOrDefault(property => property.Name == name && property.GetGetMethod() is not null);
    }
}
