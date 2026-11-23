// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_Observer.when_ownership_moves;

public class and_only_the_state_stamp_is_stale : given.a_live_observer_with_guarded_state
{
    readonly Storage.InMemory.Observation.ObserverStateStorage _alreadyStamped = new();

    async Task Establish()
    {
        await _alreadyStamped.Save(new() { Identifier = _observerId }, _observerKey.EventSequenceId);
        var otherNamespace = Substitute.For<Storage.IEventStoreNamespaceStorage>();
        otherNamespace.Observers.Returns(_alreadyStamped);
        _eventStoreStorage.GetNamespace("already-stamped").Returns(otherNamespace);
        _eventStoreStorage.Namespaces.GetAll().Returns([
            new Storage.Namespaces.NamespaceState(_observerKey.Namespace, DateTimeOffset.UnixEpoch),
            new Storage.Namespaces.NamespaceState("already-stamped", DateTimeOffset.UnixEpoch)
        ]);
    }

    async Task Because()
    {
        await _guarded.ClaimOwnership(_observerId, "another-sequence");
        await _observer.SetHandledStats(99);
    }

    [Fact] async Task should_reclaim_the_stamp_for_the_stored_definition() =>
        (await _guarded.Get(_observerId)).Owner.ShouldEqual(_observerKey.EventSequenceId);

    [Fact] async Task should_retry_and_save_the_progress() =>
        (await _guarded.Get(_observerId)).LastHandledEventSequenceNumber.ShouldEqual((Concepts.Events.EventSequenceNumber)99UL);

    [Fact] async Task should_leave_the_already_stamped_namespace_unchanged() =>
        (await _alreadyStamped.Get(_observerId)).Owner.ShouldEqual(_observerKey.EventSequenceId);

    void Destroy() => _alreadyStamped.Dispose();
}
