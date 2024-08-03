// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints.for_Constraints.when_getting_for_name;

public class and_it_does_not_have_it : given.no_constraints
{
    UnknownConstraint _result;

    async Task Establish()
    {
        await _constraints.Discover();
    }

    void Because() => _result = Catch.Exception<UnknownConstraint>(() => _constraints.GetFor("SomeConstraint"));

    [Fact] void should_throw_unknown_constraint() => _result.ShouldBeOfExactType<UnknownConstraint>();
    [Fact] void should_have_constraint_name_asked_for_on_exception() => _result.ConstraintName.Value.ShouldEqual("SomeConstraint");
}
