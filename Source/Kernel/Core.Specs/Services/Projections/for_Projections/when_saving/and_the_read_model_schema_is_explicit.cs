// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using ProjectionDefinition = Cratis.Chronicle.Concepts.Projections.Definitions.ProjectionDefinition;
using SaveProjectionResult = Cratis.Chronicle.Contracts.Projections.SaveProjectionResult;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_saving;

public class and_the_read_model_schema_is_explicit : given.all_dependencies
{
    IProjectionsManager _projectionsManager;
    SaveProjectionResult _result;

    void Establish()
    {
        var eventType = new EventType("ItemAddedToOrder", EventTypeGeneration.First);
        SetCompiledDefinition(_projectionDefinition with
        {
            ReadModel = ReadModelName,
            From = new Dictionary<EventType, FromDefinition>
            {
                [eventType] = new(new Dictionary<PropertyPath, string> { ["ItemCount"] = WellKnownExpressions.Increment }, WellKnownExpressions.EventSourceId, null)
            },
            Children = new Dictionary<PropertyPath, ChildrenDefinition>
            {
                ["Lines"] = new(
                    "ProductId",
                    new Dictionary<EventType, FromDefinition>
                    {
                        [eventType] = new(new Dictionary<PropertyPath, string> { ["Quantity"] = "Quantity" }, "ProductId", null)
                    },
                    new Dictionary<EventType, JoinDefinition>(),
                    new Dictionary<PropertyPath, ChildrenDefinition>(),
                    new(new Dictionary<PropertyPath, string>(), false),
                    new Dictionary<EventType, RemovedWithDefinition>(),
                    new Dictionary<EventType, RemovedWithJoinDefinition>())
            }
        });
        _grainFactory.GetGrain<IReadModelsManager>(Arg.Any<string>()).Returns(Substitute.For<IReadModelsManager>());
        _projectionsManager = Substitute.For<IProjectionsManager>();
        _projectionsManager.GetProjectionDefinitions().Returns([]);
        _grainFactory.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(_projectionsManager);
    }

    async Task Because() => _result = await _service.Save(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = "projection OrderOverview => TestReadModel"
    });

    [Fact] void should_save_without_errors() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_register_the_projection() => _projectionsManager.Received(1).Register(Arg.Any<IEnumerable<ProjectionDefinition>>());
}
