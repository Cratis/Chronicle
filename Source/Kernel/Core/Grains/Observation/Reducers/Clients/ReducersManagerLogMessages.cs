// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

internal static partial class ReducersManagerLogMessages
{
    [LoggerMessage(LogLevel.Information, "Registering client reducers")]
    internal static partial void RegisterReducers(this ILogger<ReducersManager> logger);

    [LoggerMessage(LogLevel.Trace, "Registering reducer with id '{ReducerId}', for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterReducer(this ILogger<ReducersManager> logger, ReducerId reducerId, EventSequenceId eventSequenceId);
}
