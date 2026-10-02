// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_ensuring_subscription_for_durable_append;

public class and_the_state_snapshot_is_missing : given.a_pattern_capture
{
    bool _subscribed;

    void Establish()
    {
        EventTypesAre("CustomerNamed");
        _namespaceStorage.HasData().Returns(false);
    }

    async Task Because() => _subscribed = await _capture.EnsureSubscribedForDurableAppend(_eventStore, _namespace);

    [Fact] void should_report_a_subscription() => _subscribed.ShouldBeTrue();
    [Fact] async Task should_not_depend_on_the_state_snapshot() => await _namespaceStorage.DidNotReceive().HasData();
    [Fact] async Task should_subscribe_the_observer() => await _observer.Received(1).EnsureSubscribed<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), Arg.Any<SiloAddress>(), Arg.Any<object?>(), false);
}
