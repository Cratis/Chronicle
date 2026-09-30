// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Events.Constraints.when_registering;

/// <summary>
/// The definitions are persisted before the rebuild starts, so the registration itself has succeeded; a failure to
/// start the rebuild is logged rather than reported to a client that would retry into a registration with no change.
/// </summary>
public class and_rebuilding_the_stale_indexes_fails : given.a_constraints_system
{
    Exception _error;

    void Establish() => _constraintIndexes
        .RebuildStaleIndexes(Arg.Any<EventStoreName>(), Arg.Any<IReadOnlyCollection<IConstraintDefinition>>(), Arg.Any<IReadOnlyCollection<IConstraintDefinition>>())
        .Returns(Task.FromException(new InvalidOperationException()));

    async Task Because() => _error = await Catch.Exception(() => _constraints.Register(
    [
        new UniqueConstraintDefinition("UniqueEmail", [new("InvitationSent", ["email"])])
    ]));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_write_state() => _storageStats.Writes.ShouldEqual(1);
}
