// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_an_event_has_named_tags : given.an_event_sequence_storage
{
    Exception _exception;
    EventCount _count;

    async Task Because()
    {
        try
        {
            await _storage.AppendMany([new EventToAppendToStorage(
                EventSequenceNumber.First,
                EventSourceType.Default,
                "source",
                EventStreamType.All,
                EventStreamId.Default,
                _eventType,
                CorrelationId.New(),
                [],
                [],
                [],
                DateTimeOffset.UtcNow,
                new ExpandoObject(),
                EventHash.NotSet)
            {
                NamedTags = [new NamedTag(new TagName("account"), "one")]
            }]);
        }
        catch (Exception exception)
        {
            _exception = exception;
        }

        _count = await _storage.GetCount();
    }

    [Fact] void should_reject_the_batch_before_writing() => _exception.ShouldBeOfExactType<NamedTagsNotSupported>();
    [Fact] void should_leave_storage_empty() => _count.Value.ShouldEqual(0UL);
}
