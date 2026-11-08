// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;

namespace Cratis.Chronicle.Testing.Reactors;

/// <summary>
/// A reactor with a batch method and a per-event method, for specifying batch delivery in a scenario.
/// </summary>
/// <param name="log">The <see cref="SeatBatchLog"/> to record calls in.</param>
public class SeatIndexReactor(SeatBatchLog log) : IReactor
{
    /// <summary>
    /// Indexes booked and released seats in one call per page; fails seats named "unavailable" and defers seats named "later".
    /// </summary>
    /// <param name="batch">The <see cref="EventBatch"/>.</param>
    /// <param name="context">The <see cref="BatchContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    [Observes<SeatBooked>]
    [Observes<SeatReleased>]
    public Task Index(EventBatch batch, BatchContext context)
    {
        log.Batches.Add([.. batch.Select(_ => _.Event)]);
        log.Contexts.Add(context);
        foreach (var @event in batch)
        {
            switch (@event.Event)
            {
                case SeatBooked { Seat: "unavailable" } booked:
                    batch.Fail(@event.Context, new SeatUnavailable(booked.Seat));
                    break;

                case SeatBooked { Seat: "later" }:
                    batch.Defer(@event.Context.EventSourceId, TimeSpan.FromSeconds(5));
                    break;
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Records a note, one event at a time.
    /// </summary>
    /// <param name="event">The <see cref="SeatNoted"/> event.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    public Task Noted(SeatNoted @event, EventContext context)
    {
        log.PerEvent.Add(@event);
        return Task.CompletedTask;
    }
}
