// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Services.Projections.for_Projections.when_saving;

public class and_the_read_model_schema_is_inferred : given.an_inferred_composed_scalar_schema
{
    ReadModelDefinition _savedDefinition;
    SaveProjectionResult _saveResult;

    void Establish()
    {
        var readModelsManager = Substitute.For<IReadModelsManager>();
        readModelsManager.When(manager => manager.RegisterSingle(Arg.Any<ReadModelDefinition>()))
            .Do(call => _savedDefinition = call.Arg<ReadModelDefinition>());
        _grainFactory.GetGrain<IReadModelsManager>(Arg.Any<string>()).Returns(readModelsManager);
        var projectionsManager = Substitute.For<IProjectionsManager>();
        projectionsManager.GetProjectionDefinitions().Returns([]);
        _grainFactory.GetGrain<IProjectionsManager>(Arg.Any<string>()).Returns(projectionsManager);
    }

    async Task Because() => _saveResult = await _service.Save(new()
    {
        EventStore = EventStore,
        Namespace = EventStoreNamespace,
        EventSequenceId = "event-log",
        Declaration = Declaration,
        DraftReadModel = new() { Identifier = "saved-scalars", ContainerName = "saved-scalars", DisplayName = "Saved Scalars" }
    });

    [Fact] void should_save_without_diagnostics() => _saveResult.Errors.ShouldBeEmpty();
    [Fact] void should_register_the_inferred_properties() => _savedDefinition.GetSchemaForLatestGeneration().Properties.Keys.ShouldContainOnly("enabled", "count", "code");
    [Fact] void should_register_the_effective_nullable_type() => _savedDefinition.GetSchemaForLatestGeneration().Properties["enabled"].Type.ShouldEqual(JsonObjectType.Boolean | JsonObjectType.Null);
    [Fact] void should_register_the_effective_scalar_format() => _savedDefinition.GetSchemaForLatestGeneration().Properties["count"].Format.ShouldEqual("int64");
}
