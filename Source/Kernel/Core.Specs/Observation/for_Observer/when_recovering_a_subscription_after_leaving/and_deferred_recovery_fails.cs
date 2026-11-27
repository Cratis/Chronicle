// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Choreography.Storage.Jobs;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation.States;

namespace Cratis.Chronicle.Observation.for_Observer.when_recovering_a_subscription_after_leaving;

public class and_deferred_recovery_fails : given.an_observer_with_controlled_persistence
{
    readonly TaskCompletionSource _leaving = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _leave = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly ConnectedClient _client = new() { ConnectionId = "a-connection", Version = "1.0.0" };
    bool _established;

    void Establish()
    {
        _appendedEventsQueues.Unsubscribe(Arg.Any<AppendedEventsQueueSubscription>()).Returns(_ =>
        {
            _leaving.TrySetResult();
            return _leave.Task;
        });
        _jobsManager.GetUnfinishedJobs().Returns(
            _ => Task.FromException<IImmutableList<JobState>>(new TimeoutException("Job lookup failed")),
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
            await Subscribe().WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System);
        }
        finally
        {
            _leave.TrySetResult();
        }
        await Catch.Exception(() => transition.WaitAsync(TimeSpan.FromSeconds(5), TimeProvider.System));
        _established = await _observer.IsSubscriptionEstablished();
        await Catch.Exception(() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [Concepts.Events.EventType.Unknown], SiloAddress.Zero, _client));
    }

    [Fact] void should_not_report_the_subscription_as_established() => _established.ShouldBeFalse();
    [Fact] async Task should_restore_the_client_subscription_when_asked() => (await _observer.GetCurrentState()).ShouldBeOfExactType<Observing>();
}
