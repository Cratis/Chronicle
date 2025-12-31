// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Extension methods for the <see cref="IGrainFactory"/> to work with event sequences.
/// </summary>
public static class EventSequencesGrainFactoryExtensions
{
    /// <summary>
    /// Gets the event sequence for a specific <see cref="EventSequenceId"/> and <see cref="EventStoreName"/> and <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to use for getting the event sequence.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> to get the event sequence for.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to get the event sequence for.</param>
    /// <param name="namespaceName">The <see cref="EventStoreNamespaceName"/> to get the event sequence for.</param>
    /// <returns>An <see cref="IEventSequence"/> representing the event sequence.</returns>
    public static IEventSequence GetEventSequence(this IGrainFactory grainFactory, EventSequenceId eventSequenceId, EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var key = new EventSequenceKey(eventSequenceId, eventStore, namespaceName);
        return grainFactory.GetGrain<IEventSequence>(key);
    }

    /// <summary>
    /// Gets the event log sequence.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to use for getting the event sequence.</param>
    /// <returns>An <see cref="IEventSequence"/> representing the event log sequence.</returns>
    public static IEventSequence GetEventLog(this IGrainFactory grainFactory)
    {
        var key = new EventSequenceKey(EventSequenceId.Log, EventStoreName.System, EventStoreNamespaceName.Default);
        return grainFactory.GetGrain<IEventSequence>(key);
    }

    /// <summary>
    /// Gets the system event sequence for a specific <see cref="EventStoreName"/> and <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to use for getting the event sequence.</param>
    /// <param name="eventStore">Optional <see cref="EventStoreName"/> to get the event sequence for. Defaults to System.</param>
    /// <param name="namespaceName">Optional <see cref="EventStoreNamespaceName"/> to get the event sequence for. Defaults to Default.</param>
    /// <returns>An <see cref="IEventSequence"/> representing the system event sequence.</returns>
    public static IEventSequence GetSystemEventSequence(this IGrainFactory grainFactory, EventStoreName? eventStore = default, EventStoreNamespaceName? namespaceName = default)
    {
        eventStore ??= EventStoreName.System;
        namespaceName ??= EventStoreNamespaceName.Default;
        var key = new EventSequenceKey(EventSequenceId.System, eventStore, namespaceName);
        return grainFactory.GetGrain<IEventSequence>(key);
    }
}
