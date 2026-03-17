// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Storage.Sql.EventStores.EventStoreSubscriptions;

/// <summary>
/// Provides extension methods for converting between Kernel and SQL <see cref="EventStoreSubscriptionDefinition"/> representations.
/// </summary>
public static class EventStoreSubscriptionDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition"/> to a SQL <see cref="EventStoreSubscriptionDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel event store subscription definition.</param>
    /// <returns>The SQL event store subscription definition.</returns>
    public static EventStoreSubscriptionDefinition ToSql(
        this Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition definition) =>
        new()
        {
            Id = definition.Identifier.Value,
            SourceEventStore = definition.SourceEventStore.Value,
            EventTypes = definition.EventTypes.ToArray()
        };

    /// <summary>
    /// Converts a SQL <see cref="EventStoreSubscriptionDefinition"/> to a Kernel <see cref="Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition"/>.
    /// </summary>
    /// <param name="definition">The SQL event store subscription definition.</param>
    /// <returns>The Kernel event store subscription definition.</returns>
    public static Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition ToKernel(
        this EventStoreSubscriptionDefinition definition) =>
        new(
            new EventStoreSubscriptionId(definition.Id),
            new Concepts.EventStoreName(definition.SourceEventStore),
            definition.EventTypes);
}
