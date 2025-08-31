// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cratis.Chronicle.Storage.Sql.Converters;

/// <summary>
/// Represents a value converter for converting dictionaries to and from JSON strings.
/// </summary>
public class DictionaryValueConverter : ValueConverter<IDictionary<string, string>, string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryValueConverter"/> class.
    /// </summary>
    public DictionaryValueConverter() : base(
        v => JsonSerializer.Serialize(v ?? new Dictionary<string, string>(), (JsonSerializerOptions?)null),
        v => string.IsNullOrEmpty(v)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null)!)
    {
    }
}
