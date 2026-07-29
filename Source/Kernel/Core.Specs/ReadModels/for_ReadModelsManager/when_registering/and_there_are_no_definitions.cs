// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

/// <summary>
/// A client with no read models still registers - a reactor-only application has none - and registering an empty
/// set must not wipe what is already there. That is what makes the client's unconditional Register() call safe.
/// </summary>
public class and_there_are_no_definitions : given.a_read_models_manager
{
    static readonly ReadModelDefinition _existing = DefinitionFor("some-read-model", "Some read model");

    IEnumerable<ReadModelDefinition> _result;

    async Task Establish() => await _manager.Register([_existing]);

    async Task Because()
    {
        await _manager.Register([]);
        _result = await _manager.GetDefinitions();
    }

    [Fact] void should_keep_the_definition_that_was_already_registered() => _result.ShouldContainOnly(_existing);
}
