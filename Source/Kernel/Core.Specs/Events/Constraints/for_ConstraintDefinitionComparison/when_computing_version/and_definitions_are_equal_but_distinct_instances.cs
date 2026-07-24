// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_computing_version;

public class and_definitions_are_equal_but_distinct_instances : Specification
{
    ConstraintsVersion _first;
    ConstraintsVersion _second;

    void Because()
    {
        _first = ConstraintDefinitionComparison.ComputeVersion(
        [
            new UniqueConstraintDefinition("unique-thing", [new UniqueConstraintEventDefinition("some-event", ["Some"])]),
            new UniqueEventTypeConstraintDefinition("unique-type", "another-event")
        ]);
        _second = ConstraintDefinitionComparison.ComputeVersion(
        [
            new UniqueConstraintDefinition("unique-thing", [new UniqueConstraintEventDefinition("some-event", ["Some"])]),
            new UniqueEventTypeConstraintDefinition("unique-type", "another-event")
        ]);
    }

    [Fact] void should_produce_the_same_version() => _second.ShouldEqual(_first);
}
