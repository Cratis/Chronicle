// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_joining_with_every_event_mappings_in_children : Specification
{
    long _withChildren;
    long _withoutChildren;

    async Task Because()
    {
        _withChildren = (await and_joining_with_every_event_mappings.ProjectJoin(true, isChild: true)).Count;
        _withoutChildren = (await and_joining_with_every_event_mappings.ProjectJoin(false, isChild: true)).Count;
    }

    [Fact] void should_count_the_join_once_when_children_are_included() => _withChildren.ShouldEqual(1);
    [Fact] void should_count_the_join_once_when_children_are_excluded() => _withoutChildren.ShouldEqual(1);
}
