// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reducers;

internal static partial class ReducerReplayLogging
{
    [LoggerMessage(LogLevel.Error, "Reducer {ObserverId} was rebuilt and published, but recording its replay occurrence failed")]
    internal static partial void BookkeepingFailed(this ILogger<ReducerReplay> logger, Exception exception, ObserverId observerId);
}
