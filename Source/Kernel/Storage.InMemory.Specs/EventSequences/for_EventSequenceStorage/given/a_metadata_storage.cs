// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class a_metadata_storage : Specification
{
    protected EventSequenceStorage _storage;
    protected IIdentityStorage _identities;
    protected EventToAppendToStorage _entry;
    protected StoredEventMetadata _result;

    void Establish()
    {
        _identities = Substitute.For<IIdentityStorage>();
        _identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _storage = new EventSequenceStorage("store", "tenant", EventSequenceId.Log, _identities);
        _entry = new EventToAppendToStorage(
            42UL,
            "Source",
            "source",
            "Stream",
            "stream-id",
            new EventType("type", 1),
            CorrelationId.New(),
            [new Causation(DateTimeOffset.UtcNow, "original", new Dictionary<string, string> { ["value"] = "original" })],
            [IdentityId.New()],
            [new Tag("tag")],
            DateTimeOffset.UtcNow,
            new ExpandoObject(),
            EventHash.NotSet,
            new Subject("subject"))
        {
            EventSource = "SourceName"
        };
    }

    protected async Task Read()
    {
        _identities.ClearReceivedCalls();
        _result = (await _storage.GetMetadataAt([42UL, 42UL, 1000UL])).Single();
    }
}
