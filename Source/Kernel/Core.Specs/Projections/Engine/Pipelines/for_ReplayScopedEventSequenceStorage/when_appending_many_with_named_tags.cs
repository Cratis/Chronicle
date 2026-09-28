// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_appending_many_with_named_tags : given.a_replay_scoped_storage
{
    EventToAppendToStorage[] _events;

    void Establish() => _events =
    [
        new EventToAppendToStorage(
            EventSequenceNumber.First,
            EventSourceType.Default,
            _eventSourceId,
            EventStreamType.All,
            EventStreamId.Default,
            _eventTypes[0],
            CorrelationId.New(),
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new ExpandoObject(),
            EventHash.NotSet)
        {
            NamedTags = [new NamedTag(new TagName("account"), "one")]
        }
    ];

    Task Because() => _storage.AppendManyWithNamedTags(_events);

    [Fact] void should_pass_the_batch_through_to_storage() => _inner.Received(1).AppendManyWithNamedTags(_events);
}
