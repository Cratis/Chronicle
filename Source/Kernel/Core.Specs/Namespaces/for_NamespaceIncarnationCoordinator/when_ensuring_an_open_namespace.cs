// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_ensuring_an_open_namespace : given.a_coordinator
{
    readonly NamespaceIncarnationId _identity = NamespaceIncarnationId.New();

    void Establish()
    {
        _state = NamespaceLifecycle.Open(_name, _identity, _clock.GetUtcNow());
        _markerState = new(_identity, true, 0);
    }

    async Task Because() => _result = await _coordinator.Ensure(true);

    [Fact] void should_keep_the_existing_identity() => _result.Incarnation.ShouldEqual(_identity);
    [Fact] void should_not_repeat_any_initialization_effect() => _order.Count.ShouldEqual(0);
    [Fact] void should_not_increment_the_control_revision() => _writes.Count.ShouldEqual(0);
}
