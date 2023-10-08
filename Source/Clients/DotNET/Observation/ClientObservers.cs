// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IClientObservers"/>.
/// </summary>
public class ClientObservers : IClientObservers
{
    readonly IObservers _observers;
    readonly ILogger<ClientObservers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObservers"/> class.
    /// </summary>
    /// <param name="observers">The <see cref="IObservers"/> in the system.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientObservers(
        IObservers observers,
        ILogger<ClientObservers> logger)
    {
        _observers = observers;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<EventSequenceNumber> OnNext(
        ObserverId observerId,
        IEnumerable<AppendedEvent> events)
    {
        var lastSuccessfullyObservedEvent = EventSequenceNumber.Unavailable;

        foreach (var @event in events)
        {
            _logger.EventReceived(@event.Metadata.Type.Id, observerId);
            var handler = _observers.GetHandlerById(observerId);
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
