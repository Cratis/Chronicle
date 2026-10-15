// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Monads;
using Cratis.Orleans.Jobs;
using Cratis.Orleans.Storage.Jobs;
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
/// <param name="grainFactory">The <see cref="IGrainFactory"/> for resolving subscriber grains.</param>
/// <param name="subscriberSelector">The <see cref="IObserverSubscriberSelector"/> for selecting the target client instance.</param>
/// <param name="configurationProvider">The <see cref="IConfigurationForObserverProvider"/> for the observer's subscriber timeout.</param>
/// <param name="observerServiceClient">The client for confirming buffered writes.</param>
/// <param name="logger">The logger.</param>
/// <param name="eventGenerationRelease">The pinned generation release boundary.</param>
public class TestableHandleEventsForObserver(
    [PersistentState(nameof(JobStepState), Cratis.Orleans.WellKnownGrainStorageProviders.JobSteps)]
    IPersistentState<HandleEventsForObserverState> state,
    IJobStepThrottle throttle,
    IStorage storage,
    IEventCompliance eventCompliance,
    IGrainFactory grainFactory,
    IObserverSubscriberSelector subscriberSelector,
    IConfigurationForObserverProvider configurationProvider,
    IObserverServiceClient observerServiceClient,
    ILogger<HandleEventsForObserver> logger,
    IEventGenerationRelease? eventGenerationRelease = null)
    : HandleEventsForObserver(GuardedState(state), throttle, storage, eventCompliance, grainFactory, subscriberSelector, configurationProvider, observerServiceClient, logger, eventGenerationRelease), IGrainType
{
    static readonly FieldInfo _observerField = typeof(HandleEventsForObserver).GetField("_observer", BindingFlags.NonPublic | BindingFlags.Instance)!;
    static readonly FieldInfo _subscriptionField = typeof(HandleEventsForObserver).GetField("_subscription", BindingFlags.NonPublic | BindingFlags.Instance)!;
    readonly IHandleEventsForObserver _selfReference = Substitute.For<IHandleEventsForObserver>();
    static readonly AsyncLocal<bool> _onGrainPath = new();
    static readonly AsyncLocal<bool> _guardStateAccess = new();

    /// <summary>
    /// Gets or sets whether persistent state access outside the simulated grain reference is rejected.
    /// </summary>
    public bool GuardStateAccess { get; set; }

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
        _selfReference.ReportNewSuccessfullyHandledEvent(Arg.Any<Concepts.Events.EventSequenceNumber>())
            .Returns(call => OnGrainPath(() => ReportNewSuccessfullyHandledEvent(call.Arg<Concepts.Events.EventSequenceNumber>())));
        _selfReference.FlushCheckpoint(Arg.Any<bool>()).Returns(call => OnGrainPath(() => FlushCheckpoint(call.Arg<bool>())));
    }

    /// <summary>
    /// Exposes the protected <see cref="HandleEventsForObserver.PerformStep"/> for testing.
    /// </summary>
    /// <param name="currentState">The current state of the job step.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the step.</returns>
    public async Task<Catch<JobStepResult>> InvokePerformStep(HandleEventsForObserverState currentState, CancellationToken cancellationToken = default)
    {
        _guardStateAccess.Value = GuardStateAccess;
        return await PerformStep(currentState, cancellationToken);
    }

    static IPersistentState<HandleEventsForObserverState> GuardedState(IPersistentState<HandleEventsForObserverState> inner)
    {
        var guarded = Substitute.For<IPersistentState<HandleEventsForObserverState>>();
        guarded.State.Returns(_ => !_guardStateAccess.Value || _onGrainPath.Value
            ? inner.State
            : throw new given.StateAccessOutsideActivation());
        guarded.Etag.Returns(_ => inner.Etag);
        guarded.RecordExists.Returns(_ => inner.RecordExists);
        guarded.WriteStateAsync().Returns(_ => inner.WriteStateAsync());
        guarded.ReadStateAsync().Returns(_ => inner.ReadStateAsync());
        guarded.ClearStateAsync().Returns(_ => inner.ClearStateAsync());
        return guarded;
    }

    async Task<T> OnGrainPath<T>(Func<Task<T>> action)
    {
        _onGrainPath.Value = true;
        try
        {
            return await action();
        }
        finally
        {
            _onGrainPath.Value = false;
        }
    }
}
