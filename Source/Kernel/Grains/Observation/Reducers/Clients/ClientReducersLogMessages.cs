// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

internal static partial class ClientReducersLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering client reducers")]
    internal static partial void RegisterReducers(this ILogger<ClientReducers> logger);

    [LoggerMessage(1, LogLevel.Trace, "Registering reducer with id '{ReducerId}' - friendly name {ReducerName}, for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterReducer(this ILogger<ClientReducers> logger, ReducerId reducerId, ObserverName reducerName, EventSequenceId eventSequenceId);
}
