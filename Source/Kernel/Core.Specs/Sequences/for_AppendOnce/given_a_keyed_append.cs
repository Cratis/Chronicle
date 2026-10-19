// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendOnce;

public class given_a_keyed_append : Sequences.given.an_append_endpoint
{
    protected IdempotencyKey _forwardedKey;
    protected EventSequences.EventToAppend _forwardedEvent;
    protected AppendOnceResult _result;

    protected void ReturnSettlement(IdempotentAppendResult settlement) =>
        _eventSequence.AppendWithIdempotencyKey(
            Arg.Do<EventSequences.EventToAppend>(_ => _forwardedEvent = _),
            Arg.Do<IdempotencyKey>(_ => _forwardedKey = _),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<Concepts.EventSequences.Concurrency.ConcurrencyScope>()).Returns(Task.FromResult(settlement));

    protected async Task<AppendOnceResult> Handle() => _result = await new AppendOnce(
        "store",
        "namespace",
        "event-log",
        "source",
        EventSourceType.Unspecified,
        new EventStreamType(string.Empty),
        new EventStreamId(string.Empty),
        new EventType("event", 1, false),
        "{}",
        "the-key").Handle(_grainFactory, _causation, _principal);
}
