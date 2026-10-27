// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Schemas;
using Orleans.Core;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_getting_model_instance;

public class and_the_definition_changed_without_a_notification : given.an_immediate_projection
{
    IStorage<ProjectionDefinition> _stateStorage;
    ProjectionResult _first;
    ProjectionResult _reread;

    void Establish()
    {
        _stateStorage = (IStorage<ProjectionDefinition>)_silo.Options.StorageFactory(typeof(ProjectionDefinition));
        _projection.GetDefinition().Returns(
            CreateProjectionDefinition() with { InitialModelState = new JsonObject { ["name"] = "seeded" } },
            CreateProjectionDefinition() with { InitialModelState = new JsonObject { ["name"] = "refreshed" } });
        _expandoObjectConverter.ToExpandoObject(Arg.Is<JsonObject>(json => json.ContainsKey("name")), Arg.Any<JsonSchema>())
            .Returns(call =>
            {
                var model = new ExpandoObject();
                ((IDictionary<string, object?>)model)["name"] = call.Arg<JsonObject>()["name"]!.GetValue<string>();
                return model;
            });
        _projection.GetEventTypes().Returns([new EventType("Created", EventTypeGeneration.First)]);
        _eventSequenceStorage.GetFromSequenceNumber(EventSequenceNumber.First, eventSourceId: ReadModelKey, eventTypes: Arg.Any<IEnumerable<EventType>>())
            .Returns(_ => CreateCursor());
        _expandoObjectConverter.ToJsonObject(Arg.Any<ExpandoObject>(), Arg.Any<JsonSchema>())
            .Returns(call => new JsonObject { ["name"] = (string)((IDictionary<string, object?>)call.Arg<ExpandoObject>())["name"]! });
    }

    async Task Because()
    {
        _first = await _grain.GetModelInstance();
        _reread = await _grain.GetModelInstance();
    }

    [Fact] void should_have_a_read_model() => _first.HasReadModel.ShouldBeTrue();
    [Fact] void should_not_count_any_events() => _first.ProjectedEventsCount.ShouldEqual(0);
    [Fact] void should_use_the_current_initial_model_state() => _first.ReadModel["name"]!.GetValue<string>().ShouldEqual("seeded");
    [Fact] void should_use_the_changed_initial_model_state_on_the_next_read() => _reread.ReadModel["name"]!.GetValue<string>().ShouldEqual("refreshed");
    [Fact] async Task should_fetch_the_definition_on_every_read() => await _projection.Received(2).GetDefinition();
    [Fact] async Task should_not_write_definition_state() => await _stateStorage.DidNotReceive().WriteStateAsync();
    [Fact] async Task should_not_clear_definition_state() => await _stateStorage.DidNotReceive().ClearStateAsync();
}
