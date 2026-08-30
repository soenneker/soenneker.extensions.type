[![](https://img.shields.io/nuget/v/Soenneker.Extensions.Type.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Type/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.type/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.type/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Extensions.Type.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Type/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.type/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.type/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.Type
Reflection and text-conversion helpers for runtime `Type` values.

## Installation

```bash
dotnet add package Soenneker.Extensions.Type
```

## Convert text to a target type

```csharp
using Soenneker.Extensions.Type;

object? port = typeof(int).ConvertPropertyValue("8080");
object? ids = typeof(Guid[]).ConvertPropertyValue("6f9619ff-8b86-d011-b42d-00cf4fc964ff, 7c9e6679-7425-40de-944b-e07fc1f90ae7");
```

`ConvertPropertyValue()` supports strings, numeric primitives, booleans, `DateTime`, `DateTimeOffset`, `TimeSpan`, `Guid`, `Uri`, `char`, `DateOnly`, `TimeOnly`, enums, nullable values, arrays, and `List<T>`. Parsing is invariant-culture and trims the input. Arrays and lists use comma-separated tokens.

Invalid scalar input returns `null`. A collection also returns `null` when a non-empty element cannot be converted, rather than silently substituting a default value. An empty token is valid as `null` only for `Nullable<T>` elements. Enum parsing is case-insensitive and, like `Enum.TryParse`, accepts numeric values even when the enum does not define them.

URI conversion accepts relative or absolute URIs. Date/time conversion parses text but does not apply an application-specific time-zone policy.

## Read JSON property names

```csharp
string jsonName = typeof(Order).GetJsonPropertyName(nameof(Order.OrderId));
```

`GetJsonPropertyName()` returns `JsonPropertyNameAttribute.Name` when the exact public CLR property has that attribute; otherwise it returns the input name. It does not apply a `JsonSerializerOptions.PropertyNamingPolicy`. Successful property lookups are cached with a bounded cache.

## Inspect types

- `GetFieldsOfType<T>()` reads public static fields, including inherited public static fields, whose declared field type is assignable to `T`.
- `GetInterfacesAndSelf()` returns implemented interfaces for a class. For an interface, it returns that interface first, followed by its inherited interfaces in reflection order.
- `IsNumeric()` recognizes the built-in CLR integer and floating-point types, including `decimal`. It does not unwrap nullable types or classify enums and user-defined numeric types as numeric.
