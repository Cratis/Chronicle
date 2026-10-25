// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Schemas;
using Orleans.Core;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_definition_was_registered_after_activation : given.an_immediate_projection
{
    readonly EventType _eventType = new("Created", EventTypeGeneration.First);
    IStorage<ProjectionDefinition> _stateStorage;
    ProjectionResult _result;

    protected override ProjectionDefinition DefinitionAtActivation => CreateProjectionDefinition() with { ReadModel = null! };

    void Establish()
    {
        _stateStorage = (IStorage<ProjectionDefinition>)_silo.Options.StorageFactory(typeof(ProjectionDefinition));
        _projection.GetDefinition().Returns(CreateProjectionDefinition());
        _projection.GetEventTypes().Returns([_eventType]);
        var created = AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(_eventType, 7);
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => CreateCursor(created));
        var model = new ExpandoObject();
        ((IDictionary<string, object?>)model)["name"] = "Created";
        _projection.FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Any<IEnumerable<AppendedEvent>>())
            .Returns(new SingleReadModelProjectionState(model, true, true));
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(new JsonObject { ["name"] = "Created" });
    }

    async Task Because() => _result = await _grain.GetModelInstance();

    [Fact] void should_have_a_read_model() => _result.HasReadModel.ShouldBeTrue();
    [Fact] void should_count_the_persisted_event() => _result.ProjectedEventsCount.ShouldEqual(1);
    [Fact] void should_report_the_last_handled_event() => _result.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)7);
    [Fact] async Task should_fold_the_persisted_events() => await _projection.Received(1).FoldForSingleReadModel(EventStoreNamespaceName.Default, Arg.Any<SingleReadModelProjectionState>(), Arg.Is<IEnumerable<AppendedEvent>>(events => events.Count() == 1 && events.Single().Context.SequenceNumber == (EventSequenceNumber)7));
    [Fact] async Task should_not_write_definition_state() => await _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_clear_definition_state() => await _stateStorage.DidNotReceive().ClearStateAsync();
}
