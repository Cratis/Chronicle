// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Auditing;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.for_ReplayScopedEventSequenceStorage;

public class when_appending_with_named_tags : given.a_replay_scoped_storage
{
    IReadOnlyCollection<NamedTag> _namedTags;

    void Establish() => _namedTags = [new NamedTag(new TagName("account"), "one")];

    Task Because() => _storage.Append(
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
        new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = new ExpandoObject() },
        new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = EventHash.NotSet },
        null,
        _namedTags);

    [Fact] void should_pass_the_named_tags_through_to_storage() =>
        _inner.Received(1).Append(
            Arg.Any<EventSequenceNumber>(),
            Arg.Any<EventSourceType>(),
            Arg.Any<EventSourceId>(),
            Arg.Any<EventStreamType>(),
            Arg.Any<EventStreamId>(),
            Arg.Any<EventType>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Causation>>(),
            Arg.Any<IEnumerable<IdentityId>>(),
            Arg.Any<IEnumerable<Tag>>(),
            Arg.Any<DateTimeOffset>(),
            Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(),
            Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(),
            Arg.Any<Subject?>(),
            _namedTags);
}
