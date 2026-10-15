// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_expiring_leases;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class with_two_workers(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    OccurrenceId _other;

    async Task Establish()
    {
        _other = new(Guid.NewGuid().ToString("N"));
        await _storage.Record(_occurrence with { State = OccurrenceState.Leased, Owner = "disconnected", Epoch = 3, LeaseExpiry = _now.AddMinutes(1) });
        await _storage.Record(_occurrence with { Id = _other, State = OccurrenceState.Leased, Owner = "live", Epoch = 2, LeaseExpiry = _now.AddMinutes(1) });
    }

    async Task Because() => await _storage.ExpireLeases("disconnected", _now);

    [Fact] async Task should_expire_the_disconnected_worker() => (await Recreate().Get(_occurrence.Id))!.LeaseExpiry.ShouldEqual(_now);
    [Fact] async Task should_not_change_fencing() => (await Recreate().Get(_occurrence.Id))!.Epoch.ShouldEqual(3);
    [Fact] async Task should_leave_the_other_worker_alone() => (await Recreate().Get(_other))!.LeaseExpiry.ShouldEqual(_now.AddMinutes(1));
    [Fact] async Task should_discover_the_queue_without_event_data() => (await Recreate().GetPendingTriggers()).ShouldContainOnly(_trigger);
}
