// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Validation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.when_committing;

public class and_an_older_kernel_rejects_validate_only : given.a_unit_of_work
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
        _eventSequence.AppendMany(
            Arg.Any<IEnumerable<EventForEventSourceId>>(),
            Arg.Any<CorrelationId?>(),
            Arg.Any<IEnumerable<string>>(),
            Arg.Any<IDictionary<EventSourceId, ConcurrencyScope>>())
            .Returns<Task<AppendManyResult>>(_ => throw new CommandFailed(new CommandResult
            {
                ValidationResults = [new ValidationResult { Message = "At least one event is required." }]
            }));
    }

    async Task Because()
    {
        var owner = _unitOfWork.ClaimDecisionReadCommitOwnership();
        _exception = await Record.ExceptionAsync(() => _unitOfWork.CommitAsOwner(owner));
    }

    [Fact] void should_fail_closed_with_a_typed_result() => _exception.ShouldBeOfExactType<DecisionReadValidateOnlyNotSupported>();
    [Fact] void should_complete_the_unit_of_work() => _unitOfWork.IsCompleted.ShouldBeTrue();
}
