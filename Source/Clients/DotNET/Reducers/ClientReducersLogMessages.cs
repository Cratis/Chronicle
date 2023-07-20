// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Reducers;

/// <summary>
/// Log messages for <see cref="ClientReducers"/>.
/// </summary>
internal static partial class ClientReducersLogMessages
{
    [LoggerMessage(0, LogLevel.Trace, "Event of type {EventTypeId} was received for reducer {ReducerId}")]
    internal static partial void EventReceived(this ILogger<ClientReducers> logger, EventTypeId eventTypeId, ReducerId observerId);

    [LoggerMessage(1, LogLevel.Information, "Reducer with id '{ReducerId}' does not exist.")]
    internal partial void UnknownReducer(this ILogger<ClientReducers> logger, ReducerId reducerId);
}
