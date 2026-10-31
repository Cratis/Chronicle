// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.ReadModels.for_ReadModelsManager.given;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModel.when_setting_definition;

public class and_collections_are_reconstructed : Specification
{
    readonly TestKitSilo _silo = new();
    IStorage<ReadModelDefinition> _storage;
    ReadModel _readModel;
    ReadModelDefinition _definition;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage<ReadModelDefinition>>();
        _silo.Options.StorageFactory = _ => _storage;
        _readModel = await _silo.CreateGrainAsync<ReadModel>("model");
        _definition = a_read_models_manager.DefinitionFor("model", "Model");
        await _readModel.SetDefinition(_definition);
    }

    async Task Because() => await _readModel.SetDefinition(_definition with { Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), Indexes = [] });

    [Fact] async Task should_persist_only_once() => await _storage.Received(1).WriteStateAsync();
}
