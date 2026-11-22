// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_container_differs : given.a_read_models_manager
{
    [Theory]
    [InlineData("Container", 1U)]
    [InlineData("another-container", 2U)]
    public async Task should_refuse_case_only_and_later_generation_drift(string requestedContainer, uint generation)
    {
        var original = DefinitionFor("model", "Model") with
        {
            ContainerName = "container",
            Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema> { [ReadModelGeneration.First] = new() }
        };
        await _manager.RegisterSingle(original);
        var changed = original with
        {
            ContainerName = requestedContainer,
            Schemas = new Dictionary<ReadModelGeneration, Schemas.JsonSchema> { [(ReadModelGeneration)generation] = new() }
        };

        var error = await Catch.Exception(() => _manager.RegisterSingle(changed));

        error.ShouldBeOfExactType<ReadModelContainerChangeRequiresExplicitUpdate>();
        (await _manager.GetDefinitions()).ShouldContainOnly(original);
        _silo.StorageManager.GetStorageStats(typeof(ReadModelsManager).FullName)!.Writes.ShouldEqual(1);
    }
}
