// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Channels;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Delivers the events of one <see cref="AppendedEventsQueue"/> to one observer, at that observer's own pace.
/// </summary>
/// <remarks>
/// Every observer subscribed to a queue gets its own lane, so a slow observer only delays itself: the queue routes
/// each batch to the lanes of the observers it matches and moves on without waiting for any of them. A lane keeps
/// the order the queue routed the events in and delivers them one call at a time, which is what an observer's single
/// next-event-sequence-number needs. A lane that has fallen behind coalesces its backlog into one delivery.
/// </remarks>
internal sealed class AppendedEventsQueueObserverLane
{
    /// <summary>
    /// Upper bound on how many routed batches one delivery coalesces.
    /// </summary>
    internal const int MaxCoalescedBatchesPerDelivery = 100;

    readonly Channel<Routed> _channel = Channel.CreateUnbounded<Routed>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = true
    });

    readonly Func<AppendedEventsQueueObserverLane, IReadOnlyList<AppendedEvent>, Task> _deliver;
    readonly Action<int> _settled;
    readonly Action<Exception> _failed;
    int _pendingBatches;
    volatile bool _isClosed;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueueObserverLane"/> class.
    /// </summary>
    /// <param name="observerKey">The <see cref="ObserverKey"/> of the observer the lane delivers to.</param>
    /// <param name="deliver">Delivers events, in sequence order, to the observer; it should stop once the lane is closed.</param>
    /// <param name="settled">Called with the number of queued batches a delivery (or a discard) has settled.</param>
    /// <param name="failed">Called when a delivery throws; the lane carries on with the next delivery.</param>
    /// <param name="run">Starts the lane's worker.</param>
    public AppendedEventsQueueObserverLane(
        ObserverKey observerKey,
        Func<AppendedEventsQueueObserverLane, IReadOnlyList<AppendedEvent>, Task> deliver,
        Action<int> settled,
        Action<Exception> failed,
        Func<Func<Task>, Task> run)
    {
        ObserverKey = observerKey;
        _deliver = deliver;
        _settled = settled;
        _failed = failed;
        Completion = run(Run) ?? Task.CompletedTask;
    }

    /// <summary>
    /// Gets the <see cref="ObserverKey"/> of the observer the lane delivers to.
    /// </summary>
    public ObserverKey ObserverKey { get; }

    /// <summary>
    /// Gets the task that completes once the lane has stopped and settled everything routed to it.
    /// </summary>
    public Task Completion { get; }

    /// <summary>
    /// Gets the number of queued batches routed to the lane and not yet settled.
    /// </summary>
    public int PendingBatches => Volatile.Read(ref _pendingBatches);

    /// <summary>
    /// Gets a value indicating whether the lane has been closed and discards what it still holds.
    /// </summary>
    public bool IsClosed => _isClosed;

    /// <summary>
    /// Routes events to the lane.
    /// </summary>
    /// <param name="events">The events, in sequence order, that the observer subscribes to.</param>
    /// <param name="batches">The number of queued batches the events came from.</param>
    /// <returns>True if the lane accepted the events; false if it has been closed or completed.</returns>
    public bool TryRoute(IReadOnlyList<AppendedEvent> events, int batches)
    {
        if (_isClosed)
        {
            return false;
        }

        Interlocked.Add(ref _pendingBatches, batches);
        if (_channel.Writer.TryWrite(new(events, batches)))
        {
            return true;
        }

        Interlocked.Add(ref _pendingBatches, -batches);
        return false;
    }

    /// <summary>
    /// Stops the lane after it has delivered everything already routed to it.
    /// </summary>
    public void Complete() => _channel.Writer.TryComplete();

    /// <summary>
    /// Stops the lane and discards everything routed to it that has not been delivered yet. A delivery already in
    /// flight runs to completion.
    /// </summary>
    public void Close()
    {
        _isClosed = true;
        _channel.Writer.TryComplete();
    }

    async Task Run()
    {
        var reader = _channel.Reader;
        while (await reader.WaitToReadAsync())
        {
            var routedEvents = new List<AppendedEvent>();
            var batches = 0;
            var drained = 0;
            while (drained < MaxCoalescedBatchesPerDelivery && reader.TryRead(out var routed))
            {
                routedEvents.AddRange(routed.Events);
                batches += routed.Batches;
                drained++;
            }

            if (drained == 0)
            {
                continue;
            }

            try
            {
                if (!_isClosed)
                {
                    // Each routed batch is in sequence order already; merging several needs a stable re-sort.
                    IReadOnlyList<AppendedEvent> events = drained > 1
                        ? [.. routedEvents.OrderBy(@event => @event.Context.SequenceNumber)]
                        : routedEvents;
                    await _deliver(this, events);
                }
            }
            catch (Exception ex)
            {
                // The observer's own partition-failure and catch-up machinery recovers what was not delivered; the
                // lane must keep going, or every later event for this observer would pile up behind a dead worker.
                _failed(ex);
            }
            finally
            {
                Interlocked.Add(ref _pendingBatches, -batches);
                _settled(batches);
            }
        }
    }

    sealed record Routed(IReadOnlyList<AppendedEvent> Events, int Batches);
}
