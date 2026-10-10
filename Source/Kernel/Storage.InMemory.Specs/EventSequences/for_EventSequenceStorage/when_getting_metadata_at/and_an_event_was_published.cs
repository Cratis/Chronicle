// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_an_event_was_published : given.a_metadata_storage
{
    async Task Establish() => await _storage.AppendPublication(new EventPublication("publication", "fingerprint"), _entry);

    async Task Because() => await Read();

    [Fact] void should_find_the_metadata() => _result.SequenceNumber.ShouldEqual(_entry.SequenceNumber);
    [Fact] void should_preserve_source_name() => _result.EventSourceName.ShouldEqual(_entry.EventSource);
    [Fact] void should_preserve_identity_chain_ids() => _result.CausedByChain.ShouldContainOnly(_entry.CausedByChain);
    [Fact] void should_not_resolve_or_create_identities_during_reads() => _identities.ReceivedCalls().ShouldBeEmpty();
}
