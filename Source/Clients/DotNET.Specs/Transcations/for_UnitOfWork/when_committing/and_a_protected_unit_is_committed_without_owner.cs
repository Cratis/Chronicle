// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_a_protected_unit_is_committed_without_owner : given.a_unit_of_work
{
    Exception _exception;

    void Establish()
    {
        _eventStore.Name.Returns((EventStoreName)"store");
        _eventStore.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _unitOfWork.AddDecisionRead(new DecisionRead<object>(
            "source",
            null,
            _eventStore.Name,
            _eventStore.Namespace,
            0,
            [new EventType("created", EventTypeGeneration.First)]));
    }

    async Task Because() => _exception = await Record.ExceptionAsync(() => _unitOfWork.Commit());

    [Fact] void should_reject_early_commit() => _exception.ShouldBeOfExactType<ProtectedUnitOfWorkRequiresOwner>();
    [Fact] void should_leave_the_unit_open() => _unitOfWork.IsCompleted.ShouldBeFalse();
    [Fact] void should_not_append() => _eventSequence.DidNotReceiveWithAnyArgs().AppendMany(default!);
}
