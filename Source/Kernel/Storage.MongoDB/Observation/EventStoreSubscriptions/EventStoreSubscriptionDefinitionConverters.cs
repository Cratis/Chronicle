// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Observation.EventStoreSubscriptions;

/// <summary>
/// Provides extension methods for converting between Kernel and MongoDB <see cref="EventStoreSubscriptionDefinition"/> representations.
/// </summary>
public static class EventStoreSubscriptionDefinitionConverters
{
    /// <summary>
    /// Converts a Kernel <see cref="Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition"/> to a MongoDB <see cref="EventStoreSubscriptionDefinition"/>.
    /// </summary>
    /// <param name="definition">The Kernel subscription definition.</param>
    /// <returns>The MongoDB subscription definition.</returns>
    public static EventStoreSubscriptionDefinition ToMongoDB(this Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition definition) =>
        new()
        {
            Id = definition.Identifier,
            SourceEventStore = definition.SourceEventStore,
            EventTypes = [.. definition.EventTypes]
        };

    /// <summary>
    /// Converts a MongoDB <see cref="EventStoreSubscriptionDefinition"/> to a Kernel <see cref="Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition"/>.
    /// </summary>
    /// <param name="definition">The MongoDB subscription definition.</param>
    /// <returns>The Kernel subscription definition.</returns>
    public static Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition ToKernel(this EventStoreSubscriptionDefinition definition) =>
        new(
            definition.Id,
            definition.SourceEventStore,
            definition.EventTypes);
}
