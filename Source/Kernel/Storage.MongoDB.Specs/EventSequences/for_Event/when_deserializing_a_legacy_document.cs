// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_Event;

public class when_deserializing_a_legacy_document : Specification
{
    Event _event;

    void Because()
    {
        var document = new Event(
            EventSequenceNumber.First,
            CorrelationId.NotSet,
            [],
            [IdentityId.NotSet],
            new EventTypeId(Guid.NewGuid().ToString()),
            DateTimeOffset.UtcNow,
            EventSourceType.Default,
            "event-source",
            EventStreamType.All,
            EventStreamId.Default,
            [],
            new Dictionary<string, BsonDocument> { ["1"] = new() },
            new Dictionary<string, string> { ["1"] = "hash" },
            []).ToBsonDocument();
        document.Remove("lastMutationOrdinal");

        _event = BsonSerializer.Deserialize<Event>(document);
    }

    [Fact] void should_default_the_last_mutation_ordinal_to_zero() => _event.LastMutationOrdinal.ShouldEqual(0L);
}
