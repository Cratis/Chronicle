// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueEventTypeConstraintDefinition;

/// <summary>
/// The event sequences are part of what the constraint means, so a change to them must be persisted - but this
/// constraint keeps no index, so nothing is rebuilt.
/// </summary>
public class when_comparing_with_a_definition_applying_to_other_event_sequences : Specification
{
    UniqueEventTypeConstraintDefinition _existing;
    UniqueEventTypeConstraintDefinition _definition;

    void Establish()
    {
        _existing = new("some-constraint", ["some-event-type"]);
        _definition = _existing with { EventSequences = [EventSequenceId.Log] };
    }

    [Fact] void should_not_be_equal() => _definition.Equals(_existing).ShouldBeFalse();
    [Fact] void should_not_require_reindex() => _definition.CompareWith(_existing).ShouldEqual(ConstraintChange.None);
    [Fact] void should_apply_to_the_event_log() => _definition.AppliesTo(EventSequenceId.Log).ShouldBeTrue();
    [Fact] void should_not_apply_to_the_outbox() => _definition.AppliesTo(EventSequenceId.Outbox).ShouldBeFalse();
}
