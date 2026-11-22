// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.Engine.DeclarationLanguage;
using Cratis.Chronicle.ReadModels;
using Cratis.Monads;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_saving;

public class and_the_container_is_selected_deliberately : given.all_dependencies
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task should_use_explicit_update_for_existing_models_and_registration_for_new_models(bool graph, bool exists)
    {
        var manager = Substitute.For<IReadModelsManager>();
        manager.GetDefinitions().Returns(exists ? [_readModelDefinition] : []);
        _grainFactory.GetGrain<IReadModelsManager>(Arg.Any<string>()).Returns(manager);
        var projections = Substitute.For<IProjectionsManager>();
        projections.GetProjectionDefinitions().Returns([]);
        _grainFactory.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(projections);
        _languageService.IsGraphDeclaration(Arg.Any<string>()).Returns(graph);
        _languageService.CompileGraph(Arg.Any<string>(), Arg.Any<ProjectionOwner>(), Arg.Any<IEnumerable<EventTypeSchema>>(), Arg.Any<EventGenerationDelivery>())
            .Returns(Result.Success<CompiledGraph, CompilerErrors>(new(_projectionDefinition, _readModelDefinition with { ContainerName = "selected-container" })));

        var result = await _service.Save(new()
        {
            EventStore = EventStore,
            Namespace = EventStoreNamespace,
            EventSequenceId = "event-log",
            Declaration = "projection",
            DraftReadModel = new()
            {
                Identifier = _readModelDefinition.Identifier,
                ContainerName = "selected-container",
                DisplayName = "Model",
                Schema = _readModelDefinition.GetSchemaForLatestGeneration().ToJson()
            }
        });

        result.Errors.ShouldBeEmpty();
        await manager.Received(exists ? 1 : 0).UpdateDefinition(Arg.Is<ReadModelDefinition>(_ => _.ContainerName == "selected-container"));
        await manager.Received(exists ? 0 : 1).RegisterSingle(Arg.Is<ReadModelDefinition>(_ => _.ContainerName == "selected-container"));
        await projections.Received(1).Register(Arg.Any<IEnumerable<Chronicle.Concepts.Projections.Definitions.ProjectionDefinition>>());
    }
}
