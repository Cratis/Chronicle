// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_in_order;

public class and_a_join_follows_a_direct_change : given.a_sink_recording_writes
{
    void Establish() => _changes = [Direct(10), Join(20)];

    async Task Because() => await Apply();

    [Fact] void should_store_the_value_computed_last() => _documents["root"]["value"].AsInt32.ShouldEqual(20);
    [Fact] void should_write_in_changeset_order() => _writes.ShouldEqual(["direct:10", "join:20"]);
    [Fact] void should_update_the_document_touched_only_by_the_join() => _documents["joined-only"]["value"].AsInt32.ShouldEqual(20);
    [Fact] void should_leave_unrelated_documents_unchanged() => _documents["direct-only"]["value"].AsInt32.ShouldEqual(0);
}
