// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_initial_values;

public class and_the_root_is_removed_then_recreated : given.a_projection_with_initial_values
{
    async Task Establish() => await Materialize(Event(_created, 0), Event(_removed, 1), Event(_created, 2));
    async Task Because() => _result = await Process(Event(_created, 0), Event(_removed, 1), Event(_created, 2));

    [Fact] void should_restore_the_declared_name() => Properties(_result)["name"].ShouldEqual("Initial");
    [Fact] void should_restore_the_declared_value() => Properties(_result)["value"].ShouldEqual(42);
    [Fact] void should_match_the_materialized_state() => WithoutBookkeeping(_result).ShouldEqual(WithoutBookkeeping(_materialized));
}
