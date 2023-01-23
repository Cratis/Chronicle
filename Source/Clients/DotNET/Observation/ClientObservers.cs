// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Observation;

/// <summary>
/// Represents a controller for receiving events from the kernel.
/// </summary>
[Route("/.cratis/observers")]
public class ClientObservers : Controller
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

    /// <summary>
    /// Action that is called for events to be handled.
    /// </summary>
    /// <param name="observerId">The <see cref="ObserverId"/> of the observer it is for.</param>
    /// <param name="event">The <see cref="AppendedEvent"/>.</param>
    /// <returns>Awaitable task.</returns>
    [HttpPost("{observerId}")]
    public async Task OnNext(
        [FromRoute] ObserverId observerId,
        [FromBody] AppendedEvent @event)
    {
        _logger.EventReceived(@event.Metadata.Type.Id, observerId);
        var handler = _observers.Handlers.FirstOrDefault(_ => _.ObserverId == observerId);
        if (handler is not null)
        {
            await handler.OnNext(@event);
        }
        else
        {
            _logger.UnknownObserver(observerId);
        }
    }
}
