// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Identities;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.given;

public class a_publication_storage : Specification
{
    protected EventSequenceStorage _storage;
    protected EventPublication _publication = new("opaque:id/with:delimiters", "immutable-intent");
    protected EventToAppendToStorage _event;

    void Establish()
    {
        var identities = Substitute.For<IIdentityStorage>();
        identities.GetFor(Arg.Any<IEnumerable<IdentityId>>()).Returns(Identity.System);
        _storage = new("store", "tenant", EventSequenceId.Outbox, identities);
        dynamic content = new ExpandoObject();
        content.name = "public fact";
        _event = new(
            0,
            EventSourceType.Default,
            "opaque:source/42",
            EventStreamType.All,
            EventStreamId.Default,
            new EventType("Published", EventTypeGeneration.First),
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            (ExpandoObject)content,
            EventHash.NotSet,
            new Subject("separate:subject"));
    }
}
