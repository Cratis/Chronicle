// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_getting_metadata_at : given.an_event_sequence_storage
{
    IReadOnlyList<StoredEventMetadata> _result;
    IdentityId _id;

    void Establish()
    {
        _id = IdentityId.New();
        using var context = CreateContext();
        context.Events.Add(new EventEntry
        {
            SequenceNumber = 42,
            Type = _eventType.Id,
            CorrelationId = CorrelationId.New().Value.ToString(),
            CausedBy = EventEntryConverter.SerializeCausedBy([_id]),
            Causation = "[]",
            Content = "not valid JSON",
            Tags = "[\"tag\"]",
            EventStreamId = "stream-id",
            Subject = "subject"
        });
        context.SaveChanges();
        _identityStorage.ClearReceivedCalls();
    }

    async Task Because() => _result = await _storage.GetMetadataAt([42UL, 42UL, 1000UL]);

    [Fact] void should_read_without_parsing_content() => _result.Count.ShouldEqual(1);
    [Fact] void should_omit_missing_locators() => _result.Single().SequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_preserve_identity_ids_without_resolving_them() => _result.Single().CausedByChain.ShouldContainOnly(_id);
    [Fact] void should_preserve_stream_id() => _result.Single().EventStreamId.Value.ShouldEqual("stream-id");
    [Fact] void should_preserve_tags() => _result.Single().Tags.Select(_ => _.Value).ShouldContainOnly("tag");
    [Fact] void should_not_use_cached_identity_resolution() => _identityStorage.ReceivedCalls().ShouldBeEmpty();
}
