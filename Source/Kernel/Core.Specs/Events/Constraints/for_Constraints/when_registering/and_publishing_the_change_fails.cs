// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

/// <summary>
/// The definitions are persisted by the time the change is published, so a retried registration would find nothing
/// changed. A failed publish must neither fail the registration nor keep the stale indexes from being rebuilt.
/// </summary>
public class and_publishing_the_change_fails : given.a_constraints_system
{
    Exception _error;

    void Establish() => _broadcastChannelWriter.Publish(Arg.Any<ConstraintsChanged>()).Returns(Task.FromException(new InvalidOperationException()));

    async Task Because() => _error = await Catch.Exception(() => _constraints.Register(
    [
        new UniqueConstraintDefinition("UniqueEmail", [new("InvitationSent", ["email"])])
    ]));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_write_state() => _storageStats.Writes.ShouldEqual(1);

    [Fact]
    void should_still_rebuild_the_stale_indexes() =>
        _constraintIndexes.Received(1).RebuildStaleIndexes(
            Arg.Any<EventStoreName>(),
            Arg.Any<IReadOnlyCollection<IConstraintDefinition>>(),
            Arg.Any<IReadOnlyCollection<IConstraintDefinition>>());
}
