// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_the_batch_fails_after_staging_named_tags : given.an_event_sequence_storage
{
    bool _failed;
    int _eventCount;
    int _tagCount;

    async Task Because()
    {
        SeedEvent(new EventSequenceNumber(2));
        var result = await _storage.AppendManyWithNamedTags(
        [
            Event(1, [new NamedTag(new TagName("account"), "one")]),
            Event(2, [new NamedTag(new TagName("account"), "two")])
        ]);
        _failed = !result.IsSuccess;

        await using var context = CreateContext();
        _eventCount = await context.Events.CountAsync();
        _tagCount = await context.NamedTags.CountAsync(tag => tag.EventSequenceId == _tableName);
    }

    static EventToAppendToStorage Event(ulong number, IReadOnlyCollection<NamedTag> tags) =>
        new(new EventSequenceNumber(number), EventSourceType.Default, "source", EventStreamType.All, EventStreamId.Default, _eventType, CorrelationId.New(), [], [], [], DateTimeOffset.UtcNow, new ExpandoObject(), EventHash.NotSet) { NamedTags = tags };

    [Fact] void should_reject_the_batch() => _failed.ShouldBeTrue();
    [Fact] void should_not_persist_the_staged_event() => _eventCount.ShouldEqual(1);
    [Fact] void should_not_persist_the_staged_tags() => _tagCount.ShouldEqual(0);
}
