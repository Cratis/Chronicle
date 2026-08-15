// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelUniqueEventTypeConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueEventTypeConstraintDefinition;
using UniqueEventTypeConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// The other arm of the conversion reads the removal events through the same seam, so the payload an older client
/// sends has to survive it too. Here the loss would be a cycle that never reopens: the loan is returned, the
/// constraint is registered as if nothing released it, and the borrower can never take another one.
/// </summary>
public class and_a_unique_event_type_constraint_carries_a_single_removal_event : Specification
{
    static readonly EventTypeId _checkedOutEventTypeId = "LoanCheckedOut";
    static readonly EventTypeId _returnedEventTypeId = "LoanReturned";

    Contracts.Events.Constraints.Constraint _contract;
    KernelUniqueEventTypeConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = "LoanOpen",
        Type = Contracts.Events.Constraints.ConstraintType.UniqueEventType,
        RemovedWith = [_returnedEventTypeId.Value],
        Definition = new(new UniqueEventTypeConstraintDefinitionContract
        {
            EventTypeIds = [_checkedOutEventTypeId.Value]
        })
    };

    void Because() => _result = (KernelUniqueEventTypeConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_keep_the_removal_event_that_was_declared() => _result.RemovedWith.ShouldContainOnly([_returnedEventTypeId]);
    [Fact] void should_still_cover_the_declared_event_type() => _result.EventTypeIds.ShouldContainOnly([_checkedOutEventTypeId]);
}
