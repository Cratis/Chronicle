// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.DependencyInversion;
using Aksio.Cratis.Events;
using Aksio.Cratis.Kernel.Grains.Observation;
using Aksio.Cratis.Kernel.Observation;
using Aksio.Cratis.Kernel.Grains.Workers;
using Aksio.Cratis.Observation;
using Microsoft.Extensions.Logging;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Kernel.Grains.EventSequences;

/// <summary>
/// Represents a worker that rewinds a partition for observers affected by a redact.
/// </summary>
public class RewindPartitionForObserversAfterRedact : Worker<RewindPartitionForObserversAfterRedactRequest, RewindPartitionForObserversAfterRedactResponse>
{
    readonly ProviderFor<IObserverStorage> _observerStorageProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RewindPartitionForObserversAfterRedact"/> class.
    /// </summary>
    /// <param name="observerStorageProvider">Provider for <see cref="IObserverStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="logger">Logger for logging.</param>
    public RewindPartitionForObserversAfterRedact(
        ProviderFor<IObserverStorage> observerStorageProvider,
        IExecutionContextManager executionContextManager,
        ILogger<RewindPartitionForObserversAfterRedact> logger) : base(executionContextManager, logger)
    {
        _observerStorageProvider = observerStorageProvider;
    }

    /// <inheritdoc/>
    protected override async Task<RewindPartitionForObserversAfterRedactResponse> PerformWork(RewindPartitionForObserversAfterRedactRequest request)
    {
        var workers = new List<IWorker<ReplayPartitionRequest, ReplayPartitionResponse>>();

        var observers = await _observerStorageProvider().GetObserversForEventTypes(request.AffectedEventTypes);
        foreach (var observerId in observers.Select(_ => _.ObserverId))
        {
            var observer = GrainFactory.GetGrain<IObserverSupervisor>(
                    observerId,
                    new ObserverKey(
                        request.MicroserviceId,
                        request.TenantId,
                        request.EventSequenceId));

            var eventTypes = await observer.GetEventTypes();
            if (eventTypes.Any(_ => _.Id == GlobalEventTypes.Redaction))
            {
                await observer.RewindPartitionTo(request.EventSourceId, request.SequenceNumber);
            }
        }

        while (workers.Count > 0)
        {
            var workersToRemove = new List<IWorker<ReplayPartitionRequest, ReplayPartitionResponse>>();

            foreach (var worker in workers)
            {
                var status = await worker.GetStatus();
                switch (status)
                {
                    case WorkerStatus.Failed:
                    case WorkerStatus.Completed:
                        workersToRemove.Add(worker);
                        break;
                }
            }

            foreach (var worker in workersToRemove)
            {
                workers.Remove(worker);
            }
        }

        return new();
    }
}
