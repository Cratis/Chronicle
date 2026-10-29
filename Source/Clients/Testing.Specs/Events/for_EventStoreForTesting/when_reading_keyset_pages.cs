// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

namespace Cratis.Chronicle.Testing.Events.for_EventStoreForTesting;

public class when_reading_keyset_pages : Specification
{
    EventStoreForTesting _store;
    IEventSequence _sequence;
    readonly List<ulong> _forward = [];
    readonly List<ulong> _backward = [];
    EventSequencePage _latest;
    EventSequencePage _log;
    EventSequencePage _empty;

    async Task Establish()
    {
        _store = new();
        _sequence = _store.GetEventSequence("history");
        for (var index = 0; index < 9; index++)
        {
            var result = await _sequence.Append(index % 2 == 0 ? "selected-source" : "other-source", new MigrationTopicCreated($"event-{index}"), tags: [index % 2 == 0 ? "selected" : "other"]);
            result.IsSuccess.ShouldBeTrue();
        }
        var logResult = await _store.EventLog.Append("log-source", new MigrationTopicCreated("log"));
        logResult.IsSuccess.ShouldBeTrue();
    }

    async Task Because()
    {
        var filter = new EventSequenceFilter(EventSourceId: "selected-source", Tags: ["selected"]);
        _forward.AddRange(await ReadAll(EventSequencePageRequest.Forward(null, 2, filter) with { Until = 6 }));
        _backward.AddRange(await ReadAll(EventSequencePageRequest.Backward(null, 2, filter) with { Until = 2 }));

        _latest = await _sequence.GetLatest(2, filter);
        _empty = await _sequence.GetPage(EventSequencePageRequest.Forward(null, 2, new(EventSourceId: "absent")));
        _log = await _store.EventLog.GetLatest(2);
    }

    void Destroy() => _store.Dispose();

    async Task<IEnumerable<ulong>> ReadAll(EventSequencePageRequest request)
    {
        var numbers = new List<ulong>();
        for (var pageIndex = 0; pageIndex < 10; pageIndex++)
        {
            var page = await _sequence.GetPage(request);
            numbers.AddRange(page.Events.Select(@event => @event.Context.SequenceNumber.Value));
            if (!page.HasMore) return numbers;
            request = request with { From = page.Next };
        }

        throw new InvalidOperationException("The continuation did not reach the end within ten pages.");
    }

    [Fact] void should_read_filtered_forward_pages_without_duplicates_or_missing_events() => _forward.ShouldEqual([0UL, 2UL, 4UL, 6UL]);
    [Fact] void should_read_filtered_backward_pages_without_duplicates_or_missing_events() => _backward.ShouldEqual([8UL, 6UL, 4UL, 2UL]);
    [Fact] void should_return_the_latest_matching_events_newest_first() => _latest.Events.Select(@event => @event.Context.SequenceNumber.Value).ShouldEqual([8UL, 6UL]);
    [Fact] void should_release_and_deserialize_the_content() => _latest.Events.Select(@event => ((MigrationTopicCreated)@event.Content).Module).ShouldEqual(["event-8", "event-6"]);
    [Fact] void should_return_a_real_inclusive_continuation() => _latest.Next.ShouldEqual((EventSequenceNumber)5UL);
    [Fact] void should_return_an_empty_page_for_a_nonmatching_filter() => _empty.Events.ShouldBeEmpty();
    [Fact] void should_not_continue_an_empty_page() => _empty.HasMore.ShouldBeFalse();
    [Fact] void should_forward_event_log_pages_without_including_named_sequence_events() => _log.Events.Count.ShouldEqual(1);
    [Fact] void should_preserve_the_end_of_the_event_log() => _log.Next.ShouldBeNull();
}
