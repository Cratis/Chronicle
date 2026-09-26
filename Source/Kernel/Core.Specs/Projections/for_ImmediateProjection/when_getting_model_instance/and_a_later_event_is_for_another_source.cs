// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_a_later_event_is_for_another_source : given.an_immediate_projection
{
    readonly EventType _eventType = new("ModelCreated", EventTypeGeneration.First);
    ProjectionResult _first;
    ProjectionResult _cached;
    int _processingCalls;

    void Establish()
    {
        _projection.GetEventTypes().Returns([_eventType]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)10);
        var initialCursor = CreateCursor(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 5));
        var cachedCursor = CreateCursor();
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(initialCursor);
        _eventSequenceStorage.GetFromSequenceNumber((EventSequenceNumber)6, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cachedCursor);
        _projection.ProcessForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(_ =>
            {
                _processingCalls++;
                var state = new ExpandoObject();
                ((IDictionary<string, object?>)state)["name"] = "Created";
                return Task.FromResult(state);
            });
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new JsonObject { ["name"] = "Created" });
    }

    async Task Because()
    {
        _first = await _grain.GetModelInstance();
        _cached = await _grain.GetModelInstance();
    }

    [Fact] void should_report_the_last_event_folded_for_this_source() => _cached.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)5);
    [Fact] void should_keep_the_instance() => _cached.HasReadModel.ShouldBeTrue();
    [Fact] void should_not_refold_the_other_sources_events() => _processingCalls.ShouldEqual(1);
    [Fact] async Task should_filter_the_log_by_this_source_and_the_projected_event_type() =>
        await _eventSequenceStorage.Received(1).GetFromSequenceNumber(
            (EventSequenceNumber)6,
            eventSourceId: ReadModelKey,
            eventTypes: Arg.Is<IEnumerable<EventType>>(types => types.Count() == 1 && types.Contains(_eventType)));
}
