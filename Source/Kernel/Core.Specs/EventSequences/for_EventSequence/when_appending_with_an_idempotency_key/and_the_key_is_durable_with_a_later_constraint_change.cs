// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using NSubstitute.ExceptionExtensions;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_an_idempotency_key;

public class and_the_key_is_durable_with_a_later_constraint_change : given.an_event_sequence_with_idempotency_keys
{
    void Establish()
    {
        Durable();
        _constraintsGrain.GetVersion().Throws(new Exception("Must not reload later constraints on a hit"));
    }
    async Task Because() => _result = await AppendKeyed();
    [Fact] void should_settle_without_reloading_constraints() => _result.Outcome.ShouldEqual(AppendOutcome.AlreadyAppended);
    [Fact] void should_not_update_constraint_indexes_from_retry_content() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
    [Fact] async Task should_not_write() => await ShouldNotWrite();
}
