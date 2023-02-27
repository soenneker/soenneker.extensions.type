using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Soenneker.Extensions.Type;

/// <summary>
/// Useful Type operations
/// </summary>
public static class TypeExtension
{
    [Pure]
    public static List<TFieldType> GetFieldsOfType<TFieldType>(this System.Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(p => type.IsAssignableFrom(p.FieldType))
            .Select(pi => (TFieldType)pi.GetValue(null)!)
            .ToList();
    }

    [Pure]
    public static IEnumerable<System.Type> GetInterfacesAndSelf(this System.Type type)
    {
        return (type ?? throw new ArgumentNullException()).IsInterface ? new[] { type }.Concat(type.GetInterfaces()) : type.GetInterfaces();
    }

    /// <summary>
    /// int, float, double etc are numeric
    /// </summary>
    [Pure]
    public static bool IsNumeric(this System.Type type)
    {
        var typeCode = (int)System.Type.GetTypeCode(type);
        return typeCode is > 4 and < 16;
    }

    /// <summary>
    /// Returns the Name property of a System.Text.Json JsonPropertyName. If it doesn't have one it will return propertyName.
    /// </summary>
    [Pure]
    public static string GetJsonPropertyName(this System.Type type, string propertyName)
    {
        string? result = type.GetProperty(propertyName)?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

        if (result == null)
            result = propertyName;

        return result;
    }
}