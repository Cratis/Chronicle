// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;
using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_using_a_reserved_name;

public class and_the_sequence_is_new_when_using_other_mutators : given.an_event_sequence_with_a_reserved_name
{
    readonly List<Exception> _errors = [];
    readonly CorrelationId _correlationId = CorrelationId.New();

    async Task Because()
    {
        Func<Task>[] writes =
        [
            () => _eventSequence.Append("source", new object()),
            () => _eventSequence.AppendMany(null!, _correlationId, [], Identity.System, null!),
            () => _eventSequence.AppendManyWithConjunctions(null!, _correlationId, [], Identity.System, null!),
            () => _eventSequence.AppendManyWithEffectReceipt(null!, _correlationId, [], Identity.System, null!, null!),
            () => _eventSequence.AppendManyWithConjunctionsAndEffectReceipt(null!, _correlationId, [], Identity.System, null!, null!),
            () => _eventSequence.AppendPublication(null!, null!, null!, _correlationId, [], Identity.System),
            () => _eventSequence.AppendWithIdempotencyKey(null!, null!, _correlationId, [], Identity.System, ConcurrencyScope.None),
            () => _eventSequence.AppendManyWithIdempotencyKeys(null!, null!, _correlationId, [], Identity.System, null!),
            () => _eventSequence.RecordEffectReceipt(null!, EffectOutcome.NoEvents, []),
            () => _eventSequence.Revise(0, null!, null!, _correlationId, [], Identity.System),
            () => _eventSequence.Redact(0, "reason", _correlationId, [], Identity.System),
            () => _eventSequence.Redact("source", "reason", [], _correlationId, [], Identity.System),
            () => _eventSequence.ReopenCompletedStream(new ClosedStreamScope(EventSourceId: "source"), "reason", _correlationId, [], Identity.System),
        ];
        foreach (var write in writes) _errors.Add(await Catch.Exception(write));
    }

    [Fact] void should_refuse_every_write() => _errors.Count.ShouldEqual(13);
    [Fact] void should_refuse_before_accessing_arguments() => _errors.TrueForAll(error => error is ReservedEventSequenceName).ShouldBeTrue();
    [Fact] void should_not_open_sequence_storage() => _namespaceStorage.DidNotReceive().GetEventSequence(SequenceId);
    [Fact] void should_not_write_state() => StateWrites.ShouldEqual(0);
}
