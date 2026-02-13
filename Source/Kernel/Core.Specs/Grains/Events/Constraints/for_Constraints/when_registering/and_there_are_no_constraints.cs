// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Grains.Events.Constraints.when_registering;

public class and_there_are_no_constraints : given.a_constraints_system
{
    IConstraintDefinition _firstConstraint;
    IConstraintDefinition _secondConstraint;

    void Establish()
    {
        _firstConstraint = Substitute.For<IConstraintDefinition>();
        _secondConstraint = Substitute.For<IConstraintDefinition>();
    }

    async Task Because() => await _constraints.Register([_firstConstraint, _secondConstraint]);

    [Fact] void should_add_first_constraint() => _stateStorage.State.Constraints.ShouldContain(_firstConstraint);
    [Fact] void should_add_second_constraint() => _stateStorage.State.Constraints.ShouldContain(_secondConstraint);
    [Fact] void should_write_state() => _storageStats.Writes.ShouldEqual(1);
    [Fact] void should_broadcast_constraints_changed() => _broadcastChannelWriter.Received().Publish(Arg.Any<ConstraintsChanged>());
}
