// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_a_from_without_a_join_has_every_event_mappings : Specification
{
    long _withAll;
    long _withoutAll;
    long _childWithIncludedChildren;
    long _childWithExcludedChildren;

    async Task Because()
    {
        _withAll = (await and_joining_with_every_event_mappings.ProjectJoin(false, fromAlsoJoins: true, subscribeToAllEvents: true, includeJoin: false)).Count;
        _withoutAll = (await and_joining_with_every_event_mappings.ProjectJoin(false, fromAlsoJoins: true, includeJoin: false)).Count;
        _childWithIncludedChildren = (await and_joining_with_every_event_mappings.ProjectJoin(true, isChild: true, fromAlsoJoins: true, includeJoin: false)).Count;
        _childWithExcludedChildren = (await and_joining_with_every_event_mappings.ProjectJoin(false, isChild: true, fromAlsoJoins: true, includeJoin: false)).Count;
    }

    [Fact] void should_apply_once_to_a_root_with_all() => _withAll.ShouldEqual(1);
    [Fact] void should_apply_once_to_a_root_without_all() => _withoutAll.ShouldEqual(1);
    [Fact] void should_apply_once_to_a_child_with_children_included() => _childWithIncludedChildren.ShouldEqual(1);
    [Fact] void should_apply_once_to_a_child_with_children_excluded() => _childWithExcludedChildren.ShouldEqual(1);
}
