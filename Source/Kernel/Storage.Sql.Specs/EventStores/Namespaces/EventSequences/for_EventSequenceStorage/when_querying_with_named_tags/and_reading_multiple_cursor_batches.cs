// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_reading_multiple_cursor_batches : given.an_event_sequence_storage
{
    AppendedEvent[] _events;
    int _batches;

    async Task Because()
    {
        var result = await _storage.AppendManyWithNamedTags(Enumerable.Range(0, 205).Select(index => new EventToAppendToStorage(
            new EventSequenceNumber((ulong)index), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            NamedTags = [new NamedTag(new TagName("account"), index.ToString(System.Globalization.CultureInfo.InvariantCulture))]
        }));
        result.IsSuccess.ShouldBeTrue();

        var events = new List<AppendedEvent>();
        using var cursor = await _storage.GetRange(EventSequenceNumber.First, EventSequenceNumber.Max);
        while (await cursor.MoveNext())
        {
            _batches++;
            events.AddRange(cursor.Current);
        }
        _events = events.ToArray();
    }

    [Fact] void should_hydrate_every_event() => _events.Length.ShouldEqual(205);
    [Fact] void should_fetch_multiple_batches() => _batches.ShouldEqual(3);
    [Fact] void should_preserve_tag_values_across_batch_boundaries() => _events.Select((@event, index) => @event.Context.NamedTags.Single().Value == index.ToString(System.Globalization.CultureInfo.InvariantCulture)).All(matches => matches).ShouldBeTrue();
}
