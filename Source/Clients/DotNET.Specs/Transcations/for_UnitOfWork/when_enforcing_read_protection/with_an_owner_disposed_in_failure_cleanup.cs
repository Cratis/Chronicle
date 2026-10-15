// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_enforcing_read_protection;

public class with_an_owner_disposed_in_failure_cleanup : for_UnitOfWork.given.a_unit_of_work
{
    Exception _error;
    Exception _beforeCleanup;

    void Because()
    {
        var frame = _unitOfWork.EnforceReadProtection();
        var other = new UnitOfWork(CorrelationId.New(), _ => { }, _eventStore);
        _beforeCleanup = Catch.Exception(() => other.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown()));
        try
        {
            throw new OperationCanceledException();
        }
        catch (OperationCanceledException)
        {
            // Invocation cancellation is cleaned up by the pipeline's finally, not by unit completion.
        }
        finally
        {
            frame.Dispose();
        }
        _error = Catch.Exception(() => other.AddEvent(EventSequenceId.Log, "source", new object(), Causation.Unknown()));
    }

    [Fact] void should_protect_the_invocation_before_cleanup() => _beforeCleanup.ShouldBeOfExactType<ReadProtectionUnitMismatch>();
    [Fact] void should_end_the_invocation_enforcement_on_cancellation() => _error.ShouldBeNull();
    [Fact] void should_keep_the_units_strict_enforce_policy() => _unitOfWork.IsEnforcingReadProtection.ShouldBeTrue();
}
