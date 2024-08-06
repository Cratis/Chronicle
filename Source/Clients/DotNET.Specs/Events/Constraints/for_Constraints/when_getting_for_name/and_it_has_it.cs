// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_getting_for_name;

public class and_it_has_it : given.two_constraints
{
    IConstraintDefinition _result;

    void Because() => _result = _constraints.GetFor(_secondConstraintName);

    [Fact] void should_have_the_constraint() => _result.ShouldNotBeNull();
    [Fact] void should_return_the_correct_constraint() => _result.ShouldEqual(_secondConstraint);
}
