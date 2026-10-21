// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_folding_initial_values;

public class and_a_child_ends_a_batch : given.a_projection_with_initial_values
{
    SingleReadModelProjectionState _placeholder;
    SingleReadModelProjectionState _initialized;

    async Task Establish() => _placeholder = await FoldOnto(SingleReadModelProjectionState.Empty, Event(_childAdded, 0));
    async Task Because() => _initialized = await FoldOnto(_placeholder, Event(_created, 1));

    [Fact] void should_carry_the_placeholder_across_the_batch_boundary() => _placeholder.Exists.ShouldBeTrue();
    [Fact] void should_carry_the_uninitialized_root_across_the_batch_boundary() => _placeholder.IsInitialized.ShouldBeFalse();
    [Fact] void should_initialize_the_root_on_creation() => _initialized.IsInitialized.ShouldBeTrue();
    [Fact] void should_apply_the_initial_root_values() => Properties(_initialized.Model)["name"].ShouldEqual("Initial");
    [Fact] void should_preserve_the_child() => ((IEnumerable<object>)Properties(_initialized.Model)["children"]).Count().ShouldEqual(1);
}
