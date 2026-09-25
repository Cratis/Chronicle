// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_model_is_cached : given.an_immediate_projection
{
    readonly EventType _eventType = new("ModelCreated", EventTypeGeneration.First);
    ProjectionResult _cached;

    void Establish()
    {
        _projection.GetEventTypes().Returns([_eventType]);
        var cursor = CreateCursor(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 5));
        var cachedCursor = CreateCursor();
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        _eventSequenceStorage.GetFromSequenceNumber((EventSequenceNumber)6, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cachedCursor);
        _projection.ProcessForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ =>
            {
                var state = new ExpandoObject();
                ((IDictionary<string, object?>)state)["name"] = "Created";
                return Task.FromResult(state);
            });
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new JsonObject { ["name"] = "Created" });
    }

    async Task Because()
    {
        await _grain.GetModelInstance();
        _cached = await _grain.GetModelInstance();
    }

    [Fact] void should_preserve_the_folded_watermark() => _cached.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)5);
    [Fact] async Task should_check_the_log_for_new_matching_events_even_when_cached() =>
        await _eventSequenceStorage.Received(1).GetFromSequenceNumber((EventSequenceNumber)6, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>());
}
