// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_ensuring_a_new_namespace : given.a_coordinator
{
    async Task Because() => _result = await _coordinator.Ensure(false);

    [Fact] void should_install_the_wake_before_reserving_identity() => _order.ShouldEqual(["Wake", "WritePreparing", "OpenMarker", "Create", "WriteOpen"]);
    [Fact] void should_reserve_a_nonempty_identity() => _result.Incarnation.ShouldNotEqual(NamespaceIncarnationId.NotSet);
    [Fact] void should_open_only_after_creation_acknowledgement() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] void should_keep_the_reserved_identity() => _result.Incarnation.ShouldEqual(_writes[0].Incarnation);
    [Fact] void should_install_the_same_marker_identity() => _markerState.Incarnation.ShouldEqual(_result.Incarnation);
}
