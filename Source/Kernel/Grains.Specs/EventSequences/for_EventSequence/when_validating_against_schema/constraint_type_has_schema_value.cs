// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Specs.EventSequences.for_EventSequence.when_validating_against_schema;

public class constraint_type_has_schema_value : Specification
{
    [Fact] void should_have_schema_constraint_type() => Enum.IsDefined(ConstraintType.Schema).ShouldBeTrue();
    [Fact] void should_have_correct_value() => ((int)ConstraintType.Schema).ShouldEqual(3);
}
