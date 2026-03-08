// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

public class and_there_is_a_new_one : given.a_constraints_system
{
    IConstraintDefinition _existingFirstConstraint;
    IConstraintDefinition _firstConstraint;
    IConstraintDefinition _secondConstraint;

    void Establish()
    {
        _existingFirstConstraint = Substitute.For<IConstraintDefinition>();
        _existingFirstConstraint.Name.Returns<ConstraintName>("First");

        _firstConstraint = Substitute.For<IConstraintDefinition>();
        _firstConstraint.Name.Returns<ConstraintName>("First");
        _secondConstraint = Substitute.For<IConstraintDefinition>();
        _secondConstraint.Name.Returns<ConstraintName>("Second");

        _existingFirstConstraint.Equals(_firstConstraint).Returns(true);
        _firstConstraint.Equals(_existingFirstConstraint).Returns(true);

        _stateStorage.State.Constraints.Add(_existingFirstConstraint);
    }

    async Task Because() => await _constraints.Register([_firstConstraint, _secondConstraint]);

    [Fact] void should_only_have_two_constraints() => _stateStorage.State.Constraints.Count.ShouldEqual(2);
    [Fact] void should_have_the_first_constraint() => _stateStorage.State.Constraints.ShouldContain(_firstConstraint);
    [Fact] void should_add_second_constraint() => _stateStorage.State.Constraints.ShouldContain(_secondConstraint);
    [Fact] void should_broadcast_constraints_changed() => _broadcastChannelWriter.Received().Publish(Arg.Any<ConstraintsChanged>());
}
