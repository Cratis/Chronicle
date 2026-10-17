// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Occurrences.for_OccurrencesStorage.when_claiming;

public class with_a_retrying_execution_strategy : given.an_occurrence_storage_with_a_retrying_execution_strategy
{
    Occurrence[] _claimed;
    async Task Establish() => await _storage.Record(_occurrence);
    async Task Because() => _claimed = (await _storage.Claim(_trigger, "worker", _now, TimeSpan.FromSeconds(60), 1, 1)).ToArray();
    [Fact] void should_claim_the_occurrence() => _claimed.Single().Id.ShouldEqual(_occurrence.Id);
    [Fact] void should_lease_it_at_the_first_epoch() => _claimed.Single().Epoch.ShouldEqual(1L);
}
