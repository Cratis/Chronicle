// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences;

/// <content>
/// Hands durable events to the appended-events queues, and spills the queues to catch-up, without holding the append.
/// </content>
/// <remarks>
/// <para>
/// An event is acknowledged as soon as it is durable. Handing it to live delivery, and the spill that recovers a failed
/// hand-over, are calls to other grains and can be addressed to a silo that was just lost, where they do not answer
/// until the response timeout. Awaited inside the append, one such call - and the spill behind it - would hold the
/// response for more than the client's own timeout although the event is stored, and the client would be told the
/// outcome is unknown. So the append only records what has to be delivered; a single pump, running in the turns of the
/// grain, performs the calls in order and bounds each one with <see cref="LiveDispatchTimeout"/>.
/// </para>
/// <para>
/// A failed or timed-out hand-over is recovered by spilling, which re-reads every observer's events from its persisted
/// cursor and so also covers every batch still waiting. A spill that fails is retried on a short backoff (see
/// <see cref="SpillAppendedEventsQueuesToCatchup"/>); until it lands, further appends only ask for a spill instead of
/// trying live delivery against queues that are not reachable.
/// </para>
/// </remarks>
public partial class EventSequence
{
    /// <summary>
    /// The longest the pump waits for one call to the queues before treating it as failed.
    /// </summary>
    internal static readonly TimeSpan LiveDispatchTimeout = TimeSpan.FromSeconds(5);

    readonly Queue<(List<AppendedEvent> Events, bool Import)> _pendingLiveDispatches = new();
    bool _spillRequested;
    bool _dispatchPumpRunning;
    bool _dispatchStopped;
    Task _dispatchPump = Task.CompletedTask;

    /// <summary>
    /// Gets a task that completes when everything handed to the queues so far has been delivered or has failed over to a spill.
    /// </summary>
    internal Task LiveDispatchSettled => _dispatchPump;

    void HandOverToLiveDelivery(List<AppendedEvent> appendedEvents, bool import)
    {
        if (_appendedEventsQueues is null || appendedEvents.Count == 0) return;

        // While a spill is outstanding the queues are not reachable, and the spill that lands reads these events too.
        if (_spillRetryTimer is not null || _spillRequested)
        {
            RequestSpillToCatchup();
            return;
        }

        _pendingLiveDispatches.Enqueue((appendedEvents, import));
        StartDispatchPump();
    }

    void RequestSpillToCatchup()
    {
        if (_appendedEventsQueues is null) return;
        _spillRequested = true;
        StartDispatchPump();
    }

    void StartDispatchPump()
    {
        if (_dispatchPumpRunning || _dispatchStopped) return;
        _dispatchPumpRunning = true;
        _dispatchPump = RunDispatchPump();
    }

    async Task RunDispatchPump()
    {
        try
        {
            while (!_dispatchStopped)
            {
                if (_spillRequested)
                {
                    // The spill covers every durable event, so the batches still waiting need no live delivery.
                    _spillRequested = false;
                    _pendingLiveDispatches.Clear();
                    await SpillAppendedEventsQueuesToCatchup();
                    continue;
                }

                if (!_pendingLiveDispatches.TryDequeue(out var batch)) return;

                try
                {
                    var queues = _appendedEventsQueues!;
                    var call = batch.Import ? queues.SpillMatchingToCatchup(batch.Events) : queues.Enqueue(batch.Events);
                    await call.WaitAsync(LiveDispatchTimeout, _timeProvider);
                }
                catch (Exception ex)
                {
                    logger.FailedEnqueueingAppendedEvents(_eventSequenceKey.EventStore, _eventSequenceKey.Namespace, _eventSequenceId, batch.Events.Count, ex);
                    _spillRequested = true;
                }
            }
        }
        finally
        {
            _dispatchPumpRunning = false;
        }
    }

    /// <summary>
    /// Settles what the pump still owes before the grain goes away, so a deactivation never drops an acknowledged event.
    /// </summary>
    /// <remarks>
    /// Batches waiting for the pump, an in-flight hand-over and an outstanding spill exist only in this activation. Once
    /// it is gone nothing would tell the observers about those events, and an observer that is then handed a later event
    /// moves its position past them for good. So the pump is allowed to finish, bounded, and when anything is still owed
    /// after that one last spill covers every durable event from each observer's persisted cursor.
    /// </remarks>
    /// <returns>An awaitable task.</returns>
    async Task SettleLiveDispatchBeforeDeactivation()
    {
        if (_appendedEventsQueues is null) return;

        try
        {
            await _dispatchPump.WaitAsync(LiveDispatchTimeout, _timeProvider);
        }
        catch (Exception)
        {
            // The pump did not settle in time; what it still owed is covered by the spill below.
            _spillRequested = true;
        }

        if (_spillRequested || _spillRetryTimer is not null || _pendingLiveDispatches.Count > 0)
        {
            _spillRequested = false;
            _pendingLiveDispatches.Clear();
            await SpillAppendedEventsQueuesToCatchup();
        }
    }

    void StopDispatchPump()
    {
        _dispatchStopped = true;
        _spillRequested = false;
        _pendingLiveDispatches.Clear();
    }
}
