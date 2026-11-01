// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.for_IEventSequenceStorage;

public abstract class when_reading_a_window<THarness> : Specification
    where THarness : IEventSequenceWindowHarness, new()
{
    static readonly EventType _selectedType = new("selected", EventTypeGeneration.First);
    static readonly EventType _otherType = new("other", EventTypeGeneration.First);
    readonly Dictionary<string, string> _pages = [];
    THarness _harness;
    IEventSequenceStorage _storage;
    Exception? _zeroLimit;
    Exception? _negativeLimit;

    async Task Establish()
    {
        _harness = CreateHarness();
        _storage = await _harness.CreateStorage();

        // Sparse, deliberately out of insertion order; each filter removes interleaved events.
        foreach (var number in (ulong[])[40, 0, 20, 2, 8, 10, 4])
        {
            var result = await _storage.AppendMany([new EventToAppendToStorage(
                number,
                number == 4 ? (EventSourceType)"other-source-type" : "selected-source-type",
                number == 2 ? (EventSourceId)"other-source" : "selected-source",
                number == 8 ? (EventStreamType)"other-stream-type" : "selected-stream-type",
                number == 10 ? (EventStreamId)"other-stream" : "selected-stream",
                number == 20 ? _otherType : _selectedType,
                CorrelationId.NotSet,
                [],
                [],
                number == 40 ? [(Tag)"other-tag"] : [(Tag)"selected-tag"],
                DateTimeOffset.UtcNow,
                new ExpandoObject(),
                EventHash.NotSet)]);
            result.IsSuccess.ShouldBeTrue();
        }
    }

    async Task Because()
    {
        await Read("ascending", 0, EventSequenceNumber.Max, 3, false);
        await Read("descending", 0, EventSequenceNumber.Max, 3, true);
        await Read("inclusive", 2, 10, 10, false);
        await Read("inclusive-backward", 2, 10, 10, true);
        await Read("reversed", 10, 2, 3, false);
        await Read("reversed-backward", 10, 2, 3, true);
        await Read("beyond-tail", 41, EventSequenceNumber.Max, 3, false);
        await Read("no-match", 3, 3, 3, true);
        await Read("source", 0, EventSequenceNumber.Max, 3, false, eventSourceId: "selected-source");
        await Read("source-type", 0, EventSequenceNumber.Max, 3, false, eventSourceType: "selected-source-type");
        await Read("stream-type", 0, EventSequenceNumber.Max, 5, false, eventStreamType: "selected-stream-type");
        await Read("stream", 0, EventSequenceNumber.Max, 6, false, eventStreamId: "selected-stream");
        await Read("event-type", 0, EventSequenceNumber.Max, 6, true, eventTypes: [_selectedType]);
        await Read("tags", 0, EventSequenceNumber.Max, 3, true, tags: ["selected-tag", "absent"]);
        await Read("all-filters", 0, EventSequenceNumber.Max, 2, false, "selected-source", "selected-source-type", "selected-stream-type", "selected-stream", [_selectedType], ["selected-tag"]);
        await Read("sentinels", 0, EventSequenceNumber.Max, 3, false, EventSourceId.Unspecified, EventSourceType.Default, EventStreamType.All, EventStreamId.Default, [], []);
        _zeroLimit = await Catch.Exception(async () => await _storage.GetWindow(0, 40, 0, false));
        _negativeLimit = await Catch.Exception(async () => await _storage.GetWindow(0, 40, -1, false));
    }

    void Destroy() => _harness?.Dispose();

    [Theory]
    [InlineData("ascending", "0,2,4")]
    [InlineData("descending", "40,20,10")]
    [InlineData("inclusive", "2,4,8,10")]
    [InlineData("inclusive-backward", "10,8,4,2")]
    [InlineData("reversed", "")]
    [InlineData("reversed-backward", "")]
    [InlineData("beyond-tail", "")]
    [InlineData("no-match", "")]
    [InlineData("source", "0,4,8")]
    [InlineData("source-type", "0,2,8")]
    [InlineData("stream-type", "0,2,4,10,20")]
    [InlineData("stream", "0,2,4,8,20,40")]
    [InlineData("event-type", "40,10,8,4,2,0")]
    [InlineData("tags", "20,10,8")]
    [InlineData("all-filters", "0")]
    [InlineData("sentinels", "0,2,4")]
    public void should_return_only_matching_events_in_bounded_read_order(string scenario, string expected) => _pages[scenario].ShouldEqual(expected);

    [Fact] public void should_reject_a_zero_limit() => _zeroLimit.ShouldBeOfExactType<InvalidEventSequenceWindowLimit>();
    [Fact] public void should_reject_a_negative_limit() => _negativeLimit.ShouldBeOfExactType<InvalidEventSequenceWindowLimit>();

    /// <summary>
    /// Creates the backend harness, overridable for infrastructure fixtures.
    /// </summary>
    /// <returns>The harness to run the contract against.</returns>
    protected virtual THarness CreateHarness() => new();

    async Task Read(
        string scenario,
        EventSequenceNumber from,
        EventSequenceNumber until,
        int limit,
        bool descending,
        EventSourceId? eventSourceId = null,
        EventSourceType? eventSourceType = null,
        EventStreamType? eventStreamType = null,
        EventStreamId? eventStreamId = null,
        IEnumerable<EventType>? eventTypes = null,
        IEnumerable<Tag>? tags = null)
    {
        using var cursor = await _storage.GetWindow(from, until, limit, descending, eventSourceId, eventSourceType, eventStreamType, eventStreamId, eventTypes, tags);
        var numbers = new List<ulong>();
        while (await cursor.MoveNext())
        {
            numbers.AddRange(cursor.Current.Select(@event => @event.Context.SequenceNumber.Value));
        }

        _pages.Add(scenario, string.Join(',', numbers));
    }
}
