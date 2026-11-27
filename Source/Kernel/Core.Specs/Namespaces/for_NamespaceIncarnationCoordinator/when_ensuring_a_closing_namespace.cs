// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_ensuring_a_closing_namespace : given.a_coordinator
{
    Exception _error;

    void Establish() => _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow()).BeginRemoval(Guid.NewGuid(), _clock.GetUtcNow());

    async Task Because() => _error = await Catch.Exception(() => _coordinator.Ensure(true));

    [Fact] void should_refuse_admission() => _error.ShouldBeOfExactType<NamespaceClosing>();
    [Fact] void should_not_rewrite_control_state() => _writes.Count.ShouldEqual(0);
    [Fact] void should_not_open_a_marker_or_create_a_listing() => _order.Count.ShouldEqual(0);
}
