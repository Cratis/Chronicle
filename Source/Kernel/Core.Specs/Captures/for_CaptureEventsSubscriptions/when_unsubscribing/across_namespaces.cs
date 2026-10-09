// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_unsubscribing;

public class across_namespaces : given.a_capture_events_subscriptions
{
    async Task Because() => await _subscriptions.Unsubscribe(_eventStore, _definition);

    [Fact] void should_unsubscribe_every_namespace() => _observersByNamespace.Values.ShouldEachConformTo(observer => observer.ReceivedCalls().Count() == 1);
    [Fact] void should_cover_both_namespaces() => _observersByNamespace.Count.ShouldEqual(2);
}
