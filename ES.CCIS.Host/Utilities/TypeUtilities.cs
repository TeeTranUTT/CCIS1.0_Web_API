using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ES.CCIS.Host.Utilities
{
    public static class TypeUtilities
    {
        public static Dictionary<string, T> GetAllPublicConstantNameValues<T>(this Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                       .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
                       .ToDictionary(x => x.Name, x => (T)x.GetRawConstantValue());
        }
        public static List<T> GetAllPublicConstantValues<T>(this Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                       .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(T))
                       .Select(x => (T)x.GetRawConstantValue())
                       .ToList();
        }
    }
}