// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_keyed_events;

public class with_an_idempotency_key_in_a_batch : given.a_unit_recording_read_protection
{
    void Establish() => Record.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new("source", new object(), Causation.Unknown()), new("source", new object(), Causation.Unknown()) { IdempotencyKey = new("k") }],
        []));

    Task Because() => _unitOfWork.Commit();

    [Fact] void should_stage_none_of_the_batch() => (_eventsAppended ?? []).ShouldBeEmpty();
}
