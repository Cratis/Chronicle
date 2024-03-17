// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Cratis.EventSequences;
using Cratis.Kernel.Events.EventSequences;
using Cratis.Kernel.Storage.EventSequences;
using Cratis.Observation;
using EventRedacted = Cratis.Kernel.Events.EventSequences.EventRedacted;
using IEventSequence = Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Cratis.Kernel.Reactions.EventSequences;

/// <summary>
/// Observes events related to redaction done on any <see cref="EventSequence"/> and reacts to them.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="Redaction"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
[Observer("341c5a8b-b3a0-49ff-9b26-6339fa53dc3b", eventSequence: EventSequenceId.System)]
public class Redaction(IGrainFactory grainFactory)
{
    /// <summary>
    /// Redact a single event.
    /// </summary>
    /// <param name="event">The event holding all details.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task SingleEvent(EventRedacted @event, EventContext context)
    {
        var grain = grainFactory.GetGrain<IEventSequence>(@event.Sequence, keyExtension: new EventSequenceKey(context.EventStore, context.Namespace));
        await grain.Redact(@event.SequenceNumber, @event.Reason, context.Causation, context.CausedBy);
    }

    /// <summary>
    /// Redact all events for a specific event source.
    /// </summary>
    /// <param name="event">The event holding all details.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task ByEventSource(EventsRedactedForEventSource @event, EventContext context)
    {
        var grain = grainFactory.GetGrain<IEventSequence>(@event.Sequence, keyExtension: new EventSequenceKey(context.EventStore, context.Namespace));
        await grain.Redact(@event.EventSourceId, @event.Reason, @event.EventTypes, context.Causation, context.CausedBy);
    }
}
