// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Definitions;

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_definition_changes_during_a_read;

public class and_several_changes_arrive : given.a_read_waiting_for_a_fold
{
    ProjectionDefinition _definitionWhileReading;
    ProjectionResult _result;

    async Task Because()
    {
        await _grain.OnProjectionDefinitionsChanged(_changed);
        await _grain.OnProjectionDefinitionsChanged(_latest);
        _definitionWhileReading = _grain.Definition;
        _fold.SetResult(_folded);
        _result = await _read;
    }

    [Fact] void should_apply_only_the_latest_change() => _grain.AppliedDefinitions.Single().ShouldBeSame(_latest);
    [Fact] void should_keep_the_reads_definition_while_it_is_active() => _definitionWhileReading.ShouldBeSame(_readDefinition);
    [Fact] void should_complete_the_read_with_its_model() => _result.HasReadModel.ShouldBeTrue();
}
