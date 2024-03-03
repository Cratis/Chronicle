// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.EventSequences;
using Cratis.Kernel.Storage;
using Microsoft.Extensions.Logging;

namespace Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCacheFactory"/>.
/// </summary>
public class EventSequenceCacheFactory : IEventSequenceCacheFactory
{
    readonly IStorage _storage;
    readonly ILogger<EventSequenceCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCacheFactory"/> class.
    /// </summary>
    /// <param name="storage"><see cref="IStorage"/> for accessing storage for the cluster.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public EventSequenceCacheFactory(
        IStorage storage,
        ILogger<EventSequenceCache> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IEventSequenceCache Create(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId) =>
        new EventSequenceCache(_storage.GetEventStore((string)microserviceId).GetNamespace(tenantId).GetEventSequence(eventSequenceId), _logger);
}
