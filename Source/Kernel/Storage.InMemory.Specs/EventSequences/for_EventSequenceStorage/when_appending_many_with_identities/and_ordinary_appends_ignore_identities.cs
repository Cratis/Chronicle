// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many_with_identities;

public class and_ordinary_appends_ignore_identities : given.a_keyed_batch_storage
{
    IReadOnlyDictionary<string, StoredEventPublication> _receipts;
    async Task Because()
    {
        (await _storage.AppendMany([_event with { Identity = _publication }])).IsSuccess.ShouldBeTrue();
        (await _storage.AppendManyWithNamedTags([_event with { SequenceNumber = 1, Identity = _publication }])).IsSuccess.ShouldBeTrue();
        _receipts = await _storage.TryGetPublications([_publication.Id]);
    }
    [Fact] void should_keep_the_ordinary_append_contract() => _receipts.Count.ShouldEqual(0);
    [Fact] async Task should_append_both_events() => (await _storage.GetTailSequenceNumber()).Value.ShouldEqual(1UL);
}
