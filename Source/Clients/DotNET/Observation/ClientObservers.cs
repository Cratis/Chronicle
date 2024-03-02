// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents an implementation of <see cref="IClientObservers"/>.
/// </summary>
public class ClientObservers : IClientObservers
{
    readonly IObserversRegistrar _observers;
    readonly IEventTypes _eventTypes;
    readonly ILogger<ClientObservers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientObservers"/> class.
    /// </summary>
    /// <param name="observers">The <see cref="IObserversRegistrar"/> in the system.</param>
    /// <param name="eventTypes">The <see cref="IEventTypes"/> for the system. </param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientObservers(
        IObserversRegistrar observers,
        IEventTypes eventTypes,
        ILogger<ClientObservers> logger)
    {
        _observers = observers;
        _eventTypes = eventTypes;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<ObserverInvocationResult> OnNext(
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
                try
                {
                    await handler.OnNext(@event);
                    lastSuccessfullyObservedEvent = @event.Metadata.SequenceNumber;
                }
                catch (Exception ex)
                {
                    var eventType = _eventTypes.GetClrTypeFor(@event.Metadata.Type.Id);
                    _logger.ObserverFailed(handler.Name, eventType.Name, @event.Metadata.SequenceNumber, ex);
                    return ObserverInvocationResult.Failed(lastSuccessfullyObservedEvent, ex);
                }
            }
            else
            {
                _logger.UnknownObserver(observerId);
            }
        }

        return ObserverInvocationResult.Success(lastSuccessfullyObservedEvent);
    }
}
