// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_another_activation_wins_the_race : given.an_idempotent_batch
{
    int _attempts;
    void Establish() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(call =>
    {
        if (++_attempts == 1)
        {
            DurableAt(1, 3);
            return (Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>)(AppendManyStorageFailure)new IdentitiesAlreadyDurable(_durable);
        }
        _written = call.Arg<IEnumerable<EventToAppendToStorage>>().ToList();
        return Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>.Success(AppendedEventsFrom(_written));
    });
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_preserve_the_removed_middle_request() => _batchResult.Result.SequenceNumbers.ShouldEqual([4, 3, 5]);
    [Fact] void should_merge_settled_and_new_outcomes() => _batchResult.Outcomes.ShouldEqual([AppendOutcome.Committed, AppendOutcome.AlreadyAppended, AppendOutcome.Committed]);
    [Fact] void should_retry_only_the_absent_remainder() => _written.Count.ShouldEqual(2);
    [Fact] void should_attempt_storage_twice() => _attempts.ShouldEqual(2);
}
