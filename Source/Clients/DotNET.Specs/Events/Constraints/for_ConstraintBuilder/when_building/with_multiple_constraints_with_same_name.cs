// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_ConstraintBuilder.when_building;

public class with_multiple_constraints_with_same_name : given.a_constraint_builder_with_owner
{
    static ConstraintName _firstName = "SomeName";
    static ConstraintName _secondName = "SomeOtherName";
    DuplicateConstraintNames _exception;

    void Establish()
    {
        _constraintBuilder.AddConstraint(CreateConstraint(_firstName));
        _constraintBuilder.AddConstraint(CreateConstraint(_firstName));
        _constraintBuilder.AddConstraint(CreateConstraint(_secondName));
        _constraintBuilder.AddConstraint(CreateConstraint(_secondName));
    }

    void Because() => _exception = Catch.Exception<DuplicateConstraintNames>(() => _constraintBuilder.Build());

    [Fact] void should_throw_duplicate_constraint_names() => _exception.ShouldNotBeNull();
    [Fact] void should_have_the_correct_constraint_names() => _exception.ConstraintNames.ShouldContainOnly(_firstName, _secondName);
}
