// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_persistence_fails_before_a_reconnect : given.a_read_models_manager
{
    static readonly ReadModelDefinition _definition = DefinitionFor("model", "Model");
    Exception? _failure;
    IEnumerable<ReadModelDefinition> _afterFailure;

    void Establish() => _readModelGrain.SetDefinition(_definition).Returns(
        Task.FromException(new ReadModelNotFound(_definition.Identifier)), Task.CompletedTask);

    async Task Because()
    {
        _failure = await Catch.Exception(() => _manager.Register([_definition]));
        _afterFailure = await _manager.GetDefinitions();
        await _manager.Register([_definition]);
    }

    [Fact] void should_surface_the_failed_registration() => _failure.ShouldBeOfExactType<ReadModelNotFound>();
    [Fact] void should_not_accept_the_failed_definition() => _afterFailure.ShouldBeEmpty();
    [Fact] async Task should_retry_the_write_on_reconnect() => await _readModelGrain.Received(2).SetDefinition(_definition);
    [Fact] async Task should_accept_the_successful_retry() => (await _manager.GetDefinitions()).ShouldContainOnly(_definition);
}
