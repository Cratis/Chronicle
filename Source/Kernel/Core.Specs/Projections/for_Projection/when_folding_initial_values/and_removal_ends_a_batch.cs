// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_folding_initial_values;

public class and_removal_ends_a_batch : given.a_projection_with_initial_values
{
    SingleReadModelProjectionState _removedState;
    SingleReadModelProjectionState _recreatedState;

    async Task Establish()
    {
        _removedState = await FoldOnto(SingleReadModelProjectionState.Empty, Event(_created, 0), Event(_removed, 1));
        await Materialize(Event(_created, 0), Event(_removed, 1), Event(_created, 2));
    }

    async Task Because() => _recreatedState = await FoldOnto(_removedState, Event(_created, 2));

    [Fact] void should_carry_absence_across_the_batch_boundary() => _removedState.Exists.ShouldBeFalse();
    [Fact] void should_carry_an_uninitialized_root_across_the_batch_boundary() => _removedState.IsInitialized.ShouldBeFalse();
    [Fact] void should_create_the_instance_again() => _recreatedState.Exists.ShouldBeTrue();
    [Fact] void should_initialize_the_instance_again() => _recreatedState.IsInitialized.ShouldBeTrue();
    [Fact] void should_restore_the_initial_values() => Properties(_recreatedState.Model)["name"].ShouldEqual("Initial");
    [Fact] void should_match_the_materialized_state() => Properties(_recreatedState.Model).ShouldEqual(Properties(_materialized));
}
