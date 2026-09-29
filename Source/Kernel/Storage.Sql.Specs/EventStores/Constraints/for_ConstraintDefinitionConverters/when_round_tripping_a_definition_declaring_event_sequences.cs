// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Constraints.for_ConstraintDefinitionConverters;

/// <summary>
/// The event sequences are an init-only member rather than a constructor parameter, so System.Text.Json has to set
/// them after construction. A definition that came back without them would apply to every event sequence again.
/// </summary>
public class when_round_tripping_a_definition_declaring_event_sequences : Specification
{
    UniqueConstraintDefinition _unique;
    UniqueEventTypeConstraintDefinition _uniqueEventType;
    IConstraintDefinition _uniqueResult;
    IConstraintDefinition _uniqueEventTypeResult;

    void Establish()
    {
        _unique = new UniqueConstraintDefinition("unique-name", [new("NameClaimed", ["Name"])])
        {
            EventSequences = [EventSequenceId.Log]
        };
        _uniqueEventType = new UniqueEventTypeConstraintDefinition("once-only", [(EventTypeId)"Registered"])
        {
            EventSequences = [EventSequenceId.Log]
        };
    }

    void Because()
    {
        _uniqueResult = _unique.ToSql(1).ToKernel();
        _uniqueEventTypeResult = _uniqueEventType.ToSql(1).ToKernel();
    }

    [Fact] void should_read_back_the_unique_constraint_that_was_written() => _uniqueResult.ShouldEqual(_unique);
    [Fact] void should_keep_the_event_sequences_of_the_unique_constraint() => ((UniqueConstraintDefinition)_uniqueResult).EventSequences.ShouldContainOnly([EventSequenceId.Log]);
    [Fact] void should_read_back_the_unique_event_type_constraint_that_was_written() => _uniqueEventTypeResult.ShouldEqual(_uniqueEventType);
    [Fact] void should_keep_the_event_sequences_of_the_unique_event_type_constraint() => ((UniqueEventTypeConstraintDefinition)_uniqueEventTypeResult).EventSequences.ShouldContainOnly([EventSequenceId.Log]);
}
