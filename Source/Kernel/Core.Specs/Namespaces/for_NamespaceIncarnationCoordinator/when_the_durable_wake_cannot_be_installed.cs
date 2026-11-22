// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_the_durable_wake_cannot_be_installed : given.a_coordinator
{
    Exception _error;

    void Establish() => _effects.EnsureWake().Returns(Task.FromException(new InvalidOperationException("reminder unavailable")));

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Ensure(false));

    [Fact] void should_not_persist_orphaned_pending_work() => _writes.Count.ShouldEqual(0);
    [Fact] async Task should_not_open_a_marker() => await _marker.DidNotReceive().TryOpen(Arg.Any<NamespaceIncarnationId>(), Arg.Any<NamespaceIncarnationId?>());
    [Fact] async Task should_not_create_a_listing() => await _effects.DidNotReceive().Create(Arg.Any<NamespaceLifecycle>());
    [Fact] void should_propagate_the_reminder_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
