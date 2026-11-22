// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_adopting_a_listed_legacy_namespace : given.a_coordinator
{
    async Task Because() => _result = await _coordinator.Ensure(true);

    [Fact] void should_install_identity_without_repeating_creation() => _order.ShouldEqual(["Wake", "WritePreparing", "OpenMarker", "WriteOpen"]);
    [Fact] void should_open_the_legacy_namespace() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Open);
    [Fact] async Task should_not_append_another_creation_notification() => await _effects.DidNotReceive().Create(Arg.Any<NamespaceLifecycle>());
}
