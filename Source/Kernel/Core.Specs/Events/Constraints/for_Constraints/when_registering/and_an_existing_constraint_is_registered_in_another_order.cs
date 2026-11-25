// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

public class and_an_existing_constraint_is_registered_in_another_order : given.a_constraints_system
{
    void Establish() =>
        _stateStorage.State.Constraints.Add(new UniqueConstraintDefinition("unique", [new("first", ["A"]), new("second", ["B"])], ["removed-one", "removed-two"]));

    async Task Because() =>
        await _constraints.Register([new UniqueConstraintDefinition("unique", [new("second", ["B"]), new("first", ["A"])], ["removed-two", "removed-one"])]);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_broadcast_constraints_changed() => _broadcastChannelWriter.DidNotReceive().Publish(Arg.Any<ConstraintsChanged>());

    [Fact]
    void should_not_rebuild_any_index() =>
        _constraintIndexes.DidNotReceive().RebuildStaleIndexes(
            Arg.Any<EventStoreName>(),
            Arg.Any<IReadOnlyCollection<IConstraintDefinition>>(),
            Arg.Any<IReadOnlyCollection<IConstraintDefinition>>());
}
