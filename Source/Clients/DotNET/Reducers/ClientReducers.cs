// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Events;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

/// <summary>
/// Represents the endpoint called for receiving events from the kernel.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ClientReducers"/> class.
/// </remarks>
/// <param name="reducers">The <see cref="IReducers"/> in the system.</param>
/// <param name="jsonSerializerOptions"><see cref="JsonSerializerOptions"/> for serialization.</param>
/// <param name="logger"><see cref="ILogger"/> for logging.</param>
public class ClientReducers(
    IReducers reducers,
    JsonSerializerOptions jsonSerializerOptions,
    ILogger<ClientReducers> logger) : IClientReducers
{
    /// <inheritdoc/>
    public async Task<InternalReduceResult> OnNext(
        ReducerId reducerId,
        IEnumerable<AppendedEvent> events,
        JsonObject? initialAsJson)
    {
        logger.EventsReceived(events.Count(), reducerId);
        var handler = reducers.GetById(reducerId);
        if (handler is not null)
        {
            var initial = initialAsJson is null ?
                null :
                initialAsJson.Deserialize(handler.ReadModelType, jsonSerializerOptions)!;
            return await handler.OnNext(events, initial);
        }

        logger.UnknownReducer(reducerId);
        return new(initialAsJson, EventSequenceNumber.Unavailable, [], string.Empty);
    }
}
