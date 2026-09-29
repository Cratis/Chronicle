// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints.for_ConstraintDefinitionSerializer.when_serializing_a_constraint_definition;

/// <summary>
/// The event sequences are part of what registration compares against, so both definition types must bring them
/// back out of storage - a definition that lost them would apply to every event sequence again after a restart.
/// </summary>
public class and_it_applies_to_specific_event_sequences : given.a_stored_constraint_definition
{
    static readonly UniqueConstraintDefinition _unique = new(ConstraintNameValue, [new UniqueConstraintEventDefinition("the-event-type", ["the-property"])])
    {
        EventSequences = [EventSequenceId.Log]
    };

    static readonly UniqueEventTypeConstraintDefinition _uniqueEventType = new(ConstraintNameValue, ["the-event-type"])
    {
        EventSequences = [EventSequenceId.Log]
    };

    IConstraintDefinition _uniqueResult;
    IConstraintDefinition _uniqueEventTypeResult;

    void Because()
    {
        _uniqueResult = Read(Write(_unique));
        _uniqueEventTypeResult = Read(Write(_uniqueEventType));
    }

    [Fact] void should_read_back_the_unique_constraint_that_was_written() => _uniqueResult.ShouldEqual(_unique);
    [Fact] void should_keep_the_event_sequences_of_the_unique_constraint() => ((UniqueConstraintDefinition)_uniqueResult).EventSequences.ShouldContainOnly([EventSequenceId.Log]);
    [Fact] void should_read_back_the_unique_event_type_constraint_that_was_written() => _uniqueEventTypeResult.ShouldEqual(_uniqueEventType);
    [Fact] void should_keep_the_event_sequences_of_the_unique_event_type_constraint() => ((UniqueEventTypeConstraintDefinition)_uniqueEventTypeResult).EventSequences.ShouldContainOnly([EventSequenceId.Log]);
}
