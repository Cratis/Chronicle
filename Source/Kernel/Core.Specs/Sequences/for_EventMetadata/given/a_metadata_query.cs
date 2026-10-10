// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Sequences.for_EventMetadata.given;

public class a_metadata_query : Specification
{
    protected IStorage _storage;
    protected IEventSequenceStorage _events;
    protected IIdentityStorage _identities;
    protected Dictionary<IdentityId, Concepts.Identities.Identity> _storedIdentities;
    protected IdentityId _identityId;
    protected StoredEventMetadata _entry;
    protected EventMetadata[] _result;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var store = Substitute.For<IEventStoreStorage>();
        var ns = Substitute.For<IEventStoreNamespaceStorage>();
        _events = Substitute.For<IEventSequenceStorage>();
        _identities = Substitute.For<IIdentityStorage>();
        _storage.GetEventStore("store").Returns(store);
        store.GetNamespace("tenant").Returns(ns);
        ns.GetEventSequence(EventSequenceId.Log).Returns(_events);
        ns.Identities.Returns(_identities);
        _identityId = IdentityId.New();
        _storedIdentities = new Dictionary<IdentityId, Concepts.Identities.Identity>
        {
            [_identityId] = new("subject", "Current name", "username")
        };
        _identities.GetByIds(Arg.Any<IEnumerable<IdentityId>>()).Returns(_storedIdentities);
        _entry = new StoredEventMetadata(42UL, "type", "Source", "source", "Stream", "stream", DateTimeOffset.UtcNow, CorrelationId.New(), [], [_identityId], [], new Subject("subject"), EventSourceName.NotSet);
        _events.GetMetadataAt(Arg.Any<IEnumerable<EventSequenceNumber>>(), Arg.Any<CancellationToken>()).Returns(_ => [_entry]);
    }

    protected async Task Read(params ulong[] locators) => _result = (await EventMetadata.MetadataAt(_storage, "store", "tenant", EventSequenceId.Log, locators)).ToArray();
}
