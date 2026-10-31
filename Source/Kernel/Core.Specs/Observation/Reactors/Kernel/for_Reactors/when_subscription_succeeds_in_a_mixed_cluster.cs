// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Releases;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_Reactors;

public class when_subscription_succeeds_in_a_mixed_cluster : given.registrations
{
    readonly TaskCompletionSource _ensureEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _ensureCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _subscriptionsBeforeEnsureCompleted;

    void Establish()
    {
        _releases.Summarize().Returns(new ClusterReleaseSummary([(new(19, 111, 0), 1), (new(19, 112, 0), 1)], 0, 0));
        _observer.Ensure().Returns(_ =>
        {
            _ensureEntered.SetResult();
            return _ensureCompleted.Task;
        });
    }

    async Task Because()
    {
        var registration = _reactors.DiscoverAndRegister(_eventStore, EventStoreNamespaceName.Default);
        try
        {
            await _ensureEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
            _subscriptionsBeforeEnsureCompleted = _observer.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(IObserver.Subscribe));
        }
        finally
        {
            _ensureCompleted.TrySetResult();
        }
        await registration.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_await_ensure_before_subscribing() => _subscriptionsBeforeEnsureCompleted.ShouldEqual(0);
    [Fact] async Task should_ensure_only_once() => await _observer.Received(1).Ensure();
    [Fact] async Task should_subscribe_only_once() => await _observer.Received(1).Subscribe<IReactorObserverSubscriber<TenantReactor>>(Arg.Any<ObserverType>(), Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), null, false);
}
