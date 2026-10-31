// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_rearming;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class with_an_operator(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    bool _result;
    Occurrence _stored;

    async Task Establish() => await _storage.Record(_occurrence with { State = OccurrenceState.Quarantined, Epoch = 7, Attempts = 10 });
    async Task Because()
    {
        _result = await _storage.RetryQuarantined(_occurrence.Id, "operator", _now);
        _stored = (await Recreate().Get(_occurrence.Id))!;
    }

    [Fact] void should_rearm() => _result.ShouldBeTrue();
    [Fact] void should_record_the_operator() => _stored.RetriedBy.ShouldEqual("operator");
    [Fact] void should_record_kernel_time() => _stored.RetriedAt.ShouldEqual(_now);
    [Fact] void should_preserve_fencing() => _stored.Epoch.ShouldEqual(7);
    [Fact] void should_reset_attempts() => _stored.Attempts.ShouldEqual(0);
    [Fact] void should_make_work_pending() => _stored.State.ShouldEqual(OccurrenceState.Pending);
    [Fact] async Task should_not_overwrite_audit_on_replay() => (await _storage.RetryQuarantined(_occurrence.Id, "other", _now.AddDays(1))).ShouldBeFalse();
}
