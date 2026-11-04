// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_a_container_name_is_conventional : given.a_read_models_manager
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _manager.Register([DefinitionFor("read-model", "Read model") with { ContainerName = "books-inventory_v2.Read" }]));

    [Fact] void should_accept_the_container_name() => _error.ShouldBeNull();
    [Fact] async Task should_register_the_definition() => (await _manager.GetDefinitions()).Count().ShouldEqual(1);
}
