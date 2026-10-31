// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_an_unkeyed_commit_cannot_be_confirmed : given.an_idempotent_batch
{
    void Establish()
    {
        _batchKeys[2] = null;
        _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(_ =>
        {
            DurableAt(0, 2);
            DurableAt(1, 3);
            return Task.FromException<Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>>(new Exception("unknown commit result"));
        });
    }
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_not_claim_the_unkeyed_event_committed() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_unknown_outcome() => _batchResult.Result.Errors.ShouldContainOnly(AppendError.AppendOutcomeUnknown);
    [Fact] async Task should_not_retry_unknown_storage() => await _publications.Received(1).AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
