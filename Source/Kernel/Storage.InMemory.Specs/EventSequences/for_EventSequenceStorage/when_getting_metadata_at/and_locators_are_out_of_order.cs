// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_locators_are_out_of_order : given.a_metadata_storage
{
    IReadOnlyList<StoredEventMetadata> _metadata;

    async Task Establish() => await _storage.AppendMany([_entry with { SequenceNumber = 43UL }, _entry]);

    async Task Because() => _metadata = await _storage.GetMetadataAt([43UL, 1000UL, 42UL, 43UL]);

    [Fact] void should_return_metadata_in_sequence_order() => _metadata.Select(_ => _.SequenceNumber.Value).SequenceEqual([42UL, 43UL]).ShouldBeTrue();
    [Fact] void should_omit_duplicates_and_missing_locators() => _metadata.Count.ShouldEqual(2);
}
