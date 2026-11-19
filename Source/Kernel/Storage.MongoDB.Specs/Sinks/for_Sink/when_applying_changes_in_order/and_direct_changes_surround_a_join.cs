// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_direct_changes_surround_a_join : given.a_sink_recording_writes
{
    void Establish() => _changes = [Direct(10), Join(20), Direct(30)];

    async Task Because() => await Apply();

    [Fact] void should_write_in_changeset_order() => _writes.ShouldEqual(["direct:10", "join:20", "direct:30"]);
    [Fact] void should_store_the_final_direct_value() => _documents["root"]["value"].AsInt32.ShouldEqual(30);
    [Fact] void should_preserve_the_value_on_the_join_only_document() => _documents["joined-only"]["value"].AsInt32.ShouldEqual(20);
}
