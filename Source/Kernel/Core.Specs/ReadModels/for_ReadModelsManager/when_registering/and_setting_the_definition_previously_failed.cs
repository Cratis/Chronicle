// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_setting_the_definition_previously_failed : given.a_read_models_manager
{
    static readonly ReadModelDefinition _definition = DefinitionFor("model", "Model");
    Exception _error;

    async Task Establish()
    {
        _readModelGrain.SetDefinition(Arg.Any<ReadModelDefinition>()).Returns(Task.FromException(new Exception("Storage unavailable")));
        _error = await Catch.Exception(() => _manager.Register([_definition]));
        _readModelGrain.SetDefinition(Arg.Any<ReadModelDefinition>()).Returns(Task.CompletedTask);
    }

    async Task Because() => await _manager.Register([_definition with { Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema>(), Indexes = [] }]);

    [Fact] void should_surface_the_original_failure() => _error.ShouldNotBeNull();
    [Fact] async Task should_retry_setting_the_definition() => await _readModelGrain.Received(2).SetDefinition(Arg.Any<ReadModelDefinition>());
    [Fact] void should_not_repeat_the_global_write() => _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(1);
}
