// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendOnceWithNamedTags;

public class when_handling : Sequences.given.an_append_endpoint
{
    static readonly DateTimeOffset _occurred = new(2024, 5, 6, 7, 8, 9, TimeSpan.Zero);
    IdempotencyKey _forwardedKey;
    EventSequences.EventToAppend _forwardedEvent;

    void Establish() =>
        _eventSequence.AppendWithIdempotencyKey(
            Arg.Do<EventSequences.EventToAppend>(_ => _forwardedEvent = _),
            Arg.Do<IdempotencyKey>(_ => _forwardedKey = _),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>())
            .Returns(Task.FromResult(new IdempotentAppendResult(AppendResult.Success(Guid.NewGuid(), new EventSequenceNumber(1UL)), AppendOutcome.Committed)));

    async Task Because() => await new AppendOnceWithNamedTags(
        "store",
        "namespace",
        "event-log",
        "source",
        EventSourceType.Unspecified,
        new EventStreamType(string.Empty),
        new EventStreamId(string.Empty),
        new EventType("event", 1, false),
        "{}",
        "the-key",
        [new NamedTag("account", "one")],
        Occurred: _occurred).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_forward_the_key() => _forwardedKey.Value.ShouldEqual("the-key");
    [Fact] void should_forward_the_named_tag() => _forwardedEvent.NamedTags.Single().Value.ShouldEqual("one");
    [Fact] void should_forward_the_occurred_time() => _forwardedEvent.Occurred.ShouldEqual(_occurred);
}
