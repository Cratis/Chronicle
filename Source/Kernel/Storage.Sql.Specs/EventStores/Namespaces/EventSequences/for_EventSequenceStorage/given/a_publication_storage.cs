// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.given;

public class a_publication_storage : an_event_sequence_storage
{
    protected EventPublication _publication = new("opaque:id/with:delimiters", "immutable-intent");
    protected EventToAppendToStorage _event;

    void Establish()
    {
        dynamic content = new ExpandoObject();
        content.name = "public fact";
        _event = new(
            0,
            EventSourceType.Default,
            "opaque:source/42",
            EventStreamType.All,
            EventStreamId.Default,
            _eventType,
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            (ExpandoObject)content,
            EventHash.NotSet,
            new Subject("separate:subject"));
    }

    protected EventSequenceStorage RecreateStorage() => new(
        _eventStore,
        _namespace,
        _eventSequenceId,
        _database,
        _identityStorage,
        Substitute.For<ILogger<EventSequenceStorage>>());
}
