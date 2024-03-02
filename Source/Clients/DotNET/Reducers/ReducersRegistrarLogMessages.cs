// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.EventSequences;
using Aksio.Cratis.Observation;
using Aksio.Cratis.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Reducers;

internal static partial class ReducersRegistrarLogMessages
{
    [LoggerMessage(0, LogLevel.Information, "Registering reducers")]
    internal static partial void RegisterReducers(this ILogger<ReducersRegistrar> logger);

    [LoggerMessage(1, LogLevel.Trace, "Registering reducer with id '{ReducerId}' - friendly name {ReducerName}, for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterReducer(this ILogger<ReducersRegistrar> logger, ReducerId reducerId, ObserverName reducerName, EventSequenceId eventSequenceId);
}
