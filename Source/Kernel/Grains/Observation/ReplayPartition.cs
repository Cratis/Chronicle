// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Kernel.EventSequences;
using Aksio.Cratis.Kernel.Grains.Workers;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Kernel.Grains.Observation;

/// <summary>
/// Represents a worker that replays a specific partition.
/// </summary>
public class ReplayPartition : Worker<ReplayPartitionRequest, ReplayPartitionResponse>
{
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly ILogger<ReplayPartition> _replayLogger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayPartition"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="logger">Logger for logging.</param>
    public ReplayPartition(
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        IExecutionContextManager executionContextManager,
        ILogger<ReplayPartition> logger) : base(executionContextManager, logger)
    {
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _replayLogger = logger;
    }

    /// <inheritdoc/>
    protected override async Task<ReplayPartitionResponse> PerformWork(ReplayPartitionRequest request)
    {
        _replayLogger.RewindingPartitionTo(
            request.ObserverId,
            request.MicroserviceId,
            request.EventSequenceId,
            request.TenantId,
            request.Partition,
            request.SequenceNumber);

        var observer = GrainFactory.GetGrain<IObserverSupervisor>(
            request.ObserverId,
            new ObserverKey(request.MicroserviceId, request.TenantId, request.EventSequenceId));

        var events = await _eventSequenceStorageProvider().GetFromSequenceNumber(request.EventSequenceId, request.SequenceNumber, request.Partition, request.EventTypes);
        while (await events.MoveNext())
        {
            foreach (var @event in events.Current)
            {
                await observer.Handle(@event, true);
            }
        }

        return new();
    }
}
