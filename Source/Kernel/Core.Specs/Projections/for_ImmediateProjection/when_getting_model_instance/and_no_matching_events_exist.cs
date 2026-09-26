// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_no_matching_events_exist : given.an_immediate_projection
{
    readonly EventType _eventType = new("ModelCreated", EventTypeGeneration.First);
    ProjectionResult _result;

    void Establish()
    {
        _projection.GetEventTypes().Returns([_eventType]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)10);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(new ExpandoObject());
        var cursor = CreateCursor();
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
    }

    async Task Because() => _result = await _grain.GetModelInstance();

    [Fact] void should_report_absence() => _result.HasReadModel.ShouldBeFalse();
    [Fact] void should_report_no_matching_event() => _result.LastHandledEventSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
