// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.when_claiming;

public class and_the_commit_acknowledgment_is_lost : given.an_occurrence_storage
{
    Exception _error;
    Occurrence _stored;
    async Task Establish() => await _storage.Record(_occurrence);
    async Task Because()
    {
        _error = await Catch.Exception(() => new given.CommitThenFailOccurrencesStorage(_storage, _schedules).Claim(_trigger, "worker", _now, TimeSpan.FromSeconds(60), 1, 1));
        _stored = (await Recreate().Get(_occurrence.Id))!;
    }
    [Fact] void should_surface_acknowledgment_loss() => _error.ShouldBeOfExactType<given.AcknowledgmentLost>();
    [Fact] void should_keep_the_durable_claim() => _stored.State.ShouldEqual(OccurrenceState.Leased);
    [Fact] void should_keep_the_fencing_epoch() => _stored.Epoch.ShouldEqual(1L);
}
