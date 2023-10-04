// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans.SyncWork;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job.
/// </summary>
public class ReplayPartitionStep : JobStep<ReplayStepRequest>, IReplayJobStep
{
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReplayPartitionStep"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="taskScheduler"><see cref="LimitedConcurrencyLevelTaskScheduler"/> to use for scheduling.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ReplayPartitionStep(
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        IExecutionContextManager executionContextManager,
        LimitedConcurrencyLevelTaskScheduler taskScheduler,
        ILogger<ReplayPartitionStep> logger) : base(taskScheduler, logger)
    {
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    protected override async Task PerformStep(ReplayStepRequest request)
    {
        var eventSourceId = (EventSourceId)(request.Partition.Value.ToString() ?? string.Empty);
        _executionContextManager.Establish(request.ObserverKey.TenantId, CorrelationId.New(), request.ObserverKey.MicroserviceId);
        var eventSequenceStorage = _eventSequenceStorageProvider();
        var events = await eventSequenceStorage.GetFromSequenceNumber(
            request.ObserverKey.EventSequenceId,
            EventSequenceNumber.First,
            eventSourceId,
            request.EventTypes);

        var observer = GrainFactory.GetGrain<IObserver>(request.ObserverId, request.ObserverKey);

        while (await events.MoveNext())
        {
            await observer.Handle(eventSourceId, events.Current);
        }
    }
}
