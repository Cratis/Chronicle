// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_key_is_stream_scoped : given.an_immediate_projection
{
    async Task Establish()
    {
        var key = new ImmediateProjectionKey(Projection, EventStore, EventStoreNamespaceName.Default, EventSequenceId.Log, "stream-id", StreamScope: new("source-id", "source-type", "stream-type", "stream-id"));
        _grain = await _silo.CreateGrainAsync<given.TestableImmediateProjection>(key.ToString());
        _projection.GetEventTypes().Returns([new EventType("created", EventTypeGeneration.First)]);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(new ExpandoObject());
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, (EventSourceId)"source-id", eventSourceType: "source-type", eventStreamType: "stream-type", eventStreamId: "stream-id", eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(CreateCursor());
    }

    async Task Because() => await _grain.GetModelInstance();

    [Fact] void should_narrow_storage_by_source_and_stream() => _eventSequenceStorage.Received(1).GetFromSequenceNumber(EventSequenceNumber.First, (EventSourceId)"source-id", eventSourceType: "source-type", eventStreamType: "stream-type", eventStreamId: "stream-id", eventTypes: Arg.Any<IEnumerable<EventType>>());
}
