// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

[Collection(ReplicaSetMongoDBCollection.Name)]
public class and_the_identity_has_a_maximum_multibyte_key(ReplicaSetMongoDBFixture fixture) : given.a_keyed_batch_storage(fixture)
{
    string _identity = $"append-key:{new string('界', 512)}";
    IReadOnlyDictionary<string, StoredEventPublication> _receipts;

    async Task Because()
    {
        // Exceeds the pre-4.2 1024-byte index-key limit. The replica-set fixture uses supported MongoDB 8.2.
        (await _storage.AppendManyWithIdentities([_event with { Identity = new EventPublication(_identity, "intent") }])).IsSuccess.ShouldBeTrue();
        _receipts = await _storage.TryGetPublications([_identity]);
    }

    [Fact] void should_store_the_full_identity_without_truncation() => _receipts[_identity].SequenceNumber.ShouldEqual(_event.SequenceNumber);
}
