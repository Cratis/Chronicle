// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Decides whether an observer addressed on an event sequence owns its stored definition.
/// </summary>
/// <remarks>
/// Definitions, observer state and failed partitions are stored by observer identifier alone, so an activation
/// addressed on another event sequence than the definition's reaches the records of the real observer. Only the
/// owner may write them.
/// </remarks>
public static class ObserverDefinitionOwnership
{
    /// <summary>
    /// Gets whether the definition belongs to an observer addressed on the given event sequence.
    /// </summary>
    /// <param name="definition">The <see cref="ObserverDefinition"/>.</param>
    /// <param name="eventSequenceId">The event sequence the observer is addressed on.</param>
    /// <returns>True when the definition is on the event sequence, or on none yet; false otherwise.</returns>
    public static bool IsOwnedBy(this ObserverDefinition definition, EventSequenceId eventSequenceId) =>
        definition.EventSequenceId == EventSequenceId.Unspecified ||
        definition.EventSequenceId == eventSequenceId;

    /// <summary>
    /// Throws when the stored definition of the observer is on another event sequence than the key addresses.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the definition.</param>
    /// <param name="observerKey">The <see cref="ObserverKey"/> the observer is addressed with.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ObserverKeyDoesNotMatchDefinition">The stored definition is on another event sequence.</exception>
    public static async Task ThrowIfKeyDoesNotOwnStoredDefinition(this IStorage storage, ObserverKey observerKey)
    {
        var definition = await storage.GetEventStore(observerKey.EventStore).Observers.Get(observerKey.ObserverId);
        if (definition?.IsOwnedBy(observerKey.EventSequenceId) == false)
        {
            throw new ObserverKeyDoesNotMatchDefinition(observerKey, definition.EventSequenceId);
        }
    }
}
