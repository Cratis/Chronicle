// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_initial_values;

public class and_two_instances_are_created : given.a_projection_with_initial_values
{
    IEnumerable<ExpandoObject> _results;

    async Task Because() => _results = await _grain.Process(
        EventStoreNamespaceName.Default,
        [Event(_created, 0), Event(_created, 1, "other"), Event(_updated, 2, "other")]);

    [Fact] void should_create_both_instances() => _results.Count().ShouldEqual(2);
    [Fact] void should_keep_the_initial_value_on_the_untouched_instance() => Properties(ById("key"))["value"].ShouldEqual(42);
    [Fact] void should_not_leak_the_update_into_the_untouched_instance() => Properties(ById("key"))["value"].ShouldNotEqual(99);
    [Fact] void should_apply_the_update_to_the_other_instance() => Properties(ById("other"))["value"].ShouldEqual(99);
    [Fact] void should_give_the_other_instance_its_initial_name() => Properties(ById("other"))["name"].ShouldEqual("Initial");

    ExpandoObject ById(string id) => _results.Single(r => Properties(r)["id"].Equals(id));
}
