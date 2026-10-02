// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing_across_namespaces;

public class and_a_namespace_fails : given.a_pattern_capture_with_a_failing_namespace
{
    Exception _error;

    async Task Because() => _error = await Catch.Exception(() => _capture.SubscribeAcrossNamespaces(_eventStore));

    [Fact] void should_not_fail_the_caller() => _error.ShouldBeNull();
    [Fact] async Task should_have_attempted_the_failing_namespace() => await _failedObserver.Received(1).Subscribe<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false);
    [Fact] async Task should_subscribe_the_remaining_namespace() => await _observer.Received(1).Subscribe<IPatternCaptureSubscriber>(ObserverType.Reactor, Arg.Any<IEnumerable<EventType>>(), SiloAddress.Zero, isReplayable: false);
}
