// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ITypeFormats"/>.
/// </summary>
public class TypeFormats : ITypeFormats
{
    readonly Dictionary<Type, string> _typesFormatInfo = new()
    {
        { typeof(short), "int16" },
        { typeof(int), "int32" },
        { typeof(uint), "uint32" },
        { typeof(long), "int64" },
        { typeof(ulong), "uint64" },
        { typeof(float), "float" },
        { typeof(double), "double" },
        { typeof(decimal), "decimal" },
        { typeof(byte), "byte" },
        { typeof(DateTime), "date-time" },
        { typeof(DateTimeOffset), "date-time-offset" },
        { typeof(DateOnly), "date" },
        { typeof(TimeOnly), "time" },
        { typeof(TimeSpan), "duration" },
        { typeof(Guid), "guid" }
    };

    /// <inheritdoc/>
    public bool IsKnown(Type type) => _typesFormatInfo.ContainsKey(type);

    /// <inheritdoc/>
    public bool IsKnown(string format)
    {
        format = StripNullable(format);
        return _typesFormatInfo.ContainsValue(format);
    }

    /// <inheritdoc/>
    public string GetFormatForType(Type type) => _typesFormatInfo[type];

    /// <inheritdoc/>
    public Type GetTypeForFormat(string format)
    {
        format = StripNullable(format);
        return _typesFormatInfo.First(_ => _.Value == format).Key;
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<Type, string> GetAllFormats() => _typesFormatInfo;

    /// <inheritdoc/>
    public IEnumerable<TypeFormatMetadata> GetAllFormatsMetadata()
    {
        var typeFormats = _typesFormatInfo.Select(kvp => new TypeFormatMetadata(
            GetJsonTypeForClrType(kvp.Key),
            kvp.Key,
            kvp.Value));

        return [
            ..new TypeFormatMetadata[]
            {
                new("boolean", typeof(bool), string.Empty),
                new("string", typeof(string), string.Empty),
            },
            ..typeFormats
        ];
    }

    static string GetJsonTypeForClrType(Type type)
    {
        if (type == typeof(string) || type == typeof(Guid) || type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) || type == typeof(DateOnly) || type == typeof(TimeOnly) ||
            type == typeof(TimeSpan))
        {
            return "string";
        }

        if (type == typeof(short) || type == typeof(int) || type == typeof(long) ||
            type == typeof(byte))
        {
            return "integer";
        }

        if (type == typeof(uint) || type == typeof(ulong))
        {
            return "integer";
        }

        if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
        {
            return "number";
        }

        if (type == typeof(bool))
        {
            return "boolean";
        }

        return "string";
    }

    static string StripNullable(string? format)
    {
        if (format?.EndsWith('?') ?? false)
        {
            format = format[..^1];
        }

        return format!;
    }
}
