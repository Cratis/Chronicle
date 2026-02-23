// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

internal static partial class ReducersLogMessages
{
    [LoggerMessage(LogLevel.Debug, "Registering reducers")]
    internal static partial void RegisterReducers(this ILogger<Reducers> logger);

    [LoggerMessage(LogLevel.Trace, "Registering reducer with id '{ReducerId}', for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterReducer(this ILogger<Reducers> logger, ReducerId reducerId, EventSequenceId eventSequenceId);

    [LoggerMessage(LogLevel.Warning, "An error occurred while handling events with sequence number {StartSequenceNumber} to {EndSequenceNumber} was for Reducer {ReducerId}")]
    internal static partial void ErrorWhileHandlingEvents(this ILogger<Reducers> logger, Exception ex, EventSequenceNumber startSequenceNumber, EventSequenceNumber endSequenceNumber, ReducerId reducerId);

    [LoggerMessage(LogLevel.Trace, "Handling of events received for Reducer {ReducerId} completed")]
    internal static partial void EventHandlingCompleted(this ILogger<Reducers> logger, ReducerId reducerId);
}
