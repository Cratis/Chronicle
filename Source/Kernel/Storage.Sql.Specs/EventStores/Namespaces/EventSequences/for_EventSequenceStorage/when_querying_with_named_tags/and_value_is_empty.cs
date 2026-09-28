// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_value_is_empty : given.an_event_sequence_storage
{
    EventCount _empty;
    EventCount _other;
    AppendedEvent _stored;

    async Task Because()
    {
        var result = await _storage.AppendManyWithNamedTags([new EventToAppendToStorage(
            EventSequenceNumber.First, EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet)
        {
            NamedTags = [new NamedTag(new TagName("account"), "")]
        }]);
        result.IsSuccess.ShouldBeTrue();
        _stored = await _storage.GetEventAt(EventSequenceNumber.First);
        _empty = await _storage.GetCountMatching(new() { NamedTags = [new NamedTagCriterion(new TagName("account"), [""])] });
        _other = await _storage.GetCountMatching(new() { NamedTags = [new NamedTagCriterion(new TagName("account"), ["other"])] });
    }

    [Fact] void should_store_the_empty_value() => _stored.Context.NamedTags.Single().Value.ShouldEqual("");
    [Fact] void should_match_an_empty_value() => _empty.Value.ShouldEqual(1UL);
    [Fact] void should_not_match_other_values() => _other.Value.ShouldEqual(0UL);
}
