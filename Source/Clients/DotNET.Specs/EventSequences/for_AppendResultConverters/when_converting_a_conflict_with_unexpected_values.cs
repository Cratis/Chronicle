// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_AppendResultConverters;

public class when_converting_a_conflict_with_unexpected_values : Specification
{
    IdempotencyKeyConflict _conflict;

    void Because() => _conflict = new Contracts.Sequences.IdempotencyKeyConflict
    {
        Key = string.Empty,
        StoredEventTypeId = "created",
        Reason = (Contracts.EventSequences.IdempotencyConflictReason)99
    }.ToClient();

    [Fact] void should_not_throw_and_keep_the_key_as_received() => _conflict.Key.Value.ShouldEqual(string.Empty);
    [Fact] void should_treat_the_unknown_reason_as_unverifiable() => _conflict.Reason.ShouldEqual(IdempotencyConflictReason.Unverifiable);
}
