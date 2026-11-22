// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_removing_an_open_namespace : given.a_coordinator
{
    void Establish()
    {
        _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow());
        _markerState = new(_state.Incarnation, true, 0);
    }

    async Task Because() => _result = await _coordinator.Remove();

    [Fact] void should_install_the_wake_before_closing_admission() => _order.Take(2).ShouldEqual(["Wake", "WriteClosing"]);
    [Fact] void should_close_the_marker_before_any_destructive_effect() => (_order.IndexOf("CloseMarker") < _order.IndexOf("Forget")).ShouldBeTrue();
    [Fact] void should_finish_with_a_tombstone() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Removed);
    [Fact] void should_leave_the_marker_closed() => _markerState.IsOpen.ShouldBeFalse();
}
