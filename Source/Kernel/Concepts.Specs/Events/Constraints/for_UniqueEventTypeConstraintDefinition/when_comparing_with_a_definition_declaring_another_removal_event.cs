// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

/// <summary>
/// Registration decides whether a stored constraint is superseded by comparing it with the incoming definition, so
/// the removal event has to take part in equality. Leaving it out makes adding, changing, or dropping the event that
/// releases the constraint indistinguishable from re-registering the same definition — the store keeps the previous
/// rule and every append after that is answered against a constraint the client no longer declares.
/// </summary>
/// <remarks>
/// No reindex follows from the change: this constraint keeps no index, it is enforced by reading the appended events
/// for the event source, so the next append already answers against the new definition.
/// </remarks>
public class when_comparing_with_a_definition_declaring_another_removal_event : Specification
{
    static readonly ConstraintName _name = "loan-open";
    static readonly EventTypeId _coveredEventTypeId = "LoanCheckedOut";
    static readonly EventTypeId _returnedEventTypeId = "LoanReturned";
    static readonly EventTypeId _writtenOffEventTypeId = "LoanWrittenOff";

    UniqueEventTypeConstraintDefinition _definition;
    UniqueEventTypeConstraintDefinition _equivalent;
    UniqueEventTypeConstraintDefinition _withAnotherRemovalEvent;
    UniqueEventTypeConstraintDefinition _withoutRemovalEvent;

    void Establish()
    {
        _definition = new(_name, [_coveredEventTypeId], _returnedEventTypeId);
        _equivalent = new(_name, [_coveredEventTypeId], _returnedEventTypeId);
        _withAnotherRemovalEvent = new(_name, [_coveredEventTypeId], _writtenOffEventTypeId);
        _withoutRemovalEvent = new(_name, [_coveredEventTypeId]);
    }

    [Fact] void should_be_equal_to_an_equivalent_definition() => _definition.Equals(_equivalent).ShouldBeTrue();
    [Fact] void should_hash_alike_as_an_equivalent_definition() => _definition.GetHashCode().ShouldEqual(_equivalent.GetHashCode());
    [Fact] void should_not_be_equal_to_one_declaring_another_removal_event() => _definition.Equals(_withAnotherRemovalEvent).ShouldBeFalse();
    [Fact] void should_not_be_equal_through_the_interface_to_one_declaring_another_removal_event() => _definition.Equals((IConstraintDefinition)_withAnotherRemovalEvent).ShouldBeFalse();
    [Fact] void should_not_be_equal_to_one_declaring_no_removal_event() => _definition.Equals(_withoutRemovalEvent).ShouldBeFalse();
    [Fact] void should_require_no_reindex() => _definition.CompareWith(_withAnotherRemovalEvent).ShouldEqual(ConstraintChange.None);
}
