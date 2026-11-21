// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_joins_surround_a_direct_change : given.a_sink_recording_writes
{
    void Establish() => _changes = [Join(5), Direct(10), Join(20)];

    async Task Because() => await Apply();

    [Fact] void should_write_in_changeset_order() => _writes.ShouldEqual(["join:5", "direct:10", "join:20"]);
    [Fact] void should_store_the_last_join_value() => _documents["root"]["value"].AsInt32.ShouldEqual(20);
}
