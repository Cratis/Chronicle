// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Namespaces;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Observation.for_ObserverRemover.when_removing;

public class and_a_later_namespace_rejects_removal : for_Observer.given.an_observer
{
    ObserverRemover _remover;
    Exception _removalError;
    ObserverRemovalNotAllowed _rejection;

    void Establish()
    {
        EventStoreNamespaceName secondNamespace = "second";
        var secondKey = _observerKey with { Namespace = secondNamespace };
        var secondObserver = Substitute.For<IObserver>();
        secondObserver.GetState().Returns(_stateStorage.State);
        _rejection = new(secondKey);
        secondObserver.Remove().Returns(Task.FromException(_rejection));
        var grains = Substitute.For<IGrainFactory>();
        var namespaces = Substitute.For<INamespaces>();
        namespaces.GetAll().Returns([_observerKey.Namespace, secondNamespace]);
        grains.GetGrain<INamespaces>(_observerKey.EventStore).Returns(namespaces);
        grains.GetGrain<IObserver>(_observerKey).Returns(_observer);
        grains.GetGrain<IObserver>(secondKey).Returns(secondObserver);
        _eventStoreStorage.Observers.Has(_observerId).Returns(true);
        _remover = new(grains, _storage, NullLogger<ObserverRemover>.Instance);
    }

    async Task Because() => _removalError = await Catch.Exception(() => _remover.Remove(_observerKey.EventStore, _observerId, _observerKey.EventSequenceId));

    [Fact] void should_propagate_the_original_rejection() => _removalError.ShouldEqual(_rejection);
    [Fact] async Task should_not_delete_the_definition() => await _eventStoreStorage.Observers.DidNotReceive().Delete(_observerId);
    [Fact] async Task should_have_deleted_earlier_failures() => await _eventStoreNamespaceStorage.FailedPartitions.Received(1).RemoveAllFor(_observerId);
    [Fact] async Task should_have_deleted_earlier_counts() => await _observerHandledCountsStorage.Received(1).RemoveAllFor(_observerId);
    [Fact] async Task should_have_deleted_earlier_state() => await _eventStoreNamespaceStorage.Observers.Received(1).Delete(_observerId);
    [Fact] async Task should_have_removed_the_earlier_alert_reminder() => (await _observer.GetReminder(Observer.AlertReminderName)).ShouldBeNull();
}
