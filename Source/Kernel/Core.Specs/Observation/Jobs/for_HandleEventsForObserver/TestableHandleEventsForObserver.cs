// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Jobs;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Jobs;
using Cratis.Monads;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver;

/// <summary>
/// A testable subclass of <see cref="HandleEventsForObserver"/> that exposes the protected
/// <see cref="HandleEventsForObserver.PerformStep"/> and provides a way to inject test doubles.
/// </summary>
/// <param name="state">The persistent state for the job step.</param>
/// <param name="throttle">The throttle for limiting parallel execution.</param>
/// <param name="storage">The storage for the cluster.</param>
/// <param name="eventCompliance">The <see cref="IEventCompliance"/> for decrypting PII event content.</param>
/// <param name="logger">The logger.</param>
public class TestableHandleEventsForObserver(
    [PersistentState(nameof(JobStepState), WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForObserverState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    ILogger<HandleEventsForObserver> logger)
    : HandleEventsForObserver(state, throttle, storage, eventCompliance, logger), IGrainType
{
    static readonly FieldInfo _observerField = typeof(HandleEventsForObserver).GetField("_observer", BindingFlags.NonPublic | BindingFlags.Instance)!;
    static readonly FieldInfo _subscriptionField = typeof(HandleEventsForObserver).GetField("_subscription", BindingFlags.NonPublic | BindingFlags.Instance)!;
    readonly IHandleEventsForObserver _selfReference = Substitute.For<IHandleEventsForObserver>();

    /// <inheritdoc/>
    public Type GrainType => typeof(IHandleEventsForObserver);

    /// <inheritdoc/>
    protected override IHandleEventsForObserver GetSelfGrainReference() => _selfReference;

    /// <summary>
    /// Sets up internal fields for testing, bypassing the full grain preparation flow.
    /// </summary>
    /// <param name="observer">The <see cref="IObserver"/> grain mock.</param>
    /// <param name="subscription">The observer subscription.</param>
    public void SetupForTesting(IObserver observer, ObserverSubscription subscription)
    {
        _observerField.SetValue(this, observer);
        _subscriptionField.SetValue(this, subscription);
    }

    /// <summary>
    /// Exposes the protected <see cref="HandleEventsForObserver.PerformStep"/> for testing.
    /// </summary>
    /// <param name="currentState">The current state of the job step.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the step.</returns>
    public Task<Catch<JobStepResult>> InvokePerformStep(HandleEventsForObserverState currentState, CancellationToken cancellationToken = default) =>
        PerformStep(currentState, cancellationToken);
}
