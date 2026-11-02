// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

public class and_a_join_is_between_direct_changes_on_the_same_property : given.a_sink_over_a_substituted_collection
{
    void Establish() => _changes = [Direct(("count", 1), ("x", 1)), Join(("count", 2)), Direct(("count", 3))];

    async Task Because() => await Apply();

    [Fact] void should_write_the_first_direct_change_the_join_and_the_last_direct_change() => _writes.Select(_ => _.Kind).ShouldEqual(["one", "many", "one"]);
    [Fact] void should_write_the_other_property_first() => _writes[0].Update["$set"]["x"].AsInt64.ShouldEqual(1);
    [Fact] void should_write_the_joined_value_second() => _writes[1].Update["$set"]["count"].AsInt64.ShouldEqual(2);
    [Fact] void should_write_the_last_direct_value_last() => _writes[2].Update["$set"]["count"].AsInt64.ShouldEqual(3);
}
