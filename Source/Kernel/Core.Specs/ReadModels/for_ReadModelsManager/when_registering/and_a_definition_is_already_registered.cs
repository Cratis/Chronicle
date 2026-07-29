// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels.for_ReadModelsManager.when_registering;

/// <summary>
/// Registering is an upsert keyed on the identifier, so a client reconnecting with a changed read model replaces
/// its definition rather than accumulating a second one alongside it.
/// </summary>
public class and_a_definition_is_already_registered : given.a_read_models_manager
{
    static readonly ReadModelDefinition _first = DefinitionFor("some-read-model", "First display name");
    static readonly ReadModelDefinition _second = DefinitionFor("some-read-model", "Second display name");
    static readonly ReadModelDefinition _other = DefinitionFor("some-other-read-model", "Another read model");

    IEnumerable<ReadModelDefinition> _result;

    async Task Establish() => await _manager.Register([_first, _other]);

    async Task Because()
    {
        await _manager.Register([_second]);
        _result = await _manager.GetDefinitions();
    }

    [Fact] void should_replace_the_definition_with_the_same_identifier() => _result.ShouldContain(_second);
    [Fact] void should_not_keep_the_previous_definition() => _result.ShouldNotContain(_first);
    [Fact] void should_leave_the_unrelated_definition_alone() => _result.ShouldContain(_other);
    [Fact] void should_not_accumulate_duplicates() => _result.Count().ShouldEqual(2);
    [Fact] async Task should_push_the_new_definition_to_the_read_model() => await _readModelGrain.Received(1).SetDefinition(_second);
}
