// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.EventSources;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventSources;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL event source representations.
/// </summary>
public static class EventSourceDefinitionConverters
{
    static readonly JsonSerializerOptions _jsonOptions = new();

    /// <summary>
    /// Converts a Kernel definition to its SQL entity.
    /// </summary>
    /// <param name="definition">The Kernel definition.</param>
    /// <returns>The SQL entity.</returns>
    public static EventSourceDefinition ToSql(this Concepts.EventSources.EventSourceDefinition definition) =>
        new()
        {
            Id = definition.Name.Value,
            Description = definition.Description.Value,
            Owner = (int)definition.Owner,
            Concurrency = (int)definition.Concurrency,
            Streams = JsonSerializer.Serialize(
                definition.Streams.Select(stream => new StoredStream(stream.Name.Value, stream.Description.Value, (int)stream.Concurrency)),
                _jsonOptions)
        };

    /// <summary>
    /// Converts a SQL entity to a Kernel definition.
    /// </summary>
    /// <param name="definition">The SQL entity.</param>
    /// <returns>The Kernel definition.</returns>
    public static Concepts.EventSources.EventSourceDefinition ToKernel(this EventSourceDefinition definition) =>
        new(
            definition.Id,
            definition.Description ?? string.Empty,
            (EventSourceOwner)definition.Owner,
            (ConcurrencyDimensions)definition.Concurrency,
            [.. (JsonSerializer.Deserialize<IEnumerable<StoredStream>>(definition.Streams ?? "[]", _jsonOptions) ?? [])
                .Select(stream => new Concepts.EventSources.EventStreamDefinition(stream.Name, stream.Description, (ConcurrencyDimensions)stream.Concurrency))]);

    sealed record StoredStream(string Name, string Description, int Concurrency);
}
