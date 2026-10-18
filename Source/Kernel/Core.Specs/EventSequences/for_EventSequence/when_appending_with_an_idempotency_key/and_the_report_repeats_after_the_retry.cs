// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_report_repeats_after_the_retry : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => RefuseWrites();
    async Task Because() => _result = await AppendKeyed();
    [Fact] async Task should_attempt_storage_exactly_twice() => await _publications.Received(2).AppendManyWithIdentities(Arg.Any<IEnumerable<EventToAppendToStorage>>());
    [Fact] void should_settle_after_both_non_commits() => _lookups.ShouldEqual(3);
    [Fact] void should_return_the_exact_unsettled_sentence() => _result.Result.Errors.Single().Value.ShouldEqual("IdempotencyKeyUnsettled: Idempotency key 'one' could not be settled because another append with the same key was still in progress, or the key protecting its values changed during the append. Nothing was written. Retry the append.");
}
