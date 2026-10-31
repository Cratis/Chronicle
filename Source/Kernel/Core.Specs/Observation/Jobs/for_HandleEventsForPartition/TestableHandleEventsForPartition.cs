// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForPartition;

/// <summary>
/// A testable subclass of <see cref="HandleEventsForPartition"/> that exposes the protected
/// <see cref="HandleEventsForPartition.PerformStep"/> and provides a way to inject test doubles
/// via reflection.
/// </summary>
/// <param name="state">The persistent state for the job step.</param>
/// <param name="throttle">The throttle for limiting parallel execution.</param>
/// <param name="storage">The storage for the cluster.</param>
/// <param name="eventCompliance">The <see cref="IEventCompliance"/> for decrypting PII event content.</param>
/// <param name="subscriberSelector">The <see cref="IObserverSubscriberSelector"/> for selecting the target client instance.</param>
/// <param name="configurationProvider">The <see cref="IConfigurationForObserverProvider"/> for the observer's subscriber timeout.</param>
/// <param name="observerServiceClient">The service client for confirming projection writes.</param>
/// <param name="logger">The logger.</param>
/// <param name="eventGenerationRelease">The pinned generation release boundary.</param>
/// <param name="timeProvider">The delivery fence clock.</param>
public class TestableHandleEventsForPartition(
    [PersistentState(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForPartitionState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    IObserverSubscriberSelector subscriberSelector,
    IConfigurationForObserverProvider configurationProvider,
    IObserverServiceClient observerServiceClient,
    ILogger<HandleEventsForPartition> logger,
    IEventGenerationRelease? eventGenerationRelease = null,
    TimeProvider? timeProvider = null)
    : HandleEventsForPartition(state, throttle, storage, eventCompliance, subscriberSelector, configurationProvider, observerServiceClient, logger, eventGenerationRelease, timeProvider), IGrainType
{
    static readonly FieldInfo _observerField = typeof(HandleEventsForPartition).GetField("_observer", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo _subscriberField = typeof(HandleEventsForPartition).GetField("_subscriber", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo _eventSourceIdField = typeof(HandleEventsForPartition).GetField("_eventSourceId", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <inheritdoc/>
    public Type GrainType => typeof(IHandleEventsForPartition);

    /// <summary>
    /// Gets the run the step performs in, once <see cref="InvokePerformStep"/> has started it.
    /// </summary>
    public StepRunForTesting? Run { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the step persists its progress checkpoint after every reported batch.
    /// </summary>
    public bool IsCheckpointingAfterEveryBatch => CheckpointAfterEveryBatch;

    /// <summary>
    /// Sets up internal fields for testing, bypassing the full grain preparation flow.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> grain mock.</param>
    /// <param name="subscriber">The <see cref="IObserverSubscriber"/> mock.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> for the partition.</param>
    /// <param name="subscription">The captured delivery subscription.</param>
    public void SetupForTesting(IObserver observer, IObserverSubscriber subscriber, EventSourceId eventSourceId, ObserverSubscription? subscription = null)
    {
        _observerField.SetValue(this, observer);
        _subscriberField.SetValue(this, subscriber);
        _eventSourceIdField.SetValue(this, eventSourceId);
        typeof(HandleEventsForPartition).GetField("_grainFactory", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, GrainFactory);
        typeof(HandleEventsForPartition).GetField("_deliveryTarget", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, subscription is { Targets.Count: > 0 } ? subscription.Targets[0] : new ObserverSubscriberTarget(SiloAddress.Zero));
        if (subscription is not null)
        {
            typeof(HandleEventsForPartition).GetField("_deliverySubscription", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(this, subscription);
        }
    }

    /// <summary>
    /// Exposes the protected <see cref="HandleEventsForPartition.PerformStep"/> for testing.
    /// </summary>
    /// <param name="currentState">The current state of the job step.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the step.</returns>
    public Task<Catch<JobStepResult>> InvokePerformStep(HandleEventsForPartitionState currentState, CancellationToken cancellationToken = default)
    {
        Run = StepRunForTesting.Begin(this);
        return PerformStep(currentState, cancellationToken);
    }
}
