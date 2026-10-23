// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_initial_values;

public class and_an_update_follows_creation : given.a_projection_with_initial_values
{
    async Task Establish() => await Materialize(Event(_created, 0), Event(_updated, 1));
    async Task Because() => _result = await Process(Event(_created, 0), Event(_updated, 1));

    [Fact] void should_apply_the_updated_value() => Properties(_result)["value"].ShouldEqual(99);
    [Fact] void should_keep_the_other_initial_values() => Properties(_result)["name"].ShouldEqual("Initial");
    [Fact] void should_match_the_materialized_state() => WithoutBookkeeping(_result).ShouldEqual(WithoutBookkeeping(_materialized));
}
