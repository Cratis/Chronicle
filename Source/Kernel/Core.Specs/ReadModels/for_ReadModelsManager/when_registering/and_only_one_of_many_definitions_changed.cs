// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

public class and_only_one_of_many_definitions_changed : given.a_read_models_manager
{
    static readonly ReadModelDefinition _changedBefore = DefinitionFor("changed", "Before");
    static readonly ReadModelDefinition _changedAfter = DefinitionFor("changed", "After");
    static readonly ReadModelDefinition _unchanged = DefinitionFor("unchanged", "Same");
    static readonly ReadModelDefinition _new = DefinitionFor("new", "New");

    async Task Establish()
    {
        await _manager.Register([_changedBefore, _unchanged]);
        _readModelGrain.ClearReceivedCalls();
    }

    async Task Because() => await _manager.Register([_changedAfter, _unchanged with { }, _new]);

    [Fact] async Task should_set_the_changed_definition() => await _readModelGrain.Received(1).SetDefinition(_changedAfter);
    [Fact] async Task should_set_the_new_definition() => await _readModelGrain.Received(1).SetDefinition(_new);
    [Fact] async Task should_not_set_the_unchanged_definition() => await _readModelGrain.DidNotReceive().SetDefinition(_unchanged);
}
