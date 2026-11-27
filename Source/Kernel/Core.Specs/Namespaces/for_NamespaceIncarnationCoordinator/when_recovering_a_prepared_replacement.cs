// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_recovering_a_prepared_replacement : given.a_coordinator
{
    readonly NamespaceIncarnationId _reserved = NamespaceIncarnationId.New();
    readonly NamespaceIncarnationId _previous = NamespaceIncarnationId.New();

    void Establish()
    {
        _state = new(_name, _reserved, _clock.GetUtcNow()) { Revision = 42, PreviousIncarnation = _previous, NotifyOnOpen = true };
        _markerState = new(_reserved, true, 0);
    }

    async Task Because() => _result = await CreateCoordinator().Ensure(true);

    [Fact] void should_reuse_the_prepared_identity_after_a_lost_marker_reply() => _result.Incarnation.ShouldEqual(_reserved);
    [Fact] void should_not_reserve_another_revision_before_marker_retry() => _writes.Count.ShouldEqual(1);
    [Fact] async Task should_retain_the_exact_previous_identity_across_activation() => await _marker.Received(1).TryOpen(_reserved, _previous);
    [Fact] void should_not_skip_pending_creation_because_the_listing_exists() => _order.Contains("Create").ShouldBeTrue();
}
