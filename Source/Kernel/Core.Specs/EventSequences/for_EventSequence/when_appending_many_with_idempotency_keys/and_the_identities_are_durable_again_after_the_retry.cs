// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many_with_idempotency_keys;

public class and_the_identities_are_durable_again_after_the_retry : given.an_idempotent_batch
{
    int _attempts;
    void Establish() => _publications.AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>()).Returns(_ =>
    {
        DurableAt(++_attempts == 1 ? 0 : 1, _attempts == 1 ? 2UL : 3UL);
        return (Result<IEnumerable<AppendedEvent>, AppendManyStorageFailure>)(AppendManyStorageFailure)new IdentitiesAlreadyDurable(_durable);
    });
    async Task Because() => _batchResult = await AppendBatch();
    [Fact] void should_refuse_the_still_absent_key() => _batchResult.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_only_the_unsettled_key() => _batchResult.Result.Errors.Single().Value.ShouldContain("Idempotency key 'three' could not be settled");
    [Fact] void should_never_attempt_a_third_write() => _attempts.ShouldEqual(2);
}
