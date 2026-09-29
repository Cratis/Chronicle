// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition.when_comparing_with_a_definition_applying_to_other_event_sequences;

/// <summary>
/// Narrowing stops validation and indexing for the sequences no longer covered, and leaves every sequence that is
/// still covered indexed exactly as before - so it is a change to persist, but nothing needs rebuilding.
/// </summary>
public class and_it_now_applies_to_fewer_event_sequences : Specification
{
    static readonly ConstraintName _name = "some-constraint";

    UniqueConstraintDefinition _existing;
    UniqueConstraintDefinition _definition;
    ConstraintChange _change;

    void Establish()
    {
        _existing = new(_name, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])]);
        _definition = _existing with { EventSequences = [EventSequenceId.Log] };
    }

    void Because() => _change = _definition.CompareWith(_existing);

    [Fact] void should_not_be_equal() => _definition.Equals(_existing).ShouldBeFalse();
    [Fact] void should_not_require_reindex() => _change.ShouldEqual(ConstraintChange.None);
}
