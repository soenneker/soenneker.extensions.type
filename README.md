[![](https://img.shields.io/nuget/v/Soenneker.Extensions.Type.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Type/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.type/publish-package.yml?style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.type/actions/workflows/publish-package.yml)
[![](https://img.shields.io/nuget/dt/Soenneker.Extensions.Type.svg?style=for-the-badge)](https://www.nuget.org/packages/Soenneker.Extensions.Type/)
[![](https://img.shields.io/github/actions/workflow/status/soenneker/soenneker.extensions.type/codeql.yml?label=CodeQL&style=for-the-badge)](https://github.com/soenneker/soenneker.extensions.type/actions/workflows/codeql.yml)

# ![](https://user-images.githubusercontent.com/4441470/224455560-91ed3ee7-f510-4041-a8d2-3fc093025112.png) Soenneker.Extensions.Type
An extension library for useful Type operations.

## Installation

```bash
dotnet add package Soenneker.Extensions.Type
```

## Quick start

```csharp
using Soenneker.Extensions.Type;

// Given an existing System.Type named type:
var result = type.GetFieldsOfType();
```

## Common operations

- `GetFieldsOfType()` - Returns values from public static fields whose field type is assignable to `TFieldType`.
- `GetInterfacesAndSelf()` - Returns implemented interfaces for a class; for an interface, the sequence includes that interface followed by inherited interfaces.
- `IsNumeric()` - Returns `true` for CLR numeric primitive types based on their `TypeCode`.
- `GetJsonPropertyName()` - Returns a property's serialized name, honoring `JsonPropertyNameAttribute`; results are cached by type and property name.
- `ConvertPropertyValue()` - Converts property value.
