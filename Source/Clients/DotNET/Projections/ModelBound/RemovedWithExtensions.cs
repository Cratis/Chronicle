// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using EventType = Cratis.Chronicle.Contracts.Events.EventType;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Extension methods for processing RemovedWith attributes.
/// </summary>
static class RemovedWithExtensions
{
    /// <summary>
    /// Processes a RemovedWith attribute and adds the removed with definition to the target dictionary.
    /// </summary>
    /// <param name="targetRemovedWith">The target RemovedWith dictionary to add the definition to.</param>
    /// <param name="eventTypes">The event types registry for resolving event type information.</param>
    /// <param name="attr">The RemovedWith attribute to process.</param>
    /// <param name="eventType">The event type that triggers removal.</param>
    internal static void ProcessRemovedWithAttribute(
        this IDictionary<EventType, RemovedWithDefinition> targetRemovedWith,
        IEventTypes eventTypes,
        Attribute attr,
        Type eventType)
    {
        var eventTypeId = eventTypes.GetEventTypeFor(eventType).ToContract();
        var keyProperty = attr.GetType().GetProperty(nameof(RemovedWithAttribute<object>.Key));
        var parentKeyProperty = attr.GetType().GetProperty(nameof(RemovedWithAttribute<object>.ParentKey));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;
        var parentKey = parentKeyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        targetRemovedWith[eventTypeId] = new RemovedWithDefinition
        {
            Key = key,
            ParentKey = parentKey
        };
    }

    /// <summary>
    /// Processes a RemovedWithJoin attribute and adds the removed with join definition to the target dictionary.
    /// </summary>
    /// <param name="targetRemovedWithJoin">The target RemovedWithJoin dictionary to add the definition to.</param>
    /// <param name="eventTypes">The event types registry for resolving event type information.</param>
    /// <param name="attr">The RemovedWithJoin attribute to process.</param>
    /// <param name="eventType">The event type that triggers removal.</param>
    internal static void ProcessRemovedWithJoinAttribute(
        this IDictionary<EventType, RemovedWithJoinDefinition> targetRemovedWithJoin,
        IEventTypes eventTypes,
        Attribute attr,
        Type eventType)
    {
        var eventTypeId = eventTypes.GetEventTypeFor(eventType).ToContract();
        var keyProperty = attr.GetType().GetProperty(nameof(RemovedWithJoinAttribute<object>.Key));

        var key = keyProperty?.GetValue(attr) as string ?? WellKnownExpressions.EventSourceId;

        targetRemovedWithJoin[eventTypeId] = new RemovedWithJoinDefinition
        {
            Key = key
        };
    }
}
