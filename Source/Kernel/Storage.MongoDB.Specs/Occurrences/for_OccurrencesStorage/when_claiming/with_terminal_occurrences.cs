// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_claiming;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class with_terminal_occurrences(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    IEnumerable<Occurrence> _claims;
    IEnumerable<Occurrence> _stored;

    async Task Establish()
    {
        foreach (var state in new[] { OccurrenceState.Completed, OccurrenceState.Skipped, OccurrenceState.Quarantined })
        {
            await _storage.Record(_occurrence with { Id = state.ToString(), State = state, Owner = "old-worker", LeaseExpiry = _now.AddMinutes(1) });
        }

        await _storage.Record(_occurrence with { Id = "live", State = OccurrenceState.Leased, LeaseExpiry = _now.AddMinutes(1) });
        await _storage.Record(_occurrence);
        await _storage.Record(_occurrence with { Id = "later", RecordedAt = _now.AddSeconds(1) });
    }

    async Task Because()
    {
        _claims = await _storage.Claim(_trigger, "worker", _now, TimeSpan.FromMinutes(1), 10, 2);
        _stored = await _storage.GetForTrigger(_trigger);
    }

    [Fact] void should_use_the_one_remaining_in_flight_slot() => _claims.Select(occurrence => occurrence.Id).ShouldContainOnly(_occurrence.Id);
    [Fact] void should_retain_the_terminal_occurrences() => _stored.Count(occurrence => occurrence.State is OccurrenceState.Completed or OccurrenceState.Skipped or OccurrenceState.Quarantined).ShouldEqual(3);
    [Fact] void should_not_exceed_the_live_lease_limit() => _stored.Count(occurrence => occurrence.State == OccurrenceState.Leased).ShouldEqual(2);
}
