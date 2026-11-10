// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

/// <summary>
/// Six events fill two chunks of three exactly. The producer stops after adding them, without completing, as a producer that
/// drops its connection does; both whole chunks have been handed to the kernel, so a reattached session continues after all six.
/// </summary>
public class and_the_events_fill_whole_chunks : given.an_event_import
{
    async Task Because()
    {
        await Import.Add(Events(6));
        await Import.DisposeAsync();
    }

    [Fact] void should_send_both_chunks() => _chunks.Select(_ => _.ChunkIndex).Order().ShouldEqual([0, 1]);
    [Fact] void should_send_three_events_in_each_chunk() => _chunks.ShouldEachConformTo(_ => _.Events.Count() == 3);
    [Fact] void should_not_complete_the_import() => _completed.ShouldBeNull();
}
