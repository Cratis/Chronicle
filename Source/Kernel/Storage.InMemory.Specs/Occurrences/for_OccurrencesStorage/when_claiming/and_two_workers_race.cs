// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_claiming;

public class and_two_workers_race : given.an_occurrence_storage
{
    IEnumerable<Occurrence>[] _claims;
    Occurrence _stored;
    async Task Establish() => await _storage.Record(_occurrence);
    async Task Because()
    {
        _claims = await StorageRace.Run(() => _storage.Claim(_trigger, "first", _now, TimeSpan.FromSeconds(60), 1, 1), () => Recreate().Claim(_trigger, "second", _now, TimeSpan.FromSeconds(60), 1, 1));
        _stored = (await _storage.Get(_occurrence.Id))!;
    }
    [Fact] void should_claim_only_once() => _claims.SelectMany(claim => claim).Count().ShouldEqual(1);
    [Fact] void should_increment_the_epoch_once() => _stored.Epoch.ShouldEqual(1L);
    [Fact] void should_count_one_attempt() => _stored.Attempts.ShouldEqual(1);
}
