// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation.for_Observer;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_a_later_namespace_rejects_the_fence : for_Observer.given.an_observer
{
    ObserverRemover _remover;
    Exception _removalError;
    Exception _subscriptionError;
    Guid _fencedLifecycle;
    Guid _canceledLifecycle;

    void Establish()
    {
        EventStoreNamespaceName secondNamespace = "second";
        var secondKey = _observerKey with { Namespace = secondNamespace };
        var secondObserver = Substitute.For<IObserver>();
        secondObserver.GetState().Returns(_stateStorage.State);
        secondObserver.Remove().Returns(_ =>
        {
            _fencedLifecycle = _stateStorage.State.AlertLifecycleId;
            return Task.FromException(new ObserverRemovalNotAllowed(secondKey));
        });
        var grains = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace, secondNamespace]);
        grains.GetGrain<INamespaces>(_observerKey.EventStore).Returns(namespaces);
        grains.GetGrain<IObserver>(_observerKey).Returns(_observer);
        grains.GetGrain<IObserver>(secondKey).Returns(secondObserver);
        _eventStoreStorage.Observers.Has(_observerId).Returns(true);
        _remover = new(grains, _storage, NullLogger<ObserverRemover>.Instance);
    }

    async Task Because()
    {
        _removalError = await Catch.Exception(() => _remover.Remove(_observerKey.EventStore, _observerId, _observerKey.EventSequenceId));
        _canceledLifecycle = _stateStorage.State.AlertLifecycleId;
        await Crash();
        _subscriptionError = await Catch.Exception(() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero));
    }

    [Fact] void should_propagate_the_rejection() => _removalError.ShouldBeOfExactType<ObserverRemovalNotAllowed>();
    [Fact] void should_supersede_the_fenced_lifecycle() => _canceledLifecycle.ShouldNotEqual(_fencedLifecycle);
    [Fact] void should_allow_subscription_after_reactivation() => _subscriptionError.ShouldBeNull();
    [Fact] async Task should_be_subscribed() => (await _observer.IsSubscribed()).ShouldBeTrue();
    [Fact] async Task should_not_delete_the_definition() => await _eventStoreStorage.Observers.DidNotReceive().Delete(Arg.Any<ObserverId>());
    [Fact] async Task should_not_delete_failures() => await _eventStoreNamespaceStorage.FailedPartitions.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
    [Fact] async Task should_not_delete_handled_counts() => await _observerHandledCountsStorage.DidNotReceive().RemoveAllFor(Arg.Any<ObserverId>());
    [Fact] async Task should_not_delete_state() => await _eventStoreNamespaceStorage.Observers.DidNotReceive().Delete(Arg.Any<ObserverId>());
}
