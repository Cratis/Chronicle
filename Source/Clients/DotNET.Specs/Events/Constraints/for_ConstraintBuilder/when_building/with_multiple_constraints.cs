// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building;

public class with_multiple_constraints : given.a_constraint_builder_with_owner
{
    static ConstraintName _firstConstraintName = "FirstConstraint";
    static ConstraintName _secondConstraintName = "SecondConstraint";

    IImmutableList<IConstraintDefinition> _result;
    IConstraintDefinition _firstConstraint;
    IConstraintDefinition _secondConstraint;

    void Establish()
    {
        _firstConstraint = Substitute.For<IConstraintDefinition>();
        _firstConstraint.Name.Returns(_firstConstraintName);
        _secondConstraint = Substitute.For<IConstraintDefinition>();
        _secondConstraint.Name.Returns(_secondConstraintName);
        _constraintBuilder.AddConstraint(_firstConstraint);
        _constraintBuilder.AddConstraint(_secondConstraint);
    }

    void Because() => _result = _constraintBuilder.Build();

    [Fact] void should_have_the_correct_number_of_constraints() => _result.Count.ShouldEqual(2);
    [Fact] void should_have_the_correct_constraints() => _result.ShouldContainOnly(_firstConstraint, _secondConstraint);
}
