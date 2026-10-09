// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_subscribing;

public class in_one_namespace : given.a_capture_events_subscriptions
{
    async Task Because() => await _subscriptions.Subscribe(_eventStore, "added", _definition);

    [Fact] void should_only_subscribe_that_namespace() => _observersByNamespace.Keys.ShouldEqual(["added"]);
}
