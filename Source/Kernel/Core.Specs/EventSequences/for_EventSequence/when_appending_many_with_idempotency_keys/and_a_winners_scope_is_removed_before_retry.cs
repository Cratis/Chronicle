// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_a_winners_scope_is_removed_before_retry : given.an_idempotent_batch
{
    int _attempts;
    void Establish()
    {
        CheckedScopes();
        _eventSequenceStorage.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>?>(), Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventStreamType?>())
            .Returns(call => Task.FromResult(_durable.Count > 0 && call.ArgAt<EventSourceId?>(1) == _events[0].EventSourceId ? (EventSequenceNumber)4 : EventSequenceNumber.Unavailable));
        _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
        {
            if (++_attempts == 1)
            {
                DurableAt(0, 4);
                return (Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>)(AppendManyStorageFailure)new IdentitiesAlreadyDurable(_durable);
            }
            _written = call.Arg<IEnumerable<EventToAppendToStorage>>().ToList();
            return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(AppendedEventsFrom(_written));
        });
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_validate_only_the_remaining_sources() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_checking_the_remainder() => _batchResult.Result.ConcurrencyCheckPerformed.ShouldBeTrue();
    [Fact] void should_append_the_remaining_events() => _written.Count.ShouldEqual(2);
}
