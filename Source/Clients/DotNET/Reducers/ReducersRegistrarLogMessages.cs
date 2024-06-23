// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

internal static partial class ReducersRegistrarLogMessages
{
    [LoggerMessage(LogLevel.Information, "Registering reducers")]
    internal static partial void RegisterReducers(this ILogger<ReducersRegistrar> logger);

    [LoggerMessage(LogLevel.Trace, "Registering reducer with id '{ReducerId}' - friendly name {ReducerName}, for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterReducer(this ILogger<ReducersRegistrar> logger, ReducerId reducerId, ObserverName reducerName, EventSequenceId eventSequenceId);
}
