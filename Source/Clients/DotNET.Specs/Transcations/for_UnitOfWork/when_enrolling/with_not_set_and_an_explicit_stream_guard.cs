// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enrolling;

public class with_not_set_and_an_explicit_stream_guard : given.a_unit_of_work
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => _unitOfWork.AddEvents(
        EventSequenceId.Log,
        [new("source", new object())],
        [new("source", ConcurrencyScope.NotSet), new("source", new ConcurrencyScope(42, "source", "stream", "first"))]));

    [Fact] void should_refuse_the_ambiguous_enrollment() => _error.ShouldBeOfExactType<DuplicateConcurrencyScopeForEventSourceId>();
}
