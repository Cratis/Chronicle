// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage;
using Cratis.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCacheFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="EventSequenceCacheFactory"/> class.
/// </remarks>
/// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class EventSequenceCacheFactory(
    IStorage storage,
    ILogger<EventSequenceCache> logger) : IEventSequenceCacheFactory
{
    /// <inheritdoc/>
    public IEventSequenceCache Create(EventStoreName eventStore, EventStoreNamespaceName @namespace, EventSequenceId eventSequenceId) =>
        new EventSequenceCache(storage.GetEventStore(eventStore).GetNamespace(@namespace).GetEventSequence(eventSequenceId), logger);
}
