// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents the endpoint called for receiving events from the kernel.
/// </summary>
public class ClientObservers
{
    readonly IObserversRegistrar _observers;
    readonly ILogger<ClientObservers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObservers"/> class.
    /// </summary>
    /// <param name="observers">The <see cref="IObserversRegistrar"/> in the system.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientObservers(
        IObserversRegistrar observers,
        ILogger<ClientObservers> logger)
    {
        _observers = observers;
        _logger = logger;
    }

    /// <summary>
    /// Called for events to be handled.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer it is for.</param>
    /// <param name="events">The collection of <see cref="AppendedEvent"/>.</param>
    /// <returns>Sequence number of last successfully processed event.</returns>
    public async Task<EventSequenceNumber> OnNext(
        ObserverId observerId,
        IEnumerable<AppendedEvent> events)
    {
        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;

        foreach (var @event in events)
        {
            _logger.EventReceived(@event.Metadata.Type.Id, observerId);
            var handler = _observers.GetById(observerId);
            if (handler is not null)
            {
                await handler.OnNext(@event);
                lastSuccessfullyObservedEvent = @event.Metadata.SequenceNumber;
            }
            else
            {
                _logger.UnknownObserver(observerId);
            }
        }

        return lastSuccessfullyObservedEvent;
    }
}
