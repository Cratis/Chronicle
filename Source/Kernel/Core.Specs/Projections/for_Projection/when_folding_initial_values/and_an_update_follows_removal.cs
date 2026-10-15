// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_folding_initial_values;

public class and_an_update_follows_removal : given.a_projection_with_initial_values
{
    SingleReadModelProjectionState _state;

    async Task Because() => _state = await FoldOnto(SingleReadModelProjectionState.Empty, Event(_created, 0), Event(_removed, 1), Event(_updated, 2));

    [Fact] void should_not_recreate_the_instance() => _state.Exists.ShouldBeFalse();
    [Fact] void should_not_initialize_the_root() => _state.IsInitialized.ShouldBeFalse();
    [Fact] void should_not_retain_the_initial_values() => _state.Model.ShouldBeEmpty();
}
