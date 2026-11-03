// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ImmediateProjection.when_definition_changes_during_a_read;

public class and_another_read_follows : given.a_read_waiting_for_a_fold
{
    async Task Because()
    {
        await _grain.OnProjectionDefinitionsChanged(_changed);
        _fold.SetResult(_folded);
        await _read;
        await _grain.GetModelInstance();
    }

    [Fact] void should_not_apply_the_change_again() => _grain.AppliedDefinitions.Count.ShouldEqual(1);
    [Fact] async Task should_resolve_the_definition_again() => await _projection.Received(2).GetDefinition();
}
