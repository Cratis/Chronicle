// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_an_event;

public class and_named_tags_are_sent_to_storage : given.an_event_sequence
{
    AppendResult _result;
    IReadOnlyCollection<NamedTag> _persistedTags;

    void Establish() => _eventSequenceStorage.Append(
        Arg.Any<EventSequenceNumber>(),
        Arg.Any<EventSourceType>(),
        Arg.Any<EventSourceId>(),
        Arg.Any<EventStreamType>(),
        Arg.Any<EventStreamId>(),
        Arg.Any<EventType>(),
        Arg.Any<CorrelationId>(),
        Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
        Arg.Any<IEnumerable<IdentityId>>(),
        Arg.Any<IEnumerable<Tag>>(),
        Arg.Any<DateTimeOffset>(),
        Arg.Any<IDictionary<EventTypeGeneration, ExpandoObject>>(),
        Arg.Any<IDictionary<EventTypeGeneration, EventHash>>(),
        Arg.Any<Subject?>(),
        Arg.Any<IReadOnlyCollection<NamedTag>>())
        .Returns(call =>
        {
            _persistedTags = call.ArgAt<IReadOnlyCollection<NamedTag>>(14);
            var context = EventContext.From(
                EventStore,
                EventStoreNamespace,
                _eventType,
                EventSourceType.Default,
                _eventSourceId,
                EventStreamType.All,
                EventStreamId.Default,
                EventSequenceNumber.First,
                CorrelationId.NotSet) with { NamedTags = _persistedTags };
            return Task.FromResult<Result<AppendedEvent, DuplicateEventSequenceNumber>>(new AppendedEvent(context, new ExpandoObject()));
        });

    async Task Because() => _result = await _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None,
        null,
        null,
        [new NamedTag(new TagName("account"), "one")]);

    [Fact] void should_append_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_named_tag_at_storage() => _persistedTags.Single().Value.ShouldEqual("one");
}
