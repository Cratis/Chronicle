// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Defines a system that manages <see cref="IEventSequenceCache"/> instances.
/// </summary>
public interface IEventSequenceCacheFactory
{
    /// <summary>
    /// Create a new <see cref="IEventSequenceCache"/> for the given <see cref="EventStoreName"/>, <see cref="EventStoreNamespaceName"/> and <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="eventStore"><see cref="EventStoreName"/> to create for.</param>
    /// <param name="namespace"><see cref="EventStoreNamespaceName"/> to create for.</param>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to create for.</param>
    /// <returns>A new <see cref="IEventSequenceCache"/>.</returns>
    IEventSequenceCache Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId);
}
