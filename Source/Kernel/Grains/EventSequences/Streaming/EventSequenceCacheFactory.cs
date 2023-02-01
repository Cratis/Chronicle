// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Execution;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.EventSequences.Streaming;

/// <summary>
/// Represents an implementation of <see cref="IEventSequenceCacheFactory"/>.
/// </summary>
public class EventSequenceCacheFactory : IEventSequenceCacheFactory
{
    readonly IExecutionContextManager _executionContextManager;
    readonly ProviderFor<IEventSequenceStorageProvider> _eventSequenceStorageProvider;
    readonly ILogger<EventSequenceCache> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSequenceCacheFactory"/> class.
    /// </summary>
    /// <param name="executionContextManager"></param>
    /// <param name="eventSequenceStorageProvider"></param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public EventSequenceCacheFactory(
        IExecutionContextManager executionContextManager,
        ProviderFor<IEventSequenceStorageProvider> eventSequenceStorageProvider,
        ILogger<EventSequenceCache> logger)
    {
        _executionContextManager = executionContextManager;
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    public IEventSequenceCache Create(MicroserviceId microserviceId, TenantId tenantId, EventSequenceId eventSequenceId) =>
        new EventSequenceCache(microserviceId, tenantId, eventSequenceId, _executionContextManager, _eventSequenceStorageProvider, _logger);
}
