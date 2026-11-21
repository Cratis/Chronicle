// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding.when_the_constraint_index_is_unavailable;

public class and_it_is_a_later_chunk : given.an_event_seeding_grain
{
    SeedingEntry[] _entries;
    SeedingResult _result;

    void Establish()
    {
        _entries = [.. Enumerable.Range(0, 150).Select(AnEntry)];
        AppendManyReturns(call => call == 1 ? AnIndexUnavailableResult() : ASuccessfulResult());
    }

    async Task Because() => _result = await _grain.SeedWithResult(_entries);

    [Fact] void should_retry_only_the_unavailable_chunk() => AppendAttempts.ShouldEqual(3);
    [Fact] void should_wait_before_retrying_the_later_chunk() => _retryDelays.ShouldEqual([TimeSpan.FromSeconds(1)]);
    [Fact] void should_record_both_chunks_by_event_type() => TrackedByEventType.Select(_ => _.Content).ShouldEqual(_entries.Select(_ => _.Content));
    [Fact] void should_record_both_chunks_by_event_source() => TrackedByEventSource.Select(_ => _.Content).ShouldEqual(_entries.Select(_ => _.Content));
    [Fact] void should_persist_both_chunks() => _state.Received(1).WriteStateAsync();
    [Fact] void should_report_complete_seeding() => _result.ShouldEqual(SeedingResult.Complete);
}
