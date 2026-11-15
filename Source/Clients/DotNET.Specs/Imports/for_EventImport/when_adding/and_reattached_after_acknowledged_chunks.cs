// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_reattached_after_acknowledged_chunks : given.an_event_import
{
    void Establish()
    {
        _acknowledgedChunks = 4;
        _acknowledgedEvents = 12;
    }

    async Task Because()
    {
        await Import.Add(Events(4));
        await Import.Complete();
    }

    [Fact] void should_continue_with_the_next_chunk_index() => _chunks.Select(_ => _.ChunkIndex).Order().ShouldEqual([4, 5]);
    [Fact] void should_continue_the_event_indexes_after_the_acknowledged_events() => _chunks.Min(_ => _.FirstEventIndex).ShouldEqual(12L);
    [Fact] void should_complete_with_every_event_of_the_import() => (_completed!.ExpectedChunks, _completed.ExpectedEvents).ShouldEqual((6, 16L));
    [Fact] void should_say_where_the_producer_resumes() => Import.EventsAcknowledged.ShouldEqual(12L);
}
