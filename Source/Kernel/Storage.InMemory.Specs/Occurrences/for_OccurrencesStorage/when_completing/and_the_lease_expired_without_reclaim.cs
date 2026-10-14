// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.InMemory.Occurrences.for_OccurrencesStorage.when_completing;

public class and_the_lease_expired_without_reclaim : given.an_occurrence_storage
{
    LeaseToken _lease;
    OccurrenceLeaseResult _result;
    OccurrenceLeaseResult _repeated;
    async Task Establish()
    {
        await _storage.Record(_occurrence);
        var claimed = (await _storage.Claim(_trigger, "first", _now.AddSeconds(-2), TimeSpan.FromSeconds(1), 1, 1)).Single();
        _lease = new LeaseToken(claimed.Id, claimed.Epoch);
    }
    async Task Because()
    {
        _result = await _storage.Complete(_lease);
        _repeated = await Recreate().Complete(_lease);
    }
    [Fact] void should_accept_the_current_epoch() => _result.ShouldEqual(OccurrenceLeaseResult.Updated);
    [Fact] void should_make_repeated_completion_idempotent() => _repeated.ShouldEqual(OccurrenceLeaseResult.AlreadyCompleted);
}
