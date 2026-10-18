// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_a_constraint_is_pending_and_the_key_is_new : given.an_event_sequence_with_idempotency_keys
{
    void Establish() => PendingConstraint();
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_refuse_until_the_index_is_available() => _result.Result.Errors.ShouldContain(AppendError.ConstraintIndexUnavailable);
    [Fact] void should_settle_again_before_refusing() => _lookups.ShouldEqual(2);
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
