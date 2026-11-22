// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Storage.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_standbys_in_a_durable_subscription : given.an_observer_state_storage
{
    KernelObserverState _restored;
    KernelObserverState _withoutStandbys;

    async Task Because()
    {
        await _storage.Save(
            new()
            {
                Identifier = "observer",
                DurableSubscription = new(
                    ObserverSubscriptionKind.ClientReactor,
                    false,
                    null,
                    [new("connection-1", "silo-a")],
                    [new("connection-2", "silo-b", ["type-a+1+False", "type-b+2+True"], true, new(["tag"], "source", "stream"), true, false)])
            },
            EventSequenceId.Log);
        await _storage.Save(
            new()
            {
                Identifier = "plain",
                DurableSubscription = new(ObserverSubscriptionKind.ClientReactor, false, null, [new("connection-1", "silo-a")])
            },
            EventSequenceId.Log);
        _restored = await _storage.Get("observer");
        _withoutStandbys = await _storage.Get("plain");
    }

    [Fact] void should_keep_the_standby() => _restored.DurableSubscription!.Standbys!.Select(standby => $"{standby.ConnectionId}@{standby.Silo}").ShouldContainOnly("connection-2@silo-b");
    [Fact] void should_keep_the_event_types_of_the_standby() => string.Join(',', _restored.DurableSubscription!.Standbys![0].EventTypes).ShouldEqual("type-a+1+False,type-b+2+True");
    [Fact] void should_keep_the_replayability_of_the_standby() => _restored.DurableSubscription!.Standbys![0].IsReplayable.ShouldBeTrue();
    [Fact] void should_keep_the_filters_of_the_standby() => _restored.DurableSubscription!.Standbys![0].Filters!.EventSourceType.ShouldEqual("source");
    [Fact] void should_keep_the_generation_delivery_of_the_standby() => _restored.DurableSubscription!.Standbys![0].PinsGenerations.ShouldBeTrue();
    [Fact] void should_read_a_record_without_standbys_as_having_none() => (_withoutStandbys.DurableSubscription!.Standbys ?? []).ShouldBeEmpty();
}
