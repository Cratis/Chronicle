// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels.for_ReadModelsManager.given;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModel.when_setting_definition;

public class and_the_previous_write_failed : Specification
{
    readonly TestKitSilo _silo = new();
    IStorage<ReadModelDefinition> _storage;
    ReadModel _readModel;
    ReadModelDefinition _definition;
    Exception _error;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage<ReadModelDefinition>>();
        _silo.Options.StorageFactory = _ => _storage;
        _readModel = await _silo.CreateGrainAsync<ReadModel>("model");
        _definition = a_read_models_manager.DefinitionFor("model", "Model");
        _storage.WriteStateAsync().Returns(Task.FromException(new Exception("Unavailable")));
        _error = await Catch.Exception(() => _readModel.SetDefinition(_definition));
        _storage.WriteStateAsync().Returns(Task.CompletedTask);
    }

    async Task Because() => await _readModel.SetDefinition(_definition with { Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), Indexes = [] });

    [Fact] void should_surface_the_original_failure() => _error.ShouldNotBeNull();
    [Fact] async Task should_retry_persistence() => await _storage.Received(2).WriteStateAsync();
}
