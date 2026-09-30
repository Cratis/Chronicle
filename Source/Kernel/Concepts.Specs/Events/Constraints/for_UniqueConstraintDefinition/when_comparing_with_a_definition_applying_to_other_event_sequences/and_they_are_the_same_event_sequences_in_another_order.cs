// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition.when_comparing_with_a_definition_applying_to_other_event_sequences;

/// <summary>
/// The event sequences are a set. Comparing them in declaration order would report a re-registration that merely
/// lists them differently as a change, persisting another version of the same definition on every connect.
/// </summary>
public class and_they_are_the_same_event_sequences_in_another_order : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _existing;
    UniqueConstraintDefinition _definition;

    void Establish()
    {
        _existing = new(_name, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])])
        {
            EventSequences = [EventSequenceId.Log, EventSequenceId.Outbox]
        };
        _definition = _existing with { EventSequences = [EventSequenceId.Outbox, EventSequenceId.Log] };
    }

    [Fact] void should_be_equal() => _definition.Equals(_existing).ShouldBeTrue();
    [Fact] void should_hash_alike() => _definition.GetHashCode().ShouldEqual(_existing.GetHashCode());
    [Fact] void should_report_no_change() => _definition.CompareWith(_existing).ShouldEqual(ConstraintChange.None);
}
