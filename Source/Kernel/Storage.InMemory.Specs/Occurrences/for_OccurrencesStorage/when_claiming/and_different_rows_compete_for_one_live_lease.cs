// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_claiming;

public class and_different_rows_compete_for_one_live_lease : given.an_occurrence_storage
{
    IEnumerable<Occurrence>[] _claims;
    async Task Establish()
    {
        await _storage.Record(_occurrence);
        await _storage.Record(_occurrence with { Id = new OccurrenceId(Guid.NewGuid().ToString("N")), RecordedAt = _now.AddSeconds(1) });
    }
    async Task Because() => _claims = await StorageRace.Run(() => _storage.Claim(_trigger, "first", _now, TimeSpan.FromSeconds(60), 1, 1), () => Recreate().Claim(_trigger, "second", _now, TimeSpan.FromSeconds(60), 1, 1));
    [Fact] void should_enforce_the_trigger_limit_across_rows() => _claims.SelectMany(claim => claim).Count().ShouldEqual(1);
    [Fact] void should_claim_the_earliest_occurrence() => _claims.SelectMany(claim => claim).Single().Id.ShouldEqual(_occurrence.Id);
}
