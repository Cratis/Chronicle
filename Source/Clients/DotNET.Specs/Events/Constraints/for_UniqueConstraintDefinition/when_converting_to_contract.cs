// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using UniqueConstraintDefinitionContract = Cratis.Chronicle.Contracts.Events.Constraints.UniqueConstraintDefinition;

namespace Cratis.Chronicle.Events.Constraints.for_UniqueConstraintDefinition;

public class when_converting_to_contract : Specification
{
    static ConstraintName _constraintName = "My Constraint";
    string _removedWithEventType;
    UniqueConstraintDefinition _definition;
    EventType _firstEventType;
    Constraint _contract;
    EventType _secondEventType;
    UniqueConstraintDefinitionContract _definitionContract;

    void Establish()
    {
        _firstEventType = new EventType("First Event Type", EventTypeGeneration.First);
        _secondEventType = new EventType("Second Event Type", EventTypeGeneration.First);
        _removedWithEventType = Guid.NewGuid().ToString();
        _definition = new UniqueConstraintDefinition(
            _constraintName,
            _ => "",
            [
                new(_firstEventType.Id, ["First event, first Property", "First event, second Property"]),
                new(_secondEventType.Id, ["Second event, first Property", "Second event, second Property"])
            ],
            _removedWithEventType,
            false);
    }

    void Because()
    {
        _contract = _definition.ToContract();
        _definitionContract = _contract.Definition.Value as UniqueConstraintDefinitionContract;
    }

    [Fact] void should_have_correct_name() => _contract.Name.ShouldEqual(_constraintName.Value);
    [Fact] void should_have_correct_type() => _contract.Type.ShouldEqual(ConstraintType.Unique);
    [Fact] void should_have_correct_removed_with() => _contract.RemovedWith.ShouldEqual(_removedWithEventType);
    [Fact] void should_have_first_event_type() => _definitionContract.EventDefinitions[0].EventTypeId.ShouldEqual(_firstEventType.Id.Value);
    [Fact] void should_have_first_event_first_property() => _definitionContract.EventDefinitions[0].Properties.ToArray()[0].ShouldEqual(_definition.EventsWithProperties.First().Properties.ToArray()[0]);
    [Fact] void should_have_first_event_second_property() => _definitionContract.EventDefinitions[0].Properties.ToArray()[1].ShouldEqual(_definition.EventsWithProperties.First().Properties.ToArray()[1]);
    [Fact] void should_have_second_event_type() => _definitionContract.EventDefinitions[1].EventTypeId.ShouldEqual(_secondEventType.Id.Value);
    [Fact] void should_have_second_event_first_property() => _definitionContract.EventDefinitions[1].Properties.ToArray()[0].ShouldEqual(_definition.EventsWithProperties.Last().Properties.ToArray()[0]);
    [Fact] void should_have_second_event_second_property() => _definitionContract.EventDefinitions[1].Properties.ToArray()[1].ShouldEqual(_definition.EventsWithProperties.Last().Properties.ToArray()[1]);
}
