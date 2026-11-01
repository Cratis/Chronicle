// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Orleans.Core;
using Orleans.TestKit;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_the_global_write_previously_failed : Specification
{
    readonly TestKitSilo _silo = new();
    IStorage<ReadModelsManagerState> _storage;
    IReadModel _readModel;
    ReadModelsManager _manager;
    ReadModelDefinition _definition;
    Exception _error;

    async Task Establish()
    {
        _storage = Substitute.For<IStorage<ReadModelsManagerState>>();
        _storage.State = new ReadModelsManagerState();
        _silo.Options.StorageFactory = _ => _storage;
        _readModel = Substitute.For<IReadModel>();
        _silo.AddProbe(_ => _readModel);
        _silo.AddService(Substitute.For<IProjectionPipelineManager>());
        _manager = await _silo.CreateGrainAsync<ReadModelsManager>("store");
        _definition = given.a_read_models_manager.DefinitionFor("model", "Model");
        _storage.WriteStateAsync().Returns(Task.FromException(new Exception("Unavailable")));
        _error = await Catch.Exception(() => _manager.Register([_definition]));
        _storage.WriteStateAsync().Returns(Task.CompletedTask);
        _silo.AddProbe(_ => Substitute.For<Projections.IProjectionsManager>());
    }

    async Task Because() => await _manager.Register([_definition]);

    [Fact] void should_surface_the_original_failure() => _error.ShouldNotBeNull();
    [Fact] async Task should_retry_the_global_write() => await _storage.Received(2).WriteStateAsync();
    [Fact] async Task should_propagate_only_after_persistence_succeeds() => await _readModel.Received(1).SetDefinition(_definition);
}
