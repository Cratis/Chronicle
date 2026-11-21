// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_a_durable_subscription : given.an_observer_state_storage
{
    KernelObserverState _restored;
    KernelObserverState _legacy;
    KernelObserverState _cleared;
    IReadOnlyDictionary<Concepts.Observation.ObserverId, ObserverDurableSubscription> _recorded;
    IReadOnlyDictionary<Concepts.Observation.ObserverId, ObserverDurableSubscription> _recordedForOtherSequence;

    async Task Establish() => await _storage.Save(new() { Identifier = "legacy" }, EventSequenceId.Log);

    async Task Because()
    {
        var expected = new KernelObserverState
        {
            Identifier = "observer",
            DurableSubscription = new(
                ObserverSubscriptionKind.ClientReactor,
                true,
                new(["tag-a", "tag-b"], "source", "stream"),
                [new("connection-1", "silo-a"), new("connection-2", "silo-b")])
        };
        await _storage.Save(expected, EventSequenceId.Log);
        _restored = await _storage.Get("observer");
        _legacy = await _storage.Get("legacy");
        _recorded = await _storage.GetDurableSubscriptions(EventSequenceId.Log);
        _recordedForOtherSequence = await _storage.GetDurableSubscriptions(EventSequenceId.Outbox);
        await _storage.Save(new() { Identifier = "observer" }, EventSequenceId.Log);
        _cleared = await _storage.Get("observer");
    }

    [Fact] void should_keep_the_kind() => _restored.DurableSubscription.Kind.ShouldEqual(ObserverSubscriptionKind.ClientReactor);
    [Fact] void should_keep_the_page_delivery() => _restored.DurableSubscription.DeliversPages.ShouldBeTrue();
    [Fact] void should_keep_the_filter_tags() => string.Join(',', _restored.DurableSubscription.Filters.Tags).ShouldEqual("tag-a,tag-b");
    [Fact] void should_keep_the_filter_event_source_type() => _restored.DurableSubscription.Filters.EventSourceType.ShouldEqual("source");
    [Fact] void should_keep_the_targets() => string.Join(',', _restored.DurableSubscription.Targets.Select(target => $"{target.ConnectionId}@{target.Silo}")).ShouldEqual("connection-1@silo-a,connection-2@silo-b");
    [Fact] void should_not_invent_a_subscription_for_a_legacy_row() => _legacy.DurableSubscription.ShouldBeNull();
    [Fact] void should_list_the_recorded_subscription_of_the_sequence() => _recorded.Keys.Single().Value.ShouldEqual("observer");
    [Fact] void should_not_list_the_subscriptions_of_another_sequence() => _recordedForOtherSequence.ShouldBeEmpty();
    [Fact] void should_clear_it_when_a_state_without_one_is_saved() => _cleared.DurableSubscription.ShouldBeNull();
}
