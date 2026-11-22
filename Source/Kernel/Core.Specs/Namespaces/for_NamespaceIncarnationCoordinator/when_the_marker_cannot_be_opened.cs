// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_the_marker_cannot_be_opened : given.a_coordinator
{
    Exception _error;

    void Establish() => _marker.TryOpen(Arg.Any<NamespaceIncarnationId>(), Arg.Any<NamespaceIncarnationId?>()).Returns(Task.FromResult(false));

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Ensure(false));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<NamespaceRecoveryRequired>();
    [Fact] void should_persist_the_terminal_diagnostic() => _state.LastFailure.ShouldEqual("NamespaceMarkerOpenNotAcknowledged");
    [Fact] void should_require_recovery() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.RecoveryRequired);
    [Fact] async Task should_never_create_the_listing() => await _effects.DidNotReceive().Create(Arg.Any<NamespaceLifecycle>());
}
