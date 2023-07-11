// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ITypeFormats"/>.
/// </summary>
public class TypeFormats : ITypeFormats
{
    readonly Dictionary<Type, string> _typesFormatInfo = new()
    {
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
        { typeof(Guid), "guid" }
    };

    /// <inheritdoc/>
    public bool IsKnown(Type type) => _typesFormatInfo.ContainsKey(type);

    /// <inheritdoc/>
    public bool IsKnown(string format) => _typesFormatInfo.ContainsValue(format);

    /// <inheritdoc/>
    public string GetFormatForType(Type type) => _typesFormatInfo[type];

    /// <inheritdoc/>
    public Type GetTypeForFormat(string format) => _typesFormatInfo.First(_ => _.Value == format).Key;
}
