// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_initial_values;

public class and_creation_changes_nothing : given.a_projection_with_initial_values
{
    async Task Establish() => await Materialize(Event(_created, 0));
    async Task Because() => _result = await Process(Event(_created, 0));

    [Fact] void should_apply_the_initial_values() => Properties(_result)["name"].ShouldEqual("Initial");
    [Fact] void should_match_the_materialized_state() => WithoutBookkeeping(_result).ShouldEqual(WithoutBookkeeping(_materialized));
    [Fact] void should_not_write_an_owned_children_collection() => Properties(_result).ContainsKey("children").ShouldBeFalse();
}
