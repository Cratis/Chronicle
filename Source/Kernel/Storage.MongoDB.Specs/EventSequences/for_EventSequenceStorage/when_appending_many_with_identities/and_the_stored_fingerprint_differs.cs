// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_stored_fingerprint_differs(ReplicaSetMongoDBFixture fixture) : given.a_keyed_batch_storage(fixture)
{
    Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure> _result;
    async Task Establish() => (await _storage.AppendPublication(_publication, _event)).IsSuccess.ShouldBeTrue();
    async Task Because() => _result = await _storage.AppendManyWithIdentities([_event with { SequenceNumber = 1, Identity = _publication with { Fingerprint = "different" } }]);
    [Fact] void should_report_the_original_fingerprint_without_comparing() => _result.AsT1.AsT1.Receipts[_publication.Id].Fingerprint.ShouldEqual(_publication.Fingerprint);
    [Fact] async Task should_not_write_the_retry() => (await _storage.GetTailSequenceNumber()).ShouldEqual(_event.SequenceNumber);
}
