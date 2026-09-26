// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_child_from_and_join_use_the_same_event_type : Specification
{
    (int Subscriptions, long Count, bool HasKeyedFrom) _withChildren;
    (int Subscriptions, long Count, bool HasKeyedFrom) _withoutChildren;

    async Task Because()
    {
        _withChildren = await and_joining_with_every_event_mappings.ProjectJoin(true, isChild: true, fromAlsoJoins: true);
        _withoutChildren = await and_joining_with_every_event_mappings.ProjectJoin(false, isChild: true, fromAlsoJoins: true);
    }

    [Fact] void should_apply_the_mapper_once_to_the_final_child_with_children_included() => _withChildren.Count.ShouldEqual(1);
    [Fact] void should_apply_the_mapper_once_to_the_final_child_with_children_excluded() => _withoutChildren.Count.ShouldEqual(1);
    [Fact] void should_identify_the_child_from_write() => _withChildren.HasKeyedFrom.ShouldBeTrue();
}
