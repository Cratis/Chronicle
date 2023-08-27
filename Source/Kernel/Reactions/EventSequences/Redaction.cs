// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Kernel.Events.EventSequences;
using Aksio.Cratis.Observation;
using EventRedacted = Aksio.Cratis.Kernel.Events.EventSequences.EventRedacted;
using IEventSequence = Aksio.Cratis.Kernel.Grains.EventSequences.IEventSequence;

namespace Aksio.Cratis.Kernel.Reactions.EventSequences;

/// <summary>
/// Observes events related to redaction done on any <see cref="EventSequence"/> and reacts to them.
/// </summary>
[Observer("341c5a8b-b3a0-49ff-9b26-6339fa53dc3b", eventSequence: EventSequenceId.System)]
public class Redaction
{
    readonly IGrainFactory _grainFactory;
    readonly IExecutionContextManager _executionContextManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="Redaction"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for getting grains.</param>
    /// <param name="executionContextManager"><see cref="IExecutionContextManager"/> for establishing correct execution context.</param>
    public Redaction(IGrainFactory grainFactory, IExecutionContextManager executionContextManager)
    {
        _grainFactory = grainFactory;
        _executionContextManager = executionContextManager;
    }

    /// <summary>
    /// Redact a single event.
    /// </summary>
    /// <param name="event">The event holding all details.</param>
    /// <param name="context">The <see cref="EventContext"/>.</param>
    /// <returns>Awaitable task.</returns>
    public async Task SingleEvent(EventRedacted @event, EventContext context)
    {
        _executionContextManager.Establish(@event.TenantId, context.CorrelationId, @event.Microservice);
        var grain = _grainFactory.GetGrain<IEventSequence>(@event.Sequence, keyExtension: new MicroserviceAndTenant(@event.Microservice, @event.TenantId));
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
        _executionContextManager.Establish(@event.TenantId, context.CorrelationId, @event.Microservice);
        var grain = _grainFactory.GetGrain<IEventSequence>(@event.Sequence, keyExtension: new MicroserviceAndTenant(@event.Microservice, @event.TenantId));
        await grain.Redact(@event.EventSourceId, @event.Reason, @event.EventTypes, context.Causation, context.CausedBy);
    }
}
