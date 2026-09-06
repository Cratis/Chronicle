// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Patterns.for_PatternCapture.when_subscribing_across_namespaces;

/// <summary>
/// Event types are registered per event store, but capture subscribes per namespace - so a registration has to
/// reach every namespace of that store. Nothing else does: a server starting against a store whose clients have not
/// connected yet has no event types to subscribe to, and without this the ones arriving afterwards would go
/// uncaptured until the next restart.
/// </summary>
public class with_multiple_namespaces : given.a_pattern_capture
{
    async Task Because()
    {
        EventTypesAre("ExpenseReportSubmitted", "ExpenseReportApproved");
        _namespaces.GetAll().Returns([
            EventStoreNamespaceName.Default,
            new EventStoreNamespaceName("tenant-a")
        ]);

        await _capture.SubscribeAcrossNamespaces(_eventStore);
    }

    [Fact] async Task should_subscribe_once_for_every_namespace() =>
        await _observer.Received(2).Subscribe<IPatternCaptureSubscriber>(
            ObserverType.Reactor,
            Arg.Any<IEnumerable<EventType>>(),
            Arg.Any<SiloAddress>(),
            Arg.Any<object?>(),
            false,
            Arg.Any<ObserverFilters?>());
}
