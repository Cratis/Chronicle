// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_folding_initial_values;

public class and_the_root_stays_removed : given.a_projection_with_initial_values
{
    SingleReadModelProjectionState _state;

    async Task Because() => _state = await FoldOnto(SingleReadModelProjectionState.Empty, Event(_created, 0), Event(_removed, 1));

    [Fact] void should_be_absent() => _state.Exists.ShouldBeFalse();
    [Fact] void should_be_uninitialized() => _state.IsInitialized.ShouldBeFalse();
    [Fact] void should_not_retain_properties_from_the_removed_instance() => _state.Model.ShouldBeEmpty();
}
