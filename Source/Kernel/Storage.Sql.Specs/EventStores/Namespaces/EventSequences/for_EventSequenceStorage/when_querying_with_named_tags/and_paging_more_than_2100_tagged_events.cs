// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_paging_more_than_2100_tagged_events : given.an_event_sequence_storage
{
    AppendedEvent[] _events;

    async Task Because()
    {
        var result = await _storage.AppendManyWithNamedTags(Enumerable.Range(0, 2105).Select(index => new EventToAppendToStorage(
            new EventSequenceNumber((ulong)index), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            NamedTags = [new NamedTag(new TagName("account"), index.ToString(System.Globalization.CultureInfo.InvariantCulture))]
        }));
        result.IsSuccess.ShouldBeTrue();

        using var page = await _storage.GetPage(new() { NamedTags = [new NamedTagCriterion(new TagName("account"))] }, 0, 2105);
        await page.MoveNext();
        _events = page.Current.ToArray();
    }

    [Fact] void should_return_the_full_page() => _events.Length.ShouldEqual(2105);
    [Fact] void should_hydrate_the_last_tag() => _events[^1].Context.NamedTags.Single().Value.ShouldEqual("2104");
}
