// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_initial_values;

public class and_only_a_child_has_arrived : given.a_projection_with_initial_values
{
    IEnumerable<ExpandoObject> _processed;

    async Task Establish() => await Materialize(Event(_childAdded, 0));
    async Task Because() => _processed = await _grain.Process(EventStoreNamespaceName.Default, [Event(_childAdded, 0)]);

    [Fact] void should_hide_the_uninitialized_root_from_processed_results() => _processed.ShouldBeEmpty();
    [Fact] void should_not_write_the_initial_root_values() => Properties(_materialized).ContainsKey("name").ShouldBeFalse();
    [Fact] void should_preserve_the_child() => ((IEnumerable<object>)Properties(_materialized)["children"]).Count().ShouldEqual(1);
}
