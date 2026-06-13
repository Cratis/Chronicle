// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
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
/// <param name="logger">The logger.</param>
public class TestableHandleEventsForPartition(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForPartitionState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    ILogger<HandleEventsForPartition> logger)
    : HandleEventsForPartition(state, throttle, storage, eventCompliance, logger), IGrainType
{
    static readonly FieldInfo _observerField = typeof(HandleEventsForPartition).GetField("_observer", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo _subscriberField = typeof(HandleEventsForPartition).GetField("_subscriber", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo _eventSourceIdField = typeof(HandleEventsForPartition).GetField("_eventSourceId", BindingFlags.NonPublic | BindingFlags.Instance);

    /// <inheritdoc/>
    public Type GrainType => typeof(IHandleEventsForPartition);

    /// <inheritdoc/>
    protected override IHandleEventsForPartition GetSelfGrainReference() => Substitute.For<IHandleEventsForPartition>();

    /// <summary>
    /// Sets up internal fields for testing, bypassing the full grain preparation flow.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> grain mock.</param>
    /// <param name="subscriber">The <see cref="IObserverSubscriber"/> mock.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/> for the partition.</param>
    public void SetupForTesting(IObserver observer, IObserverSubscriber subscriber, EventSourceId eventSourceId)
    {
        _observerField.SetValue(this, observer);
        _subscriberField.SetValue(this, subscriber);
        _eventSourceIdField.SetValue(this, eventSourceId);
    }

    /// <summary>
    /// Exposes the protected <see cref="HandleEventsForPartition.PerformStep"/> for testing.
    /// </summary>
    /// <param name="currentState">The current state of the job step.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the step.</returns>
    public Task<Catch<JobStepResult>> InvokePerformStep(HandleEventsForPartitionState currentState, CancellationToken cancellationToken = default) =>
        PerformStep(currentState, cancellationToken);
}
