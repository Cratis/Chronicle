// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Captures;

namespace Cratis.Chronicle.Captures.for_CapturesManager.when_a_namespace_is_added;

public class with_started_captures : given.a_captures_manager
{
    void Establish()
    {
        StartedState(_events);
        StartedState(_poll);
    }

    async Task Because() => await _manager.NamespaceAdded("new-tenant");

    [Fact] void should_subscribe_the_events_capture_in_the_namespace() => _subscriptions.Received(1).Subscribe(_eventStore, "new-tenant", Arg.Is<CaptureDefinition>(_ => _.Id == _events.Id));
    [Fact] void should_leave_poll_captures_alone() => _subscriptions.Received(1).Subscribe(_eventStore, "new-tenant", Arg.Any<CaptureDefinition>());
}
