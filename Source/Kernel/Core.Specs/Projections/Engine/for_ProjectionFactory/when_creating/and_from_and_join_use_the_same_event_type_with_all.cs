// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_from_and_join_use_the_same_event_type_with_all : Specification
{
    long _count;

    async Task Because() => _count = (await and_joining_with_every_event_mappings.ProjectJoin(false, fromAlsoJoins: true, subscribeToAllEvents: true)).Count;

    [Fact] void should_apply_the_every_mapper_once() => _count.ShouldEqual(1);
}
