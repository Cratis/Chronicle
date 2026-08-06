// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using KernelUniqueEventTypeConstraintDefinition = Cratis.Chronicle.Concepts.Events.Constraints.UniqueEventTypeConstraintDefinition;
using UniqueEventTypeConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition;

namespace Cratis.Chronicle.Services.Events.Constraints.for_ConstraintConverters.when_converting_to_chronicle;

/// <summary>
/// The removal event travels on the constraint rather than inside the definition payload, which is why the value form
/// reads it and this one used to walk past it. Dropping it here would leave the kernel enforcing "at most one,
/// forever" against a client that declared otherwise, with nothing anywhere reporting a problem.
/// </summary>
public class and_the_constraint_is_a_unique_event_type_constraint_with_a_removal_event : Specification
{
    const string ConstraintNameValue = "LoanOpen";
    static readonly EventTypeId _checkedOutEventTypeId = "LoanCheckedOut";
    static readonly EventTypeId _returnedEventTypeId = "LoanReturned";

    Contracts.Events.Constraints.Constraint _contract;
    KernelUniqueEventTypeConstraintDefinition _result;

    void Establish() => _contract = new()
    {
        Name = ConstraintNameValue,
        Type = Contracts.Events.Constraints.ConstraintType.UniqueEventType,
        RemovedWith = _returnedEventTypeId.Value,
        Definition = new(new UniqueEventTypeConstraintDefinitionContract
        {
            EventTypeIds = [_checkedOutEventTypeId.Value]
        })
    };

    void Because() => _result = (KernelUniqueEventTypeConstraintDefinition)_contract.ToChronicle();

    [Fact] void should_have_the_constraint_name() => _result.Name.Value.ShouldEqual(ConstraintNameValue);
    [Fact] void should_cover_the_event_type() => _result.EventTypeIds.ShouldContainOnly([_checkedOutEventTypeId]);
    [Fact] void should_carry_the_removal_event() => _result.RemovedWith.ShouldEqual(_returnedEventTypeId);
}
