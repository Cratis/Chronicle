// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation.ObserverRouting;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents how the silo-local routing table and the subscriptions of a queue disagreed on the observers of one batch.
/// </summary>
/// <param name="QueueNotTable">Observers the queue's subscriptions take the batch for and the routing table does not.</param>
/// <param name="TableNotQueue">Observers the routing table takes the batch for and the queue holds no subscription of.</param>
/// <remarks>
/// The comparison is by event type only, because a route carries no filters. <see cref="QueueNotTable"/> is the one that
/// matters before dispatch moves to the table: each is an observer the table would not deliver to. <see cref="TableNotQueue"/>
/// is expected for an observer with no connected client or one released while dormant.
/// </remarks>
public record AppendedEventsQueueShadowRouting(IReadOnlyCollection<ObserverId> QueueNotTable, IReadOnlyCollection<ObserverId> TableNotQueue)
{
    /// <summary>
    /// The kind of disagreement for an observer the queue takes the batch for and the table does not.
    /// </summary>
    public const string QueueNotTableKind = "queue_not_table";

    /// <summary>
    /// The kind of disagreement for an observer the table takes the batch for and the queue holds no subscription of.
    /// </summary>
    public const string TableNotQueueKind = "table_not_queue";

    /// <summary>
    /// Compares which observers the routing table and the subscriptions of a queue take a batch for.
    /// </summary>
    /// <param name="table">The <see cref="ObserverRoutingTable"/> of the event store.</param>
    /// <param name="eventSequenceId">The <see cref="EventSequenceId"/> the batch was appended to.</param>
    /// <param name="batchEventTypes">The distinct event types of the batch.</param>
    /// <param name="subscriptions">The subscriptions the queue holds.</param>
    /// <param name="isServedByThisQueue">Whether an observer is routed to the queue that compares, among the queues of the sequence.</param>
    /// <returns>The <see cref="AppendedEventsQueueShadowRouting"/>.</returns>
    /// <remarks>
    /// A redaction is left out: it stands in for the event type it redacted, which only its content tells.
    /// </remarks>
    public static AppendedEventsQueueShadowRouting Compare(
        ObserverRoutingTable table,
        EventSequenceId eventSequenceId,
        IEnumerable<EventTypeId> batchEventTypes,
        IEnumerable<AppendedEventsQueueObserverSubscription> subscriptions,
        Func<ObserverId, bool> isServedByThisQueue)
    {
        var eventTypes = batchEventTypes.Where(_ => _ != GlobalEventTypes.Redaction).ToHashSet();
        if (eventTypes.Count == 0)
        {
            return new([], []);
        }

        var fromTable = eventTypes
            .SelectMany(eventTypeId => table.Match(eventSequenceId, eventTypeId))
            .Select(route => route.ObserverId)
            .Where(isServedByThisQueue)
            .ToHashSet();
        var subscribed = subscriptions.Select(_ => _.ObserverKey.ObserverId).ToHashSet();
        var fromQueue = subscriptions
            .Where(subscription => subscription.AllEventTypes || subscription.EventTypeIdSet.Overlaps(eventTypes))
            .Select(subscription => subscription.ObserverKey.ObserverId)
            .ToHashSet();

        return new(
            [.. fromQueue.Where(_ => !fromTable.Contains(_))],
            [.. fromTable.Where(_ => !subscribed.Contains(_))]);
    }
}
