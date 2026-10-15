// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_events_were_appended_in_a_batch : given.a_metadata_storage
{
    async Task Establish() => await _storage.AppendMany([_entry]);

    async Task Because() => await Read();

    [Fact] void should_find_only_existing_distinct_locators() => _result.SequenceNumber.ShouldEqual(_entry.SequenceNumber);
    [Fact] void should_preserve_identity_chain_ids() => _result.CausedByChain.ShouldContainOnly(_entry.CausedByChain);
    [Fact] void should_preserve_source_name() => _result.EventSourceName.ShouldEqual(_entry.EventSource);
    [Fact] void should_not_resolve_or_create_identities_during_reads() => _identities.ReceivedCalls().ShouldBeEmpty();
}
