// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Sequences.for_AppendManyOnce;

public class and_a_mixed_keyed_batch_settles : Sequences.given.an_append_endpoint
{
    IReadOnlyList<IdempotencyKey?> _forwardedKeys;
    EventSequences.EventToAppend[] _forwardedEvents;
    AppendManyOnceResult _result;

    void Establish() =>
        _eventSequence.AppendManyWithIdempotencyKeys(
            Arg.Do<IEnumerable<EventSequences.EventToAppend>>(_ => _forwardedEvents = [.. _]),
            Arg.Do<IReadOnlyList<IdempotencyKey?>>(_ => _forwardedKeys = _),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<ConcurrencyScopes>()).Returns(Task.FromResult(new IdempotentAppendManyResult(
                AppendManyResult.Success(Guid.NewGuid(), [new EventSequenceNumber(1UL), new EventSequenceNumber(9UL)]))
            {
                Outcomes = [AppendOutcome.AlreadyAppended, AppendOutcome.Committed],
                Conflicts = [new IdempotencyConflict(new IdempotencyKey("other"), new EventSequenceNumber(4UL), new Concepts.Events.EventType("stored", 1, false), new EventTypeGeneration(1), IdempotencyConflictReason.Content)]
            }));

    async Task Because() => _result = await new AppendManyOnce(
        "store",
        "namespace",
        "event-log",
        [
            new(new EventForEventSourceId("source-one", "", "", "", new EventType("event", 1, false), "{}"), "first-key"),
            new(new EventForEventSourceId("source-two", "", "", "", new EventType("event", 1, false), "{}"))
        ]).Handle(_grainFactory, _causation, _principal);

    [Fact] void should_forward_one_event_per_request_event() => _forwardedEvents.Length.ShouldEqual(2);
    [Fact] void should_keep_each_events_own_source() => _forwardedEvents[1].EventSourceId.Value.ShouldEqual("source-two");
    [Fact] void should_forward_keys_aligned_with_events() => _forwardedKeys.Count.ShouldEqual(2);
    [Fact] void should_forward_the_key_of_the_keyed_event() => _forwardedKeys[0]!.Value.ShouldEqual("first-key");
    [Fact] void should_forward_no_key_for_the_unkeyed_event() => _forwardedKeys[1].ShouldBeNull();
    [Fact] void should_report_outcomes_in_request_order() => _result.Outcomes.ShouldEqual([AppendOutcome.AlreadyAppended, AppendOutcome.Committed]);
    [Fact] void should_report_sequence_numbers_in_request_order() => _result.SequenceNumbers.Select(_ => _.Value).ShouldEqual([1UL, 9UL]);
    [Fact] void should_report_the_conflicts() => _result.IdempotencyConflicts.Single().Reason.ShouldEqual(IdempotencyConflictReason.Content);
}
