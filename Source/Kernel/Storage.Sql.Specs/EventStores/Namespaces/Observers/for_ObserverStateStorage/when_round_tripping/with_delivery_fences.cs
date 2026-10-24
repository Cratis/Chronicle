// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using KernelObserverState = Cratis.Chronicle.Storage.Observation.ObserverState;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.for_ObserverStateStorage.when_round_tripping;

public class with_delivery_fences : given.an_observer_state_storage
{
    readonly DateTimeOffset _releaseBy = DateTimeOffset.UnixEpoch.AddSeconds(330);
    readonly DeliveryFenceTarget _target = new("type", "key", "A", "silo");
    KernelObserverState _saved;
    KernelObserverState _renamed;
    KernelObserverState _released;
    KernelObserverState _legacy;

    async Task Because()
    {
        await _storage.Save(
            new()
            {
                Identifier = "observer",
                DeliveryFences = [new("partition", _releaseBy, [_target]), new("conservative", _releaseBy, [])]
            },
            Concepts.EventSequences.EventSequenceId.Log);
        _saved = await _storage.Get("observer");
        await _storage.Rename("observer", "renamed");
        _renamed = await _storage.Get("renamed");
        await _storage.Save(_renamed with { DeliveryFences = [] }, Concepts.EventSequences.EventSequenceId.Log);
        _released = await _storage.Get("renamed");
        await _storage.Save(new() { Identifier = "legacy" }, Concepts.EventSequences.EventSequenceId.Log);
        _legacy = await _storage.Get("legacy");
    }

    [Fact] void should_keep_the_partition() => _saved.DeliveryFences.First().Partition.ToString().ShouldEqual("partition");
    [Fact] void should_keep_the_deadline() => _saved.DeliveryFences.First().ReleaseBy.ShouldEqual(_releaseBy);
    [Fact] void should_keep_the_original_target() => _saved.DeliveryFences.First().Targets.ShouldContainOnly(_target);
    [Fact] void should_keep_a_conservative_hold() => _saved.DeliveryFences.Last().Targets.ShouldBeEmpty();
    [Fact] void should_keep_the_target_after_rename() => _renamed.DeliveryFences.First().Targets.ShouldContainOnly(_target);
    [Fact] void should_keep_the_deadline_after_rename() => _renamed.DeliveryFences.First().ReleaseBy.ShouldEqual(_releaseBy);
    [Fact] void should_distinguish_a_drained_snapshot_from_legacy_state() => _released.DeliveryFences.ShouldNotBeNull();
    [Fact] void should_persist_an_empty_snapshot() => _released.DeliveryFences.ShouldBeEmpty();
    [Fact] void should_load_legacy_state_without_a_fence_snapshot() => _legacy.DeliveryFences.ShouldBeNull();
}
