// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_for_a_single_read_model;

public class and_the_root_operation_is_a_join : given.a_projection_grain_with_a_child_projection
{
    async Task Because() => await ProcessTheEvent();

    [Fact] void should_hand_the_event_to_the_root_projection() => _rootContext.ShouldNotBeNull();
    [Fact] void should_descend_into_the_child_projection() => _childContext.ShouldNotBeNull();
    [Fact] void should_hand_the_root_projection_a_joining_context() => _rootContext.IsJoin.ShouldBeTrue();
    [Fact] void should_carry_the_parents_needs_initial_state_to_the_child() => _childContext.NeedsInitialState.ShouldEqual(_rootContext.NeedsInitialState);
    [Fact] void should_not_treat_the_parent_join_as_a_need_for_initial_state() => _childContext.NeedsInitialState.ShouldBeFalse();
    [Fact] void should_carry_the_parents_join_key_to_the_child() => _childContext.JoinKey.ShouldEqual(_rootContext.JoinKey);
    [Fact] void should_give_the_child_its_own_resolved_key() => _childContext.Key.ShouldEqual(_childKey);
    [Fact] void should_give_the_child_its_own_operation_type() => _childContext.OperationType.ShouldEqual(ChildOperationType);
    [Fact] void should_share_the_changeset_with_the_child() => _childContext.Changeset.ShouldBeSame(_rootContext.Changeset);
    [Fact] void should_share_the_event_with_the_child() => _childContext.Event.ShouldBeSame(_rootContext.Event);
}
