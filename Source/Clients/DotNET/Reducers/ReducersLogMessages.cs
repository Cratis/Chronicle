// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reducers;

internal static partial class ReducersLogMessages
{
    [LoggerMessage(LogLevel.Information, "Registering reducers")]
    internal static partial void RegisterReducers(this ILogger<Reducers> logger);

    [LoggerMessage(LogLevel.Trace, "Registering reducer with id '{ReducerId}', for event sequence '{EventSequenceId}'")]
    internal static partial void RegisterReducer(this ILogger<Reducers> logger, ReducerId reducerId, EventSequenceId eventSequenceId);
}
