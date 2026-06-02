// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

public class and_existing_unique_constraint_changes : given.a_constraints_system
{
    IConstraintDefinition _existingConstraint;
    IConstraintDefinition _newConstraint;

    void Establish()
    {
        _existingConstraint = new UniqueConstraintDefinition(
            "UniquePeople",
            [new("PersonRegistered", ["ssn"])]);

        _newConstraint = new UniqueConstraintDefinition(
            "UniquePeople",
            [new("PersonRegistered", ["ssn", "countryCode"])]);

        _stateStorage.State.Constraints.Add(_existingConstraint);
    }

    async Task Because() => await _constraints.Register([_newConstraint]);

    [Fact]
    void should_publish_change_that_requires_reindex() =>
        _broadcastChannelWriter.Received().Publish(Arg.Is<ConstraintsChanged>(_ =>
            _.Changes.Count == 1 &&
            _.Changes.Single().Name == "UniquePeople" &&
            _.Changes.Single().RequiresReindex &&
            _.Changes.Single().ChangeTypes.Contains(ConstraintChangeType.IndexedPropertiesChanged)));
}
