using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Soenneker.Enumerables.CommaSeparated;

namespace Soenneker.Extensions.Type;

public static class TypeExtension
{
    private static readonly FrozenDictionary<System.Type, Func<ReadOnlySpan<char>, object?>> _parsers =
        new Dictionary<System.Type, Func<ReadOnlySpan<char>, object?>>
        {
            [typeof(string)] = static s => s.ToString(),

            [typeof(int)] = static s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : null,
            [typeof(long)] = static s => long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long v) ? v : null,
            [typeof(short)] = static s => short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out short v) ? v : null,
            [typeof(ushort)] = static s => ushort.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort v) ? v : null,
            [typeof(uint)] = static s => uint.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint v) ? v : null,
            [typeof(ulong)] = static s => ulong.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong v) ? v : null,
            [typeof(byte)] = static s => byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte v) ? v : null,
            [typeof(sbyte)] = static s => sbyte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte v) ? v : null,

            [typeof(float)] = static s =>
                float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out float v) ? v : null,
            [typeof(double)] = static s => double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out double v)
                ? v
                : null,
            [typeof(decimal)] = static s => decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal v) ? v : null,

            [typeof(bool)] = static s => bool.TryParse(s, out bool v) ? v : null,

            [typeof(DateTime)] = static s => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime v) ? v : null,
            [typeof(DateTimeOffset)] = static s =>
                DateTimeOffset.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset v) ? v : null,

            [typeof(TimeSpan)] = static s => TimeSpan.TryParse(s, CultureInfo.InvariantCulture, out TimeSpan v) ? v : null,
            [typeof(Guid)] = static s => Guid.TryParse(s, out Guid v) ? v : null,

            [typeof(Uri)] = static s =>
            {
                // unavoidable string allocation
                var str = s.ToString();
                return Uri.TryCreate(str, UriKind.RelativeOrAbsolute, out Uri? uri) ? uri : null;
            },

            [typeof(char)] = static s => s.Length == 1 ? s[0] : null,

            [typeof(DateOnly)] = static s => DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly v) ? v : null,
            [typeof(TimeOnly)] = static s => TimeOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly v) ? v : null,
        }.ToFrozenDictionary();

    private static readonly ConcurrentDictionary<(System.Type, string), string> _jsonNameCache = new();

    // Cache: elementType -> (capacity -> IList)
    private static readonly ConcurrentDictionary<System.Type, Func<int, IList>> _listFactoryCache = new();

    [Pure]
    public static List<TFieldType> GetFieldsOfType<TFieldType>(this System.Type type)
    {
        FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        var result = new List<TFieldType>(fields.Length);
        System.Type target = typeof(TFieldType);

        foreach (FieldInfo field in fields)
        {
            if (target.IsAssignableFrom(field.FieldType))
                result.Add((TFieldType)field.GetValue(null)!);
        }

        return result;
    }

    [Pure]
    public static IEnumerable<System.Type> GetInterfacesAndSelf(this System.Type type)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (!type.IsInterface)
            return type.GetInterfaces();

        System.Type[] interfaces = type.GetInterfaces();
        var result = new System.Type[interfaces.Length + 1];
        result[0] = type;
        Array.Copy(interfaces, 0, result, 1, interfaces.Length);
        return result;
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsNumeric(this System.Type type)
    {
        var typeCode = (int)System.Type.GetTypeCode(type);
        return typeCode is > 4 and < 16;
    }

    [Pure]
    public static string GetJsonPropertyName(this System.Type type, string propertyName)
    {
        if (type is null)
            throw new ArgumentNullException(nameof(type));

        if (propertyName is null)
            throw new ArgumentNullException(nameof(propertyName));

        return _jsonNameCache.GetOrAdd((type, propertyName), static key =>
        {
            string? name = key.Item1.GetProperty(key.Item2)
                              ?.GetCustomAttribute<JsonPropertyNameAttribute>()
                              ?.Name;

            return name ?? key.Item2;
        });
    }

    [Pure]
    public static object? ConvertPropertyValue(this System.Type targetType, string value) =>
        ConvertPropertyValueCore(targetType, value.AsSpan());

    [Pure]
    private static object? ConvertPropertyValueCore(System.Type? targetType, ReadOnlySpan<char> value)
    {
        if (targetType is null)
            return null;

        value = value.Trim();

        // Nullable<T>
        if (Nullable.GetUnderlyingType(targetType) is { } underlying)
        {
            if (value.IsEmpty)
                return null;

            targetType = underlying;
        }

        // --------------------------------------------------------
        // Array
        // --------------------------------------------------------
        if (targetType.IsArray)
        {
            System.Type? elementType = targetType.GetElementType();
            if (elementType is null)
                return null;

            var csv = new CommaSeparatedEnumerable(value);

            int count = csv.Count();
            var array = Array.CreateInstance(elementType, count);

            var i = 0;
            foreach (ReadOnlySpan<char> token in csv)
                array.SetValue(ConvertPropertyValueCore(elementType, token), i++);

            return array;
        }

        // --------------------------------------------------------
        // List<T>
        // --------------------------------------------------------
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
        {
            System.Type elementType = targetType.GetGenericArguments()[0];

            var csv = new CommaSeparatedEnumerable(value);
            int count = csv.Count();

            Func<int, IList> factory = _listFactoryCache.GetOrAdd(elementType, static et => CreateListFactory(et));

            IList list = factory(count);

            foreach (ReadOnlySpan<char> token in csv)
                list.Add(ConvertPropertyValueCore(elementType, token));

            return list;
        }

        // --------------------------------------------------------
        // Enum
        // --------------------------------------------------------
        if (targetType.IsEnum)
        {
            var s = value.ToString(); // unavoidable
            return Enum.TryParse(targetType, s, ignoreCase: true, out object? e) ? e : null;
        }

        // --------------------------------------------------------
        // Fast primitives
        // --------------------------------------------------------
        if (_parsers.TryGetValue(targetType, out Func<ReadOnlySpan<char>, object?>? parser))
            return parser(value);

        return null;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static Func<int, IList> CreateListFactory(System.Type elementType)
    {
        System.Type listType = typeof(List<>).MakeGenericType(elementType);

        // Prefer List<T>(int capacity)
        ConstructorInfo? capCtor = listType.GetConstructor([typeof(int)]);
        if (capCtor is not null)
            return CompileCapacityCtor(capCtor);

        // Fallback: parameterless
        ConstructorInfo? ctor = listType.GetConstructor(System.Type.EmptyTypes);
        if (ctor is null)
            throw new InvalidOperationException($"Could not find a usable constructor for {listType}.");

        return CompileParameterlessCtor(ctor);
    }

    private static Func<int, IList> CompileCapacityCtor(ConstructorInfo ctor)
    {
        // (int cap) => (IList)new List<T>(cap)
        ParameterExpression cap = Expression.Parameter(typeof(int), "cap");
        NewExpression @new = Expression.New(ctor, cap);
        UnaryExpression cast = Expression.Convert(@new, typeof(IList));
        return Expression.Lambda<Func<int, IList>>(cast, cap)
                         .Compile();
    }

    private static Func<int, IList> CompileParameterlessCtor(ConstructorInfo ctor)
    {
        // (_cap) => (IList)new List<T>()
        ParameterExpression cap = Expression.Parameter(typeof(int), "cap");
        NewExpression @new = Expression.New(ctor);
        UnaryExpression cast = Expression.Convert(@new, typeof(IList));
        return Expression.Lambda<Func<int, IList>>(cast, cap)
                         .Compile();
    }
}