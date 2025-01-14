using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Soenneker.Extensions.Type;

/// <summary>
/// Provides extension methods for the System.Type class, enhancing its functionality
/// with methods related to type introspection and reflection.
/// </summary>
public static class TypeExtension
{
    /// <summary>
    /// Retrieves a list of fields of a specified type <typeparamref name="TFieldType"/> 
    /// from the target type, including public and static fields, considering the entire hierarchy.
    /// </summary>
    /// <typeparam name="TFieldType">The type of fields to search for within the target type.</typeparam>
    /// <param name="type">The type to search for fields in.</param>
    /// <returns>A list of fields of the specified type.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
    [Pure]
    public static List<TFieldType> GetFieldsOfType<TFieldType>(this System.Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(p => type.IsAssignableFrom(p.FieldType))
            .Select(pi => (TFieldType)pi.GetValue(null)!)
            .ToList();
    }

    /// <summary>
    /// Retrieves an enumerable of <see cref="System.Type"/> objects representing the interfaces 
    /// implemented by the target type and, if the target type is an interface itself, the target type.
    /// </summary>
    /// <param name="type">The type to retrieve interfaces from.</param>
    /// <returns>An enumerable of <see cref="System.Type"/> objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
    [Pure]
    public static IEnumerable<System.Type> GetInterfacesAndSelf(this System.Type type)
    {
        return (type ?? throw new ArgumentNullException()).IsInterface ? new[] { type }.Concat(type.GetInterfaces()) : type.GetInterfaces();
    }

    /// <summary>
    /// Determines whether the target type is a numeric type (e.g., int, float, double).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is numeric; otherwise, false.</returns>
    [Pure]
    public static bool IsNumeric(this System.Type type)
    {
        var typeCode = (int)System.Type.GetTypeCode(type);
        return typeCode is > 4 and < 16;
    }

    /// <summary>
    /// Retrieves the JsonPropertyName attribute value for a specified property of the target type.
    /// If the property does not have a JsonPropertyName attribute, returns the property name itself.
    /// </summary>
    /// <param name="type">The type containing the property.</param>
    /// <param name="propertyName">The name of the property to look for the JsonPropertyName attribute on.</param>
    /// <returns>The JsonPropertyName attribute value, or the property name if the attribute is not present.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> or <paramref name="propertyName"/> is null.</exception>
    [Pure]
    public static string GetJsonPropertyName(this System.Type type, string propertyName)
    {
        string? result = type.GetProperty(propertyName)?.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name;

        if (result is null)
            result = propertyName;

        return result;
    }
}