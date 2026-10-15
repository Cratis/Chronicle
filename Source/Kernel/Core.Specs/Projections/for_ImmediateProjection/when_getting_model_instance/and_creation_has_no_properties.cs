// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_creation_has_no_properties : given.an_immediate_projection
{
    ProjectionResult _result;
    ProjectionResult _cached;

    void Establish()
    {
        var created = new EventType("Created", EventTypeGeneration.First);
        _projection.GetEventTypes().Returns([created]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)1);
        var cursor = CreateCursor(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(created, 1));
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(cursor);
        _projection.FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(new SingleReadModelProjectionState(new ExpandoObject(), true, true));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(new JsonObject());
    }

    async Task Because()
    {
        _result = await _grain.GetModelInstance();
        _cached = await _grain.GetModelInstance();
    }

    [Fact] void should_report_the_empty_instance_as_present() => _result.HasReadModel.ShouldBeTrue();
    [Fact] void should_keep_the_cached_empty_instance_present() => _cached.HasReadModel.ShouldBeTrue();
}
