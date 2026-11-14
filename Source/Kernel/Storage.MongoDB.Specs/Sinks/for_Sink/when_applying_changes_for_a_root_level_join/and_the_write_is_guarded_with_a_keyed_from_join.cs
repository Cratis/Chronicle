// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

public class and_the_write_is_guarded_with_a_keyed_from_join : given.a_sink_over_a_substituted_collection
{
    void Establish()
    {
        _mode = SinkWriteMode.OnlyWhenAdvancingWatermark;
        _changes = [Direct(("count", 1)), Join(("count", 2))];
    }

    async Task Because() => await Apply();

    [Fact] void should_keep_the_single_pass_with_the_join_first() => _writes.Select(_ => _.Kind).ShouldEqual(["many", "one"]);
}
