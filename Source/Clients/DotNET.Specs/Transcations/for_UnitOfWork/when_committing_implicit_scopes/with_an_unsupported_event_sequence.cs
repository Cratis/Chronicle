// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing_implicit_scopes;

public class with_an_unsupported_event_sequence : given.a_unit_of_work
{
    Exception _exception;
    void Establish()
    {
        ((IUnitOfWork)_unitOfWork).AddImplicitScope(EventSequenceId.Log, "source", 7);
        _unitOfWork.AddEvent(EventSequenceId.Log, "source", "event", Causation.Unknown());
    }
    async Task Because() => _exception = await Catch.Exception(_unitOfWork.Commit);
    [Fact] void should_fail_closed() => _exception.ShouldBeOfExactType<EventSequenceImplicitScopesNotSupported>();
    [Fact] void should_complete_even_after_failure() => _unitOfWork.IsCompleted.ShouldBeTrue();
}
