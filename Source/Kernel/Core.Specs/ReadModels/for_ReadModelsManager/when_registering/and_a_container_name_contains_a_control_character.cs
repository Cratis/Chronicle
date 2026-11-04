// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_container_name_contains_a_control_character : given.a_read_models_manager
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.Register([DefinitionFor("read-model", "Read model") with { ContainerName = "books\0inventory" }]));

    [Fact] void should_refuse_the_container_name() => _error.ShouldBeOfExactType<InvalidReadModelContainerName>();
    [Fact] async Task should_not_register_any_definition() => (await _manager.GetDefinitions()).ShouldBeEmpty();
}
