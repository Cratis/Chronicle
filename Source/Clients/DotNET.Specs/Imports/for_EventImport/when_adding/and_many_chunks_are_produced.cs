// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports.for_EventImport.when_adding;

public class and_many_chunks_are_produced : given.an_event_import
{
    Task _adding;
    bool _blockedWhileFull;

    void Establish()
    {
        _options = ImportOptions.Default with { MaxChunksInFlight = 2 };
        _maxChunkEvents = 1;
        _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    async Task Because()
    {
        _adding = Import.Add(Events(5));
        await Task.Delay(100);
        _blockedWhileFull = !_adding.IsCompleted;
        _gate!.SetResult();
        await _adding;
        await Import.Complete();
    }

    [Fact] void should_wait_while_the_in_flight_bound_is_reached() => _blockedWhileFull.ShouldBeTrue();
    [Fact] void should_keep_at_most_the_bound_in_flight() => _maxInFlight.ShouldEqual(2);
    [Fact] void should_send_every_chunk() => _chunks.Count.ShouldEqual(5);
}
