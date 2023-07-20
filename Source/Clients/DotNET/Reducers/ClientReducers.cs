// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents the endpoint called for receiving events from the kernel.
/// </summary>
public class ClientReducers
{
    readonly IReducersRegistry _reducers;
    readonly ILogger<ClientReducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReducers"/> class.
    /// </summary>
    /// <param name="reducers">The <see cref="IReducersRegistry"/> in the system</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientReducers(
        IReducersRegistry reducers,
        ILogger<ClientReducers> logger)
    {
        _reducers = reducers;
        _logger = logger;
    }

    /// <summary>
    /// Called for events to be handled.
    /// </summary>
    /// <param name="reducerId">The <see cref="ReducerId"/> of the reducer it is for.</param>
    /// <param name="event">The <see cref="AppendedEvent"/>.</param>
    /// <param name="initial">The initial state.</param>
    /// <returns>Reduced result.</returns>
    public async Task<object> OnNext(
        ReducerId reducerId,
        AppendedEvent @event,
        object? initial)
    {
        _logger.EventReceived(@event.Metadata.Type.Id, reducerId);
        var handler = _reducers.GetById(reducerId);
        if (handler is not null)
        {
            return await handler.OnNext(@event, initial);
        }

        _logger.UnknownReducer(reducerId);
        return initial!;
    }
}
