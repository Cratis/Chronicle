// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Concepts.Events.Constraints.for_UniqueConstraintDefinition.when_checking_if_it_applies_to_an_event_sequence;

public class and_it_declares_no_event_sequences : Specification
{
    UniqueConstraintDefinition _definition;

    void Establish() => _definition = new("some-constraint", [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])]);

    [Fact] void should_apply_to_the_event_log() => _definition.AppliesTo(EventSequenceId.Log).ShouldBeTrue();
    [Fact] void should_apply_to_the_outbox() => _definition.AppliesTo(EventSequenceId.Outbox).ShouldBeTrue();
}
