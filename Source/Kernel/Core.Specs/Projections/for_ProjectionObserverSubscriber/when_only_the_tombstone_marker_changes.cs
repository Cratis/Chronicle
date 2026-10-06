// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.for_ProjectionObserverSubscriber;

public class when_only_the_tombstone_marker_changes : given.a_subscriber_with_a_cached_pipeline
{
    async Task Establish()
    {
        var type = new EventType("some-event", 1);
        var from = new FromDefinition(new Dictionary<PropertyPath, string>(), "$eventSourceId", null);
        _definition.From[type] = from;
        await _subscriber.OnProjectionDefinitionsChanged(_definition with
        {
            From = new Dictionary<EventType, FromDefinition> { [type with { Tombstone = true }] = from }
        });
        _pipelines.ClearReceivedCalls();
        _factory.ClearReceivedCalls();
    }

    Task Because() => _subscriber.OnActivateAsync(CancellationToken.None);

    [Fact] void should_not_evict_the_pipeline() => _pipelines.DidNotReceive().EvictFor(_key.EventStore, _key.Namespace, _key.ObserverId);
}
