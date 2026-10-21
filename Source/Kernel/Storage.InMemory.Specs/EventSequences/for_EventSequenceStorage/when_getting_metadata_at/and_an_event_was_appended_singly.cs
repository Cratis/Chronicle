// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_getting_metadata_at;

public class and_an_event_was_appended_singly : given.a_metadata_storage
{
    async Task Establish() => await _storage.Append(_entry.SequenceNumber, _entry.EventSourceType, _entry.EventSourceId, _entry.EventStreamType, _entry.EventStreamId, _entry.EventType, _entry.CorrelationId, _entry.Causation, _entry.CausedByChain, _entry.Tags, _entry.Occurred, _entry.GenerationalContent, _entry.ContentHashes, _entry.Subject);

    async Task Because() => await Read();

    [Fact] void should_find_the_metadata() => _result.SequenceNumber.ShouldEqual(_entry.SequenceNumber);
    [Fact] void should_preserve_identity_chain_ids() => _result.CausedByChain.ShouldContainOnly(_entry.CausedByChain);
    [Fact] void should_not_resolve_or_create_identities_during_reads() => _identities.ReceivedCalls().ShouldBeEmpty();
}
