// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_the_identities_are_written : given.a_keyed_batch_storage
{
    IReadOnlyDictionary<string, StoredEventPublication> _receipts;
    async Task Because()
    {
        var result = await _storage.AppendManyWithIdentities([_event with { Identity = _publication }, _event with { SequenceNumber = 1, Identity = new EventPublication("second", "second-intent") }, _event with { SequenceNumber = 2 }]);
        result.IsSuccess.ShouldBeTrue();
        _receipts = await _storage.TryGetPublications([_publication.Id, "second"]);
    }
    [Fact] void should_store_both_identities() => _receipts.Count.ShouldEqual(2);
    [Fact] void should_return_the_stored_fingerprint() => _receipts[_publication.Id].Fingerprint.ShouldEqual(_publication.Fingerprint);
    [Fact] void should_return_the_stored_sequence_number() => _receipts[_publication.Id].SequenceNumber.ShouldEqual(_event.SequenceNumber);
    [Fact] async Task should_write_the_unkeyed_event_too() => (await _storage.GetTailSequenceNumber()).Value.ShouldEqual(2UL);
}
