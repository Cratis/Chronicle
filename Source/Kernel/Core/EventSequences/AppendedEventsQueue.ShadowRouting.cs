// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Diagnostics;
using Cratis.Chronicle.Observation.ObserverRouting;

namespace Cratis.Chronicle.EventSequences;

/// <content>
/// The part of the queue that compares its own matching with the silo-local routing table, without acting on it.
/// </content>
/// <remarks>
/// Dispatch still follows the queue's subscriptions. The table is only read and the disagreements counted, so the table can
/// be trusted before dispatch moves to it. A failure to read the table never touches delivery.
/// </remarks>
public partial class AppendedEventsQueue
{
    readonly IObservers? _routing;

    async Task<ObserverRoutingTable?> GetShadowRoutingTable()
    {
        if (_routing is null || _eventSequenceKey == Concepts.EventSequences.EventSequenceKey.NotSet)
        {
            return null;
        }

        try
        {
            return await _routing.GetTable(_eventSequenceKey.EventStore);
        }
        catch (Exception exception) when (ObservationOnlyFailures.CanBeContained(exception))
        {
            return null;
        }
    }

    void CompareWithRoutingTable(ObserverRoutingTable table, AppendedEventsQueueObserverSubscription[] subscriptions, HashSet<EventTypeId> batchEventTypes)
    {
        Observe(() =>
        {
            var router = new AppendedEventsQueueRouter(Math.Max(_queueCount, 1));
            var disagreements = AppendedEventsQueueShadowRouting.Compare(
                table,
                _eventSequenceKey.EventSequenceId,
                batchEventTypes,
                subscriptions,
                observerId => _queueCount < 1 || router.GetQueueIndexFor(new ObserverKey(observerId, _eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceKey.EventSequenceId)) == _queueId);
            _metrics?.ShadowRoutingDisagreed(AppendedEventsQueueShadowRouting.QueueNotTableKind, disagreements.QueueNotTable.Count);
            _metrics?.ShadowRoutingDisagreed(AppendedEventsQueueShadowRouting.TableNotQueueKind, disagreements.TableNotQueue.Count);
        });
    }
}
