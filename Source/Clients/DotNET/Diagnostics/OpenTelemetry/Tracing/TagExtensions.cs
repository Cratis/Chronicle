// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing;

/// <summary>
/// Extension methods for adding common tags to <see cref="Activity"/>.
/// </summary>
public static class TagExtensions
{
    const string EventSourceTypeTag = "cratis.eventsource.type";
    const string EventSourceIdTag = "cratis.eventsource.id";
    const string EventStoreNameTag = "cratis.eventstore.name";
    const string EventStoreNamespaceTag = "cratis.eventstore.namespace";
    const string EventSequenceIdTag = "cratis.eventsequence.id";

    /// <summary>
    /// Tag <see cref="EventStoreName"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="eventStoreName">The <see cref="EventStoreName"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, EventStoreName eventStoreName)
    {
        activity.SetTag(EventStoreNameTag, eventStoreName.Value);
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
        activity.SetTag(EventStoreNamespaceTag, eventStoreNamespace.Value);
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
        activity.SetTag(EventSequenceIdTag, eventSequenceId.Value);
        return activity;
    }

    /// <summary>
    /// Tag <see cref="EventSourceType"/> and <see cref="EventSourceId"/>.
    /// </summary>
    /// <param name="activity">The <see cref="Activity"/>.</param>
    /// <param name="eventSourceType">The <see cref="EventSourceType"/>.</param>
    /// <param name="eventSourceId">The <see cref="EventSourceId"/>.</param>
    /// <returns>The <see cref="Activity"/> with the tag.</returns>
    public static Activity Tag(this Activity activity, EventSourceType eventSourceType, EventSourceId eventSourceId)
    {
        activity
            .SetTag(EventSourceTypeTag, eventSourceType.Value)
            .SetTag(EventSourceIdTag, eventSourceId.Value);
        return activity;
    }
}
