// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_creation_notification_is_rejected : given.a_coordinator
{
    Exception _error;

    void Establish() => _effects.Create(Arg.Any<NamespaceLifecycle>()).Returns(Task.FromException(new InvalidOperationException("notification rejected")));

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Ensure(false));

    [Fact] void should_keep_the_reserved_identity_pending() => _state.Status.ShouldEqual(NamespaceLifecycleStatus.Preparing);
    [Fact] void should_not_report_an_open_namespace() => _writes.Exists(_ => _.Status == NamespaceLifecycleStatus.Open).ShouldBeFalse();
    [Fact] void should_propagate_the_creation_failure() => _error.ShouldBeOfExactType<InvalidOperationException>();
}
