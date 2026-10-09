// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.Projections.for_ProjectionFuturesConverters;

public class when_reading_a_legacy_document : Specification
{
    BsonDocument _document;
    EventContext _read;

    void Establish()
    {
        var legacy = new ProjectionFuture(
            ProjectionFutureId.New(),
            new ProjectionId("projection"),
            new Event(42, new EventType("ChildCreated", 3), "child-42", new BsonDocument()),
            "",
            "children",
            "id",
            "parentId",
            "parent",
            DateTimeOffset.Parse("2026-05-12T14:00:00Z"));
        _document = legacy.ToBsonDocument();
        _document.Remove("context");
    }

    void Because() => _read = BsonSerializer.Deserialize<ProjectionFuture>(_document).ToKernel(new JsonSerializerOptions()).Event.Context;

    [Fact] void should_restore_the_legacy_context() => _read.ShouldEqual(EventContext.Empty with
    {
        SequenceNumber = 42,
        EventType = new Concepts.Events.EventType("ChildCreated", 3),
        EventSourceId = "child-42"
    });
}
