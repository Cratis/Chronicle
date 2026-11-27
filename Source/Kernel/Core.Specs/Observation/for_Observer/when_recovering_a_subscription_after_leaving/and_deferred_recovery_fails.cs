// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_a_subscription_after_leaving;

public class and_deferred_recovery_fails : given.an_observer_with_controlled_persistence
{
    readonly TaskCompletionSource _leaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _leave = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ConnectedClient _client = new() { ConnectionId = "a-connection", Version = "1.0.0" };
    readonly TimeoutException _jobLookupFailure = new("Job lookup failed");
    Exception? _transitionError;
    bool _isClientTarget;
    bool _established;

    void Establish()
    {
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _leaving.TrySetResult();
            return _leave.Task;
        });
        _jobsManager.GetUnfinishedJobs().Returns(
            _ => Task.FromException<IImmutableList<JobState>>(_jobLookupFailure),
            _ => Task.FromResult<IImmutableList<JobState>>(ImmutableList<JobState>.Empty));
        _eventStoreStorage.Namespaces.Returns(Substitute.For<Storage.Namespaces.INamespaceStorage>());
        _eventStoreStorage.Namespaces.GetAll().Returns([new Storage.Namespaces.NamespaceState(_observerKey.Namespace, DateTimeOffset.UtcNow)]);
    }

    async Task Because()
    {
        var transition = _observer.TransitionTo<Disconnected>();
        try
        {
            await _leaving.Task.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
            await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero, _client, automatic: true)
                .WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _leave.TrySetResult();
        }
        _transitionError = await Catch.Exception(() => transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _isClientTarget = await _observer.IsClientTarget(_client.ConnectionId);
        _established = await _observer.IsSubscriptionEstablished();
        await _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [EventType.Unknown], SiloAddress.Zero, _client);
    }

    [Fact] void should_propagate_the_deferred_recovery_error() => _transitionError.ShouldEqual(_jobLookupFailure);
    [Fact] void should_keep_the_client_as_a_target_after_the_failure() => _isClientTarget.ShouldBeTrue();
    [Fact] void should_not_report_the_subscription_as_established() => _established.ShouldBeFalse();
    [Fact] async Task should_restore_the_client_subscription_when_asked() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
