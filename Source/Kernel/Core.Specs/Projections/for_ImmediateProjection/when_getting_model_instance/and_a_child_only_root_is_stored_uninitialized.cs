// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_a_child_only_root_is_stored_uninitialized : given.an_immediate_projection
{
    ProjectionResult _result;
    ProjectionResult _cached;

    void Establish()
    {
        _visibilities.GetFor(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), Arg.Any<ReadModelDefinition>()).Returns(ReadModelInstanceVisibility.IncludeUninitialized);
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>()).Returns(_ => new JsonObject { ["name"] = "Child-only before upgrade" });
        var joined = new EventType("Joined", EventTypeGeneration.First);
        _projection.GetEventTypes().Returns([joined]);
        _eventSequence.GetTailSequenceNumberForEventTypes(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)1);
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => CreateCursor(AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(joined, 1)));
        var model = new ExpandoObject();
        ((IDictionary<string, object?>)model)["name"] = "Child-only before upgrade";
        _projection.FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(new SingleReadModelProjectionState(model, true, false));
    }

    async Task Because()
    {
        _result = await _grain.GetModelInstance();
        _cached = await _grain.GetModelInstance();
    }

    [Fact] void should_report_the_root_as_present() => _result.HasReadModel.ShouldBeTrue();
    [Fact] void should_keep_the_cached_root_present() => _cached.HasReadModel.ShouldBeTrue();
    [Fact] void should_return_the_root_properties() => _result.ReadModel["name"]!.GetValue<string>().ShouldEqual("Child-only before upgrade");
}
