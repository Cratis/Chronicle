// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelUniqueEventTypeConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueEventTypeConstraintDefinition;
using UniqueEventTypeConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// A client that declares no removal event registers the constraint with none, and the constraint keeps meaning
/// "at most one, forever".
/// </summary>
public class and_the_constraint_is_a_unique_event_type_constraint_without_a_removal_event : Specification
{
    static readonly EventTypeId _checkedOutEventTypeId = "LoanCheckedOut";

    Contracts.Events.Constraints.Constraint _contract;
    KernelUniqueEventTypeConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = "LoanOpen",
        Type = Contracts.Events.Constraints.ConstraintType.UniqueEventType,
        Definition = new(new UniqueEventTypeConstraintDefinitionContract
        {
            EventTypeIds = [_checkedOutEventTypeId.Value]
        })
    };

    void Because() => _result = (KernelUniqueEventTypeConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_have_no_removal_event() => _result.RemovedWith.ShouldBeEmpty();
}
