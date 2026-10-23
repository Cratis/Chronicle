// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_initial_values;

public class and_only_a_child_has_arrived : given.a_projection_with_initial_values
{
    async Task Establish() => await Materialize(Event(_childAdded, 0));
    async Task Because() => _result = await Process(Event(_childAdded, 0));

    [Fact] void should_not_write_the_initial_root_values() => Properties(_result).ContainsKey("name").ShouldBeFalse();
    [Fact] void should_preserve_the_child() => ((IEnumerable<object>)Properties(_result)["children"]).Count().ShouldEqual(1);
    [Fact] void should_match_the_materialized_state() => WithoutBookkeeping(_result).ShouldEqual(WithoutBookkeeping(_materialized));
}
