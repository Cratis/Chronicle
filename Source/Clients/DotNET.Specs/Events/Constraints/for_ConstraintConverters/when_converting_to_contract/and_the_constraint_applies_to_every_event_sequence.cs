// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintConverters.when_converting_to_contract;

/// <summary>
/// A constraint that applies everywhere sends no event sequences at all - exactly what a client that predates the
/// field sends, so the kernel treats both the same.
/// </summary>
public class and_the_constraint_applies_to_every_event_sequence : Specification
{
    UniqueConstraintDefinition _unique;
    Constraint _contract;

    void Establish() => _unique = new UniqueConstraintDefinition(
        "UniqueName",
        _ => string.Empty,
        [new UniqueConstraintEventDefinition("SomeEvent", ["Name"])],
        [],
        false);

    void Because() => _contract = _unique.ToContract();

    [Fact] void should_carry_no_event_sequences() => _contract.EventSequences.ShouldBeEmpty();
}
