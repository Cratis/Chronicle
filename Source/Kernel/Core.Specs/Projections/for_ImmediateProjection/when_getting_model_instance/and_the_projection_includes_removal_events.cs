// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_projection_includes_removal_events : given.an_immediate_projection
{
    readonly EventType _created = new("Created", EventTypeGeneration.First);
    readonly EventType _removed = new("Removed", EventTypeGeneration.First);
    ProjectionResult _result;

    void Establish()
    {
        _projection.GetEventTypes().Returns([_created, _removed]);
        var cursor = CreateCursor(
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_created, 1),
            AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_removed, 9));
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        _expandoObjectConverter.ToExpandoObject(Arg.Any<JsonObject>(), Arg.Any<JsonSchema>()).Returns(new ExpandoObject());
        _projection.ProcessForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<ExpandoObject>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(Task.FromResult(new ExpandoObject()));
    }

    async Task Because() => _result = await _grain.GetModelInstance();

    [Fact] void should_report_absence_after_removal() => _result.HasReadModel.ShouldBeFalse();
    [Fact] void should_report_the_removal_sequence_number() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)9);
    [Fact] async Task should_filter_the_log_by_both_event_types_and_source() =>
        await _eventSequenceStorage.Received(1).GetFromSequenceNumber(
            EventSequenceNumber.First,
            eventSourceId: ReadModelKey,
            eventTypes: Arg.Is<IEnumerable<EventType>>(types => types.Contains(_created) && types.Contains(_removed) && types.Count() == 2));
}
