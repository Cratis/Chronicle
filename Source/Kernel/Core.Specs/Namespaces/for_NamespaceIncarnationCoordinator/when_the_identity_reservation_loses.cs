// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_the_identity_reservation_loses : given.a_coordinator
{
    Exception _error;

    void Establish() => _lifecycle.TryWrite(_name, Arg.Any<long?>(), Arg.Any<NamespaceLifecycle>()).Returns(Task.FromResult(false));

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Ensure(false));

    [Fact] void should_report_a_stale_operation() => _error.ShouldBeOfExactType<NamespaceLifecycleChanged>();
    [Fact] void should_not_issue_an_unreserved_marker_or_creation_effect() => _order.ShouldEqual(["Wake"]);
}
