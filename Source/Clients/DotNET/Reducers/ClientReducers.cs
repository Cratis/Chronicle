// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Aksio.Cratis.Events;
using Aksio.Cratis.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Represents the endpoint called for receiving events from the kernel.
/// </summary>
public class ClientReducers : IClientReducers
{
    readonly IReducersRegistrar _reducers;
    readonly JsonSerializerOptions _jsonSerializerOptions;
    readonly ILogger<ClientReducers> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ClientReducers"/> class.
    /// </summary>
    /// <param name="reducers">The <see cref="IReducersRegistrar"/> in the system.</param>
    /// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public ClientReducers(
        IReducersRegistrar reducers,
        JsonSerializerOptions jsonSerializerOptions,
        ILogger<ClientReducers> logger)
    {
        _reducers = reducers;
        _jsonSerializerOptions = jsonSerializerOptions;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<InternalReduceResult> OnNext(
        ReducerId reducerId,
        IEnumerable<AppendedEvent> events,
        JsonObject? initialAsJson)
    {
        _logger.EventsReceived(events.Count(), reducerId);
        var handler = _reducers.GetById(reducerId);
        if (handler is not null)
        {
            var initial = initialAsJson is null ?
                null :
                initialAsJson.Deserialize(handler.ReadModelType, _jsonSerializerOptions)!;
            return await handler.OnNext(events, initial);
        }

        _logger.UnknownReducer(reducerId);
        return new(initialAsJson, EventSequenceNumber.Unavailable, Enumerable.Empty<string>(), string.Empty);
    }
}
