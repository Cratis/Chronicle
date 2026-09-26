// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_creating;

public class and_from_and_join_use_the_same_event_type_with_all : Specification
{
    (int Subscriptions, long Count, bool HasKeyedFrom) _result;

    async Task Because() => _result = await and_joining_with_every_event_mappings.ProjectJoin(false, fromAlsoJoins: true, subscribeToAllEvents: true);

    [Fact] void should_apply_the_every_mapper_once_to_the_final_state() => _result.Count.ShouldEqual(1);
    [Fact] void should_identify_the_keyed_from_write() => _result.HasKeyedFrom.ShouldBeTrue();
}
