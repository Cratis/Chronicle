// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnceWithNamedTags;

public class when_handling : Sequences.given.an_append_endpoint
{
    EventSequences.EventToAppend[] _forwardedEvents;

    void Establish() =>
        _eventSequence.AppendManyWithIdempotencyKeys(
            Arg.Do<IEnumerable<EventSequences.EventToAppend>>(_ => _forwardedEvents = [.. _]),
            Arg.Any<IReadOnlyList<IdempotencyKey?>>(),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<ConcurrencyScopes>()).Returns(Task.FromResult(new IdempotentAppendManyResult(new AppendManyResult())));

    async Task Because() => await new AppendManyOnceWithNamedTags(
        "store",
        "namespace",
        "event-log",
        [
            new(new EventForEventSourceIdWithNamedTags("source", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "one")]), "first"),
            new(new EventForEventSourceIdWithNamedTags("source", "", "", "", new EventType("event", 1, false), "{}", null, [new NamedTag("account", "two")]), "second")
        ]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_forward_both_events() => _forwardedEvents.Length.ShouldEqual(2);
    [Fact] void should_keep_first_events_named_tag() => _forwardedEvents[0].NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_keep_second_events_named_tag() => _forwardedEvents[1].NamedTags.Single().Value.ShouldEqual("two");
}
