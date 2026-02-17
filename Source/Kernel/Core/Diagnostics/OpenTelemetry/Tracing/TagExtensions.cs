// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Extension methods for adding common tags to <see cref="Activity"/>.
/// </summary>
public static class TagExtensions
{
    const string EventSourceType = "cratis.eventsource.type";
    const string EventSourceId = "cratis.eventsource.id";
    const string EventStoreName = "cratis.eventstore.name";
    const string EventStoreNamespace = "cratis.eventstore.namespace";
    const string EventSequenceId = "cratis.eventsequence.id";
    const string ObserverId = "cratis.observer.id";
    const string ObserverType = "cratis.observer.type";
    const string ConnectionId = "cratis.connection.id";

    /// <summary>
    /// Tag <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/>.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, EventSourceType eventSourceType, EventSourceId eventSourceId)
    {
        activity
            .SetTag(EventSourceType, eventSourceType.Value)
            .SetTag(EventSourceId, eventSourceId.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="eventStoreName">The <see cref="EventStoreName"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, EventStoreName eventStoreName)
    {
        activity.SetTag(EventStoreName, eventStoreName.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="EventStoreNamespaceName"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, EventStoreNamespaceName eventStoreNamespace)
    {
        activity.SetTag(EventStoreNamespace, eventStoreNamespace.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="EventSequenceId"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, EventSequenceId eventSequenceId)
    {
        activity.SetTag(EventSequenceId, eventSequenceId.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="ObserverId"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="observerId">The <see cref="ObserverId"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, ObserverId observerId)
    {
        activity.SetTag(ObserverId, observerId.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="connectionId">The <see cref="ConnectionId"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, ConnectionId connectionId)
    {
        activity.SetTag(ConnectionId, connectionId.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="ObserverType"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="observerType">The <see cref="ObserverType"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, ObserverType observerType)
    {
        var observerTypeTag = observerType switch
        {
            Concepts.Observation.ObserverType.Reactor => "Reactor",
            Concepts.Observation.ObserverType.Projection => "Projection",
            Concepts.Observation.ObserverType.Reducer => "Reducer",
            Concepts.Observation.ObserverType.External => "External",
            _ => "Unknown"
        };
        activity.SetTag(ObserverType, observerTypeTag);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="ConnectedObserverKey"/> and <see cref="ObserverType"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="key">The <see cref="ConnectedObserverKey"/>.</param>
    /// <param name="observerType">The <see cref="ObserverType"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, ConnectedObserverKey key, ObserverType? observerType = null)
    {
        activity.Tag(key.ObserverId);
        activity.Tag(key.EventSequenceId);
        activity.Tag(key.ConnectionId);
        activity.Tag(key.Namespace);
        activity.Tag(key.EventStore);
        if (observerType.HasValue)
        {
            activity.Tag(observerType.Value);
        }

        return activity;
    }
}
