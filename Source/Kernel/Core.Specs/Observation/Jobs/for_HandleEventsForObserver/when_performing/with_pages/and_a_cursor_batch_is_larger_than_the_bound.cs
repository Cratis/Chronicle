// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Jobs.for_HandleEventsForObserver.when_performing.with_pages;

/// <summary>
/// Replay pages are sized by the read model batch size, so the per-call bound does not apply to them.
/// </summary>
public class and_a_cursor_batch_is_larger_than_the_bound : given.a_page_job_step
{
    void Establish()
    {
        _jobStep.MaxEventsPerBatch = 2;
        _eventCursor.Current.Returns(Enumerable.Range(1, 5).Select(_ => CreateEvent((ulong)_, "module")).ToArray());
    }

    async Task Because() => await _jobStep.InvokePerformStep(_performState);

    [Fact] void should_deliver_one_page() => _pages.Count.ShouldEqual(1);
    [Fact] void should_keep_all_events_in_the_page() => _pages[0].Length.ShouldEqual(5);
}
