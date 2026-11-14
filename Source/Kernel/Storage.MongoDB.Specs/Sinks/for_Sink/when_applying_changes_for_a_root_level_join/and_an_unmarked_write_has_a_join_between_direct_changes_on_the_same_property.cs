// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

public class and_an_unmarked_write_has_a_join_between_direct_changes_on_the_same_property : given.a_sink_over_a_substituted_collection
{
    void Establish() => _changes = [Direct(("count", 1), ("x", 1)), Join(("count", 2)), Direct(("count", 3))];

    async Task Because() => await Apply();

    [Fact] void should_keep_the_single_write_of_the_join_and_then_the_direct_changes() => _writes.Select(_ => _.Kind).ShouldEqual(["many", "one"]);
    [Fact] void should_write_the_joined_value_in_the_join() => _writes[0].Update["$set"]["count"].AsInt64.ShouldEqual(2);
    [Fact] void should_write_the_other_direct_property_in_the_single_direct_write() => _writes[1].Update["$set"]["x"].AsInt64.ShouldEqual(1);
}
