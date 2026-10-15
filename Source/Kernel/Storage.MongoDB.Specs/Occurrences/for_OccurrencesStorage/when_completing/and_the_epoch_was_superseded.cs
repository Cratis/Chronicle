// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_completing;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_epoch_was_superseded(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    LeaseToken _tokenBeforeReclaim;
    OccurrenceLeaseResult _complete;
    OccurrenceLeaseResult _fail;
    Occurrence _stored;
    async Task Establish()
    {
        await _storage.Record(_occurrence);
        var claimed = (await _storage.Claim(_trigger, "first", _now, TimeSpan.FromSeconds(1), 1, 1)).Single();
        _tokenBeforeReclaim = new LeaseToken(claimed.Id, claimed.Epoch);
        await Recreate().Claim(_trigger, "second", _now.AddSeconds(1), TimeSpan.FromSeconds(60), 1, 1);
    }
    async Task Because()
    {
        _complete = await _storage.Complete(_tokenBeforeReclaim);
        _fail = await _storage.Fail(_tokenBeforeReclaim, "stale", null);
        _stored = (await _storage.Get(_occurrence.Id))!;
    }
    [Fact] void should_refuse_stale_completion() => _complete.ShouldEqual(OccurrenceLeaseResult.OccurrenceLeaseLost);
    [Fact] void should_refuse_stale_failure() => _fail.ShouldEqual(OccurrenceLeaseResult.OccurrenceLeaseLost);
    [Fact] void should_keep_the_new_owner() => _stored.Owner.ShouldEqual("second");
    [Fact] void should_keep_the_reclaimed_epoch() => _stored.Epoch.ShouldEqual(2L);
}
