// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql.Converters;

/// <summary>
/// Converts a collection of values to and from a JSON string.
/// </summary>
/// /// <typeparam name="T">Type of item in collection.</typeparam>
public class EnumerableConverter<T> : ValueConverter<IEnumerable<T>, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EnumerableConverter{T}"/> class.
    /// </summary>
    public EnumerableConverter() : base(
        v => JsonSerializer.Serialize(v ?? Enumerable.Empty<T>(), (JsonSerializerOptions?)null),
        v => string.IsNullOrEmpty(v)
            ? Enumerable.Empty<T>()
            : JsonSerializer.Deserialize<IEnumerable<T>>(v, (JsonSerializerOptions?)null)!)
    {
    }
}
