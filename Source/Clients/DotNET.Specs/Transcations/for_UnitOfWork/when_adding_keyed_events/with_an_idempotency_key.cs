// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_adding_keyed_events;

public class with_an_idempotency_key : given.a_unit_recording_read_protection
{
    Exception _error;

    void Because() => _error = Record.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new("source", new object(), Causation.Unknown()) { IdempotencyKey = new("k") }],
        []));

    [Fact] void should_refuse_the_key() => _error.ShouldBeOfExactType<IdempotentAppendsNotSupported>();
}
