// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintConverters.when_converting_to_contract;

public class and_the_constraints_apply_to_specific_event_sequences : Specification
{
    static readonly EventSequenceId _custom = "custom-sequence";

    UniqueConstraintDefinition _unique;
    UniqueEventTypeConstraintDefinition _uniqueEventType;
    Constraint _uniqueContract;
    Constraint _uniqueEventTypeContract;

    void Establish()
    {
        _unique = new UniqueConstraintDefinition(
            "UniqueName",
            _ => string.Empty,
            [new UniqueConstraintEventDefinition("SomeEvent", ["Name"])],
            [],
            false)
        {
            EventSequences = [EventSequenceId.Log, _custom]
        };

        _uniqueEventType = new UniqueEventTypeConstraintDefinition(
            "OnlyOnce",
            _ => string.Empty,
            ["SomeEvent"],
            [])
        {
            EventSequences = [EventSequenceId.Log]
        };
    }

    void Because()
    {
        _uniqueContract = _unique.ToContract();
        _uniqueEventTypeContract = _uniqueEventType.ToContract();
    }

    [Fact] void should_carry_the_event_sequences_of_the_unique_constraint() => _uniqueContract.EventSequences.ShouldContainOnly([EventSequenceId.LogId, _custom.Value]);
    [Fact] void should_carry_the_event_sequences_of_the_unique_event_type_constraint() => _uniqueEventTypeContract.EventSequences.ShouldContainOnly([EventSequenceId.LogId]);
}
