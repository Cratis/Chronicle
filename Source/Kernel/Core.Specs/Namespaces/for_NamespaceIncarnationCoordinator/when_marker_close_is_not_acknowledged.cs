// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces.for_NamespaceIncarnationCoordinator;

public class when_marker_close_is_not_acknowledged : given.a_coordinator
{
    void Establish()
    {
        _state = NamespaceLifecycle.Open(_name, NamespaceIncarnationId.New(), _clock.GetUtcNow());
        _marker.TryClose(Arg.Any<NamespaceIncarnationId>()).Returns(Task.FromResult(false));
    }

    async Task Because() => _result = await _coordinator.Remove();

    [Fact] void should_not_advance_past_marker_close() => _result.Stage.ShouldEqual(NamespaceRemovalStage.CloseMarker);
    [Fact] void should_leave_admission_closed() => _result.Status.ShouldEqual(NamespaceLifecycleStatus.Closing);
    [Fact] async Task should_never_issue_destructive_effects() => await _effects.DidNotReceive().Remove(Arg.Any<NamespaceLifecycle>());
}
