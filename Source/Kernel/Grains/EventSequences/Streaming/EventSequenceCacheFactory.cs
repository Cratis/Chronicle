// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Storage;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCacheFactory"/>.
/// </summary>
public class EventSequenceCacheFactory : IEventSequenceCacheFactory
{
    readonly IClusterStorage _clusterStorage;
    readonly ILogger<EventSequenceCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCacheFactory"/> class.
    /// </summary>
    /// <param name="clusterStorage"><see cref="IClusterStorage"/> for accessing storage for the cluster.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public EventSequenceCacheFactory(
        IClusterStorage clusterStorage,
        ILogger<EventSequenceCache> logger)
    {
        _clusterStorage = clusterStorage;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IEventSequenceCache Create(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId) =>
        new EventSequenceCache(_clusterStorage.GetEventStore((string)microserviceId).GetInstance(tenantId).GetEventSequence(eventSequenceId), _logger);
}
