// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Events;

/// <summary>
/// Represents a <see cref="JsonConverterFactory"/> that produces <see cref="EventSourceIdJsonConverter{T}"/>
/// instances for any closed <see cref="EventSourceId{T}"/> type.
/// </summary>
public class EventSourceIdJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) =>
        typeToConvert.IsGenericType &&
        typeToConvert.GetGenericTypeDefinition() == typeof(EventSourceId<>);

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var typeArg = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(EventSourceIdJsonConverter<>).MakeGenericType(typeArg);
        return (Activator.CreateInstance(converterType) as JsonConverter)!;
    }
}
