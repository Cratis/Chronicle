// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks.for_Sink.when_applying_changes_for_a_root_level_join;

public class and_the_write_records_applied_event_markers : given.a_sink_over_a_substituted_collection
{
    void Establish()
    {
        _mode = SinkWriteMode.OncePerSourceEvent;
        _incoming = AppendedEvent.Empty() with { Context = EventContext.Empty with { EventType = new EventType("own", 1), EventSourceId = "source", SequenceNumber = 20UL } };
        _changes = [Direct(("count", 1)), Join(("count", 2))];
    }

    async Task Because() => await Apply();

    [Fact] void should_write_the_direct_change_and_then_the_join_in_change_order() => _writes.Select(_ => _.Kind).ShouldEqual(["one", "many"]);
    [Fact] void should_write_the_direct_change_as_a_pipeline_recording_its_marker() => _writes[0].Update.ToString().ShouldContain("__applied");
    [Fact] void should_filter_the_direct_change_on_the_document_id_alone() => _writes[0].Filter!.ElementCount.ShouldEqual(1);
    [Fact] void should_record_the_marker_of_the_join_in_its_own_slot() => _writes[1].Update.ToString().ShouldContain("__applied");
}
