// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_after_a_failed_constraint_index_update;

public class and_an_abandoned_attempt_is_repaired : given.an_event_sequence_with_repairable_indexes
{
    readonly TaskCompletionSource _write = new(TaskCreationOptions.RunContinuationsAsynchronously);
    bool _hang = true;

    async Task Establish()
    {
        _unique.Save(Arg.Any<EventSourceId>(), Arg.Any<UniqueConstraintDefinition>(), Arg.Any<EventSequenceNumber>(), Arg.Any<UniqueConstraintValue>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _hang ? _write.Task : _claims.Save(call.ArgAt<EventSourceId>(0), call.ArgAt<UniqueConstraintDefinition>(1), call.ArgAt<EventSequenceNumber>(2), call.ArgAt<UniqueConstraintValue>(3), call.ArgAt<string>(4), call.ArgAt<CancellationToken>(5)));
        await AppendAnEvent();
        _hang = false;
        _write.TrySetResult();
        _eventSourceId = EventSourceId.New();
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_clear_the_abandoned_constraint_after_a_full_later_fold() => _result.Errors.ShouldBeEmpty();
    [Fact] void should_validate_against_the_repaired_claim() => _result.ConstraintViolations.ShouldNotBeEmpty();
}
