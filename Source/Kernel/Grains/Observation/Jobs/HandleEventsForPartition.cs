// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Grains.Jobs;
using Aksio.Cratis.Observation;
using Aksio.DependencyInversion;
using Microsoft.Extensions.Logging;
using Orleans.SyncWork;

namespace Aksio.Cratis.Kernel.Grains.Observation.Jobs;

/// <summary>
/// Represents a step in a replay job.
/// </summary>
public class HandleEventsForPartition : JobStep<HandleEventsForPartitionArguments>, IReplayJobStep
{
    readonly ProviderFor<IEventSequenceStorage> _eventSequenceStorageProvider;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="HandleEventsForPartition"/> class.
    /// </summary>
    /// <param name="eventSequenceStorageProvider">Provider for <see cref="IEventSequenceStorage"/>.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for working with the execution context.</param>
    /// <param name="taskScheduler"><see cref="LimitedConcurrencyLevelTaskScheduler"/> to use for scheduling.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public HandleEventsForPartition(
        ProviderFor<IEventSequenceStorage> eventSequenceStorageProvider,
        IExecutionContextManager executionContextManager,
        LimitedConcurrencyLevelTaskScheduler taskScheduler,
        ILogger<HandleEventsForPartition> logger) : base(taskScheduler, logger)
    {
        _eventSequenceStorageProvider = eventSequenceStorageProvider;
        _executionContextManager = executionContextManager;
    }

    /// <inheritdoc/>
    protected override async Task PerformStep(HandleEventsForPartitionArguments request)
    {
        var eventSourceId = (EventSourceId)(request.Partition.Value.ToString() ?? string.Empty);
        var observer = GrainFactory.GetGrain<IObserver>(request.ObserverId, request.ObserverKey);
        var subscription = await observer.GetSubscription();
        if (!subscription.IsSubscribed)
        {
            // TODO: Talk back to the job and report status for the step.
            // We can't perform the step.
            return;
        }

        var key = new ObserverSubscriberKey(
            request.ObserverKey.MicroserviceId,
            request.ObserverKey.TenantId,
            request.ObserverKey.EventSequenceId,
            eventSourceId,
            request.ObserverKey.SourceMicroserviceId,
            request.ObserverKey.SourceTenantId);

        var subscriber = (GrainFactory.GetGrain(subscription.SubscriberType, subscription.ObserverId, key) as IObserverSubscriber)!;

        _executionContextManager.Establish(request.ObserverKey.TenantId, CorrelationId.New(), request.ObserverKey.MicroserviceId);
        var eventSequenceStorage = _eventSequenceStorageProvider();
        var events = await eventSequenceStorage.GetFromSequenceNumber(
            request.ObserverKey.EventSequenceId,
            EventSequenceNumber.First,
            eventSourceId,
            request.EventTypes);

        var subscriberContext = new ObserverSubscriberContext(subscription.Arguments);

        while (await events.MoveNext())
        {
            await subscriber.OnNext(events.Current, subscriberContext);
        }
    }
}
