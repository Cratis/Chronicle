// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql.Converters;

/// <summary>
/// Represents a value converter for converting dictionaries to and from JSON strings.
/// </summary>
/// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
/// <typeparam name="TValue">The type of the dictionary values.</typeparam>
public class DictionaryValueConverter<TKey, TValue> : ValueConverter<IDictionary<TKey, TValue>, string>
    where TKey : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryValueConverter{TKey, TValue}"/> class.
    /// </summary>
    public DictionaryValueConverter() : base(
        v => JsonSerializer.Serialize(v ?? new Dictionary<TKey, TValue>(), (JsonSerializerOptions?)null),
        v => string.IsNullOrEmpty(v)
            ? new Dictionary<TKey, TValue>()
            : JsonSerializer.Deserialize<Dictionary<TKey, TValue>>(v, (JsonSerializerOptions?)null)!)
    {
    }
}
