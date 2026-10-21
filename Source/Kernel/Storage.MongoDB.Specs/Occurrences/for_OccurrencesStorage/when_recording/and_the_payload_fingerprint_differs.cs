// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.MongoDB.EventSequences;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Storage.MongoDB.Occurrences.for_OccurrencesStorage.when_recording;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_payload_fingerprint_differs(ReplicaSetMongoDBFixture fixture) : given.an_occurrence_storage(fixture)
{
    OccurrenceRecordResult _result;
    Occurrence _stored;
    async Task Establish() => await _storage.Record(_occurrence);
    async Task Because()
    {
        _result = await Recreate().Record(_occurrence with { Fingerprint = "different", Payload = "{}" });
        _stored = (await _storage.Get(_occurrence.Id))!;
    }
    [Fact] void should_refuse_the_identity_conflict() => _result.ShouldEqual(OccurrenceRecordResult.OccurrenceIdConflict);
    [Fact] void should_keep_the_original_payload() => _stored.Payload.ShouldEqual("{\"value\":1}");
}
