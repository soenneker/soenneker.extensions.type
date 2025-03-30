using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;
using Soenneker.Extensions.String;

namespace Soenneker.Extensions.Type;

/// <summary>
/// Provides extension methods for the System.Type class, enhancing its functionality
/// with methods related to type introspection and reflection.
/// </summary>
public static class TypeExtension
{
    private static readonly Dictionary<System.Type, Func<string, object?>> Parsers = new()
    {
        [typeof(string)] = v => v,
        [typeof(int)] = v => int.TryParse(v, out int i) ? i : null,
        [typeof(long)] = v => long.TryParse(v, out long l) ? l : null,
        [typeof(short)] = v => short.TryParse(v, out short s) ? s : null,
        [typeof(ushort)] = v => ushort.TryParse(v, out ushort us) ? us : null,
        [typeof(uint)] = v => uint.TryParse(v, out uint ui) ? ui : null,
        [typeof(ulong)] = v => ulong.TryParse(v, out ulong ul) ? ul : null,
        [typeof(byte)] = v => byte.TryParse(v, out byte b) ? b : null,
        [typeof(sbyte)] = v => sbyte.TryParse(v, out sbyte sb) ? sb : null,
        [typeof(float)] = v => float.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out float f) ? f : null,
        [typeof(double)] = v => double.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out double d) ? d : null,
        [typeof(decimal)] = v => decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal dec) ? dec : null,
        [typeof(bool)] = v => bool.TryParse(v, out bool bo) ? bo : null,
        [typeof(DateTime)] = v => DateTime.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt) ? dt : null,
        [typeof(TimeSpan)] = v => TimeSpan.TryParse(v, CultureInfo.InvariantCulture, out TimeSpan ts) ? ts : null,
        [typeof(Guid)] = v => Guid.TryParse(v, out Guid g) ? g : null,
        [typeof(Uri)] = v => Uri.TryCreate(v, UriKind.RelativeOrAbsolute, out Uri? uri) ? uri : null,
        [typeof(char)] = v => v.Length == 1 ? v[0] : null,
        [typeof(DateOnly)] = v => DateOnly.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly dateOnly) ? dateOnly : null,
        [typeof(TimeOnly)] = v => TimeOnly.TryParse(v, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly timeOnly) ? timeOnly : null,
    };

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
                   .Select(pi => (TFieldType) pi.GetValue(null)!)
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

    /// <summary>
    /// Converts a string representation of a value into an object of the specified <paramref name="targetType"/>.
    /// </summary>
    /// <param name="targetType">The target type to convert the string value into. Supports primitive types, enums, arrays, lists, and nullable types.</param>
    /// <param name="value">The string value to be converted.</param>
    /// <returns>
    /// An object of the specified type if conversion succeeds; otherwise, <c>null</c>. 
    /// If <paramref name="targetType"/> is a nullable type and <paramref name="value"/> is null or whitespace, returns <c>null</c>.
    /// </returns>
    /// <remarks>
    /// Supported conversions include:
    /// <list type="bullet">
    ///   <item><description>Primitive types (int, float, bool, etc.)</description></item>
    ///   <item><description>Special types (Guid, Uri, DateTime, DateOnly, TimeOnly, TimeSpan, char)</description></item>
    ///   <item><description>Enums (case-insensitive)</description></item>
    ///   <item><description>Arrays and List&lt;T&gt; from comma-separated values</description></item>
    ///   <item><description>Nullable versions of all supported types</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// Convert a string to an integer:
    /// <code>
    /// int result = (int)typeof(int).ConvertPropertyValue("123")!;
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="targetType"/> is null.</exception>

    [Pure]
    public static object? ConvertPropertyValue(this System.Type targetType, string value)
    {
        if (targetType == null)
            return null;

        if (Nullable.GetUnderlyingType(targetType) is { } underlying)
        {
            if (value.IsNullOrWhiteSpace())
                return null;

            targetType = underlying;
        }

        // Array support (e.g. int[], string[])
        if (targetType.IsArray)
        {
            System.Type? elementType = targetType.GetElementType();
            if (elementType == null)
                return null;

            string[] rawItems = value.Split(',');
            int length = rawItems.Length;
            var array = Array.CreateInstance(elementType, length);

            for (var i = 0; i < length; i++)
            {
                string item = rawItems[i].Trim();
                object? converted = elementType.ConvertPropertyValue(item);
                array.SetValue(converted, i);
            }

            return array;
        }

        // List<T> support
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            System.Type elementType = targetType.GetGenericArguments()[0];
            string[] rawItems = value.Split(',');
            int length = rawItems.Length;

            System.Type listType = typeof(List<>).MakeGenericType(elementType);
            var list = (IList)Activator.CreateInstance(listType)!;

            for (var i = 0; i < length; i++)
            {
                string item = rawItems[i].Trim();
                object? converted = elementType.ConvertPropertyValue(item);
                list.Add(converted);
            }

            return list;
        }

        // Enum support
        if (targetType.IsEnum)
        {
            return Enum.TryParse(targetType, value.Trim(), ignoreCase: true, out object? enumValue) ? enumValue : null;
        }

        // Dictionary lookup
        if (Parsers.TryGetValue(targetType, out Func<string, object?>? parser))
            return parser(value);

        return null;
    }
}