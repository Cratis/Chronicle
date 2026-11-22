// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_recreating_a_removed_namespace : given.a_coordinator
{
    readonly NamespaceIncarnationId _previous = NamespaceIncarnationId.New();

    void Establish()
    {
        _state = NamespaceLifecycle.Open(_name, _previous, _clock.GetUtcNow()) with { Revision = 41, Status = NamespaceLifecycleStatus.Removed };
        _markerState = new(_previous, false, 0);
    }

    async Task Because() => _result = await _coordinator.Ensure(true);

    [Fact] void should_never_reuse_the_retired_identity() => _result.Incarnation.ShouldNotEqual(_previous);
    [Fact] void should_keep_monotonic_control_revisions() => _result.Revision.ShouldEqual(43);
    [Fact] void should_remember_which_closed_marker_may_be_replaced() => _writes[0].PreviousIncarnation.ShouldEqual(_previous);
    [Fact] void should_repeat_creation_even_if_the_old_listing_survived() => _order.Contains("Create").ShouldBeTrue();
    [Fact] async Task should_open_only_against_the_retired_marker() => await _marker.Received(1).TryOpen(_result.Incarnation, _previous);
}
