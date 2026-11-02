// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

public class and_a_direct_change_precedes_a_join_on_the_same_property : given.a_sink_over_a_substituted_collection
{
    void Establish() => _changes = [Direct(("count", 1)), Join(("count", 2))];

    async Task Because() => await Apply();

    [Fact] void should_write_the_direct_change_and_then_the_join() => _writes.Select(_ => _.Kind).ShouldEqual(["one", "many"]);
    [Fact] void should_write_the_direct_value_first() => _writes[0].Update["$set"]["count"].AsInt64.ShouldEqual(1);
    [Fact] void should_write_the_joined_value_last() => _writes[1].Update["$set"]["count"].AsInt64.ShouldEqual(2);
}
