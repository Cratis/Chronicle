// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_a_disposed_borrowed_lease : for_UnitOfWork.given.a_unit_of_work
{
    Exception _error;

    void Because()
    {
        // The runner isolates Establish and Because execution contexts; model one invocation here.
        using var frame = _unitOfWork.EnforceReadProtection();
        _unitOfWork.EnforceReadProtection().Dispose();
        var other = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        _error = Catch.Exception(() => other.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown()));
    }

    [Fact] void should_not_end_the_invocations_unit_authority() => _error.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
}
