// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_folding_initial_values;

public class and_only_a_child_has_arrived : given.a_projection_with_initial_values
{
    SingleReadModelProjectionState _state;

    async Task Because() => _state = await FoldOnto(SingleReadModelProjectionState.Empty, Event(_childAdded, 0));

    [Fact] void should_create_a_placeholder() => _state.Exists.ShouldBeTrue();
    [Fact] void should_keep_the_root_uninitialized() => _state.IsInitialized.ShouldBeFalse();
    [Fact] void should_not_write_the_initial_root_values() => Properties(_state.Model).ContainsKey("name").ShouldBeFalse();
    [Fact] void should_preserve_the_child() => ((IEnumerable<object>)Properties(_state.Model)["children"]).Count().ShouldEqual(1);
}
