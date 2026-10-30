// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Monads;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventSequenceStorage;

public class when_reading_last_instances_in_source_chunks : given.an_event_sequence_storage
{
    EventSourceId[] _sources;
    IReadOnlyDictionary<EventSourceId, Option<AppendedEvent>> _results;
    readonly List<FilterDefinition<Event>> _filters = [];
    readonly List<FindOptions<Event, Event>> _options = [];

    void Establish()
    {
        _sources = Enumerable.Range(0, 1001).Select(index => new EventSourceId($"source-{index}")).ToArray();
        var firstChunk = Substitute.For<IAsyncCursor<Event>>();
        firstChunk.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, true, false);
        firstChunk.Current.Returns(
            [AnEvent(4000, _sources[0], "first-parent"), AnEvent(3000, _sources[999], "first-parent")],
            [AnEvent(5000, _sources[0], "parent"), AnEvent(1, _sources[0], "parent"), AnEvent(2000, _sources[999], "parent")]);
        var secondChunk = Substitute.For<IAsyncCursor<Event>>();
        secondChunk.MoveNextAsync(Arg.Any<CancellationToken>()).Returns(true, false);
        secondChunk.Current.Returns([AnEvent(6000, _sources[1000], "parent")]);
        var cursors = new Queue<IAsyncCursor<Event>>([firstChunk, secondChunk]);
        _collection.FindAsync(Arg.Any<FilterDefinition<Event>>(), Arg.Any<FindOptions<Event, Event>>(), Arg.Any<CancellationToken>())
            .Returns(info =>
            {
                _filters.Add(info.ArgAt<FilterDefinition<Event>>(0));
                _options.Add(info.ArgAt<FindOptions<Event, Event>>(1));
                return cursors.Dequeue();
            });
        _converter.ToAppendedEvent(Arg.Any<Event>()).Returns(info => Task.FromResult(AppendedEvent.EmptyWithEventSequenceNumber(info.Arg<Event>().SequenceNumber)));
    }

    async Task Because() => _results = await _storage.GetLastInstancesOfAny(_sources.Concat([new("missing"), _sources[0]]), [new("first-parent"), new("parent")]);

    [Fact] void should_issue_one_find_per_distinct_source_chunk() => _options.Count.ShouldEqual(2);
    [Fact] void should_not_force_an_index() => _options.ShouldEachConformTo(options => options.Hint == null);
    [Fact] void should_request_large_cursor_batches() => _options.ShouldEachConformTo(options => options.BatchSize == 1000);
    [Fact] void should_not_sort_on_the_server() => _options.ShouldEachConformTo(options => options.Sort == null);
    [Fact] void should_bound_the_first_chunk() => RenderFilter(0)["eventSourceId"]["$in"].AsBsonArray.Count.ShouldEqual(1000);
    [Fact] void should_put_remaining_sources_in_the_second_chunk() => RenderFilter(1)["eventSourceId"]["$in"].AsBsonArray.ShouldEqual(new BsonArray { "source-1000", "missing" });
    [Fact] void should_filter_to_the_requested_types() => RenderFilter(0)["type"]["$in"].AsBsonArray.ShouldEqual(new BsonArray { "first-parent", "parent" });
    [Fact] void should_return_each_distinct_source() => _results.Count.ShouldEqual(1002);
    [Fact] void should_keep_missing_sources() => _results[new EventSourceId("missing")].TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_keep_sources_with_no_matching_events() => _results[_sources[1]].TryGetValue(out _).ShouldBeFalse();
    [Fact] void should_choose_the_latest_across_types_and_cursor_batches() => SequenceFor(_sources[0]).ShouldEqual((EventSequenceNumber)5000UL);
    [Fact] void should_not_replace_a_later_event_with_an_earlier_one_from_another_type() => SequenceFor(_sources[999]).ShouldEqual((EventSequenceNumber)3000UL);
    [Fact] void should_include_the_latest_event_beyond_the_chunk_boundary() => SequenceFor(_sources[1000]).ShouldEqual((EventSequenceNumber)6000UL);
    [Fact] void should_only_convert_the_winning_events() => _converter.Received(3).ToAppendedEvent(Arg.Any<Event>());

    static RenderArgs<Event> RenderArguments() => new(BsonSerializer.LookupSerializer<Event>(), BsonSerializer.SerializerRegistry);

    static Event AnEvent(ulong number, EventSourceId source, EventTypeId type) => new(number, CorrelationId.New(), [], [], type, DateTimeOffset.UtcNow, EventSourceType.Default, source, EventStreamType.All, EventStreamId.Default, [], new Dictionary<string, BsonDocument>(), new Dictionary<string, string>(), []);

    BsonDocument RenderFilter(int index) => _filters[index].Render(RenderArguments());

    EventSequenceNumber SequenceFor(EventSourceId source)
    {
        _results[source].TryGetValue(out var last).ShouldBeTrue();
        return last.Context.SequenceNumber;
    }
}
