// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintDefinitionComparison.when_computing_version;

public class and_a_constraint_is_added : Specification
{
    IConstraintDefinition _first;
    IConstraintDefinition _second;
    ConstraintsVersion _before;
    ConstraintsVersion _after;

    void Establish()
    {
        _first = new UniqueConstraintDefinition("first", [new UniqueConstraintEventDefinition("some-event", ["Some"])]);
        _second = new UniqueConstraintDefinition("second", [new UniqueConstraintEventDefinition("other-event", ["Other"])]);
    }

    void Because()
    {
        _before = ConstraintDefinitionComparison.ComputeVersion([_first]);
        _after = ConstraintDefinitionComparison.ComputeVersion([_first, _second]);
    }

    [Fact] void should_produce_a_different_version() => _after.ShouldNotEqual(_before);
}
