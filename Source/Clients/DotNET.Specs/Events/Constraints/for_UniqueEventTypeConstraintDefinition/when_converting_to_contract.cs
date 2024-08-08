// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using UniqueEventTypeConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueEventTypeConstraintDefinition;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

public class when_converting_to_contract : Specification
{
    static ConstraintName _constraintName = "My Constraint";
    UniqueEventTypeConstraintDefinition _definition;
    EventType _eventType;
    Constraint _contract;
    UniqueEventTypeConstraintDefinitionContract _definitionContract;

    void Establish()
    {
        _eventType = new EventType("First Event Type", EventGeneration.First);
        _definition = new UniqueEventTypeConstraintDefinition(
            _constraintName,
            _ => "",
            _eventType);
    }

    void Because()
    {
        _contract = _definition.ToContract();
        _definitionContract = _contract.Definition.Value as UniqueEventTypeConstraintDefinitionContract;
    }

    [Fact] void should_have_correct_name() => _contract.Name.ShouldEqual(_constraintName.Value);
    [Fact] void should_have_correct_type() => _contract.Type.ShouldEqual(ConstraintType.UniqueEventType);
    [Fact] void should_event_type() => _definitionContract.EventType.Id.ShouldEqual(_eventType.Id.Value);
}
